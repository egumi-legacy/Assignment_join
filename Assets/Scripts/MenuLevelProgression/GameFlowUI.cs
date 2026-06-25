using UnityEngine;
using UnityEngine.UI;

namespace MenuLevelProgression
{
    public sealed class GameFlowUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject completionRewardPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject traitHudRoot;

        [Header("Main Menu")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button levelSelectButton;

        [Header("Level Select")]
        [SerializeField] private Transform levelButtonRoot;
        [SerializeField] private LevelSelectButton levelButtonPrefab;
        [SerializeField] private Button levelSelectBackButton;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnToLevelSelectButton;
        [SerializeField] private Button returnToMainMenuButton;
        [SerializeField] private Button skipLevelButton;

        [Header("Completion Reward")]
        [SerializeField] private Text rewardMessageText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button rewardLevelSelectButton;
        [SerializeField] private Button rewardMainMenuButton;

        [Header("Reset Feedback")]
        [SerializeField] private Image resetFlashImage;
        [SerializeField] private float resetFlashDuration = 0.18f;

        private GameFlowController flow;
        private float resetFlashUntil;

        private void Awake()
        {
            flow = FindObjectOfType<GameFlowController>();
            ShowMainMenu();
            WireButtons();
        }

        private void Update()
        {
            UpdateResetFlash();
        }

        public void ShowMainMenu()
        {
            SetOnly(mainMenuPanel, false);
        }

        public void ShowGameplay()
        {
            SetOnly(gameplayPanel, true);
        }

        public void ShowPauseMenu()
        {
            SetOnly(pausePanel, false);
        }

        public void ShowCompletionReward(bool hasNextLevel)
        {
            SetOnly(completionRewardPanel, false);
            if (rewardMessageText != null)
            {
                rewardMessageText.text = "恭喜通关！";
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(hasNextLevel);
                nextLevelButton.interactable = hasNextLevel;
            }
        }

        public void ShowLevelSelect(LevelDefinition[] levels, int highestUnlockedLevel)
        {
            SetOnly(levelSelectPanel, false);
            RebuildLevelButtons(levels, highestUnlockedLevel);
        }

        public void PlayResetFlash()
        {
            if (resetFlashImage == null)
            {
                return;
            }

            resetFlashUntil = Time.unscaledTime + Mathf.Max(0.01f, resetFlashDuration);
            resetFlashImage.gameObject.SetActive(true);
            Color color = resetFlashImage.color;
            color.a = 0.75f;
            resetFlashImage.color = color;
        }

        private void WireButtons()
        {
            AddClick(startButton, () => flow.StartGame());
            AddClick(continueButton, () => flow.ContinueGame());
            AddClick(levelSelectButton, () => flow.ShowLevelSelect());
            AddClick(levelSelectBackButton, () => flow.ShowMainMenu());
            AddClick(resumeButton, () => flow.ResumeGame());
            AddClick(restartButton, () => flow.RestartCurrentLevel());
            AddClick(returnToLevelSelectButton, () => flow.ReturnToLevelSelect());
            AddClick(returnToMainMenuButton, () => flow.ShowMainMenu());
            AddClick(skipLevelButton, () => flow.SkipCurrentLevel());
            AddClick(nextLevelButton, () => flow.LoadNextLevel());
            AddClick(rewardLevelSelectButton, () => flow.ShowLevelSelect());
            AddClick(rewardMainMenuButton, () => flow.ShowMainMenu());
        }

        private void RebuildLevelButtons(LevelDefinition[] levels, int highestUnlockedLevel)
        {
            if (levelButtonRoot == null || levelButtonPrefab == null || levels == null)
            {
                return;
            }

            levelButtonPrefab.gameObject.SetActive(false);

            for (int i = levelButtonRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = levelButtonRoot.GetChild(i).gameObject;
                if (child != levelButtonPrefab.gameObject)
                {
                    Destroy(child);
                }
            }

            for (int i = 0; i < levels.Length; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null)
                {
                    continue;
                }

                LevelSelectButton button = Instantiate(levelButtonPrefab, levelButtonRoot);
                button.gameObject.SetActive(true);
                bool unlocked = level.LevelId <= highestUnlockedLevel;
                int index = i;
                button.Bind(level.DisplayName, unlocked, () => flow.LoadLevelByIndex(index));
            }
        }

        private void SetOnly(GameObject panel, bool showTraitHud)
        {
            SetActive(mainMenuPanel, panel == mainMenuPanel);
            SetActive(levelSelectPanel, panel == levelSelectPanel);
            SetActive(pausePanel, panel == pausePanel);
            SetActive(completionRewardPanel, panel == completionRewardPanel);
            SetActive(gameplayPanel, panel == gameplayPanel);
            SetActive(traitHudRoot, showTraitHud);
        }

        private void UpdateResetFlash()
        {
            if (resetFlashImage == null || !resetFlashImage.gameObject.activeSelf)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, resetFlashDuration);
            float remaining = resetFlashUntil - Time.unscaledTime;
            if (remaining <= 0f)
            {
                resetFlashImage.gameObject.SetActive(false);
                return;
            }

            Color color = resetFlashImage.color;
            color.a = Mathf.Clamp01(remaining / duration) * 0.75f;
            resetFlashImage.color = color;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }
    }
}
