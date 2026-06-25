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

        private GameFlowController flow;

        private void Awake()
        {
            flow = FindObjectOfType<GameFlowController>();
            WireButtons();
        }

        public void ShowMainMenu()
        {
            SetOnly(mainMenuPanel);
        }

        public void ShowGameplay()
        {
            SetOnly(gameplayPanel);
        }

        public void ShowPauseMenu()
        {
            SetOnly(pausePanel);
        }

        public void ShowCompletionReward(bool hasNextLevel)
        {
            SetOnly(completionRewardPanel);
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
            SetOnly(levelSelectPanel);
            RebuildLevelButtons(levels, highestUnlockedLevel);
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

        private void SetOnly(GameObject panel)
        {
            SetActive(mainMenuPanel, panel == mainMenuPanel);
            SetActive(levelSelectPanel, panel == levelSelectPanel);
            SetActive(pausePanel, panel == pausePanel);
            SetActive(completionRewardPanel, panel == completionRewardPanel);
            SetActive(gameplayPanel, panel == gameplayPanel);
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
