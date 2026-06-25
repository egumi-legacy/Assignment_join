using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MenuLevelProgression
{
    public enum GameFlowState
    {
        MainMenu,
        LevelSelect,
        Playing,
        Paused,
        CompletionReward
    }

    [RequireComponent(typeof(LevelLoader))]
    public sealed class GameFlowController : MonoBehaviour
    {
        [Header("Level Setup")]
        [SerializeField] private Transform player;
        [SerializeField] private LevelDefinition[] levels = new LevelDefinition[]
        {
            new LevelDefinition()
        };

        [Header("Failure Reset")]
        [SerializeField] private float outOfScreenMargin = 2f;

        [Header("UI")]
        [SerializeField] private GameFlowUI ui;

        private readonly ProgressService progress = new ProgressService();
        private LevelLoader levelLoader;
        private LevelDefinition currentLevel;
        private GameFlowState state;
        private bool transitionInProgress;

        public ProgressService Progress => progress;
        public GameFlowState State => state;
        public LevelDefinition CurrentLevel => currentLevel;
        public LevelDefinition[] Levels => levels;

        private void Awake()
        {
            levelLoader = GetComponent<LevelLoader>();
            Time.timeScale = 1f;
            SetPlayerGameplayActive(false);
        }

        private void Start()
        {
            ShowMainMenu();
        }

        private void Update()
        {
            if (state == GameFlowState.Playing && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                PauseGame();
            }

            if (state == GameFlowState.Playing && IsPlayerBelowScreen())
            {
                RestartCurrentLevelWithFeedback();
            }
        }

        public void ShowMainMenu()
        {
            StartCoroutine(ShowMainMenuRoutine());
        }

        public void ShowLevelSelect()
        {
            if (transitionInProgress)
            {
                return;
            }

            Time.timeScale = 1f;
            SetPlayerGameplayActive(false);
            state = GameFlowState.LevelSelect;
            ui?.ShowLevelSelect(levels, progress.HighestUnlockedLevel);
        }

        public void StartGame()
        {
            LoadLevelByIndex(0);
        }

        public void ContinueGame()
        {
            int unlockedIndex = Mathf.Clamp(progress.HighestUnlockedLevel - 1, 0, Mathf.Max(0, levels.Length - 1));
            LoadLevelByIndex(unlockedIndex);
        }

        public void LoadLevelByIndex(int index)
        {
            if (index < 0 || index >= levels.Length)
            {
                Debug.LogError($"Level index {index} is outside configured levels.", this);
                return;
            }

            LevelDefinition level = levels[index];
            if (level.LevelId > progress.HighestUnlockedLevel)
            {
                Debug.LogWarning($"Level {level.LevelId} is locked and cannot be loaded.", this);
                return;
            }

            StartCoroutine(LoadLevelRoutine(level));
        }

        public void PauseGame()
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            state = GameFlowState.Paused;
            Time.timeScale = 0f;
            ui?.ShowPauseMenu();
        }

        public void ResumeGame()
        {
            if (state != GameFlowState.Paused)
            {
                return;
            }

            state = GameFlowState.Playing;
            Time.timeScale = 1f;
            ui?.ShowGameplay();
        }

        public void RestartCurrentLevel()
        {
            if (currentLevel == null)
            {
                return;
            }

            Time.timeScale = 1f;
            StartCoroutine(LoadLevelRoutine(currentLevel));
        }

        public void RestartCurrentLevelWithFeedback()
        {
            if (transitionInProgress || currentLevel == null)
            {
                return;
            }

            ui?.PlayResetFlash();
            RestartCurrentLevel();
        }

        public void ReturnToLevelSelect()
        {
            StartCoroutine(ReturnToLevelSelectRoutine());
        }

        public void CompleteCurrentLevel()
        {
            if (currentLevel == null)
            {
                return;
            }

            int nextLevelId = currentLevel.LevelId + 1;
            if (HasLevel(nextLevelId))
            {
                progress.UnlockThrough(nextLevelId);
            }

            state = GameFlowState.CompletionReward;
            Time.timeScale = 1f;
            SetPlayerGameplayActive(false);
            ui?.ShowCompletionReward(HasLevel(nextLevelId));
        }

        public void SkipCurrentLevel()
        {
            CompleteCurrentLevel();
        }

        public void LoadNextLevel()
        {
            if (currentLevel == null)
            {
                return;
            }

            int nextIndex = GetLevelIndex(currentLevel.LevelId + 1);
            if (nextIndex >= 0)
            {
                LoadLevelByIndex(nextIndex);
            }
        }

        public void ClearProgressForDebug()
        {
            progress.ClearProgressForDebug();
            if (state == GameFlowState.LevelSelect)
            {
                ui?.ShowLevelSelect(levels, progress.HighestUnlockedLevel);
            }
        }

        private IEnumerator ShowMainMenuRoutine()
        {
            Time.timeScale = 1f;
            SetPlayerGameplayActive(false);
            yield return levelLoader.UnloadCurrentLevel();
            currentLevel = null;
            state = GameFlowState.MainMenu;
            ui?.ShowMainMenu();
        }

        private IEnumerator ReturnToLevelSelectRoutine()
        {
            Time.timeScale = 1f;
            SetPlayerGameplayActive(false);
            yield return levelLoader.UnloadCurrentLevel();
            currentLevel = null;
            ShowLevelSelect();
        }

        private IEnumerator LoadLevelRoutine(LevelDefinition level)
        {
            if (transitionInProgress)
            {
                yield break;
            }

            transitionInProgress = true;
            Time.timeScale = 1f;
            SetPlayerGameplayActive(false);
            ui?.ShowGameplay();
            yield return levelLoader.LoadLevel(level, player);
            currentLevel = level;
            SetPlayerGameplayActive(true);
            state = GameFlowState.Playing;
            transitionInProgress = false;
        }

        private void SetPlayerGameplayActive(bool active)
        {
            if (player == null)
            {
                return;
            }

            player.gameObject.SetActive(active);
        }

        private bool IsPlayerBelowScreen()
        {
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                return false;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            float cameraBottom = camera.ViewportToWorldPoint(Vector3.zero).y;
            return player.position.y < cameraBottom - Mathf.Max(0f, outOfScreenMargin);
        }

        private bool HasLevel(int levelId)
        {
            return GetLevelIndex(levelId) >= 0;
        }

        private int GetLevelIndex(int levelId)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i].LevelId == levelId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
