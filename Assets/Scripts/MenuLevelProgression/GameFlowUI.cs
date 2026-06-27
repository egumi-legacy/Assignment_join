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
        [SerializeField] private int maxLevelSelectColumns = 4;
        [SerializeField] private Vector2 levelButtonCellSize = new Vector2(150f, 44f);
        [SerializeField] private Vector2 levelButtonSpacing = new Vector2(16f, 12f);

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
            LevelButtonLayout layout = ConfigureLevelButtonRoot(levels.Length);

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
                PositionLevelButton(button, i, layout);
                bool unlocked = level.LevelId <= highestUnlockedLevel;
                int index = i;
                button.Bind(level.DisplayName, unlocked, () => flow.LoadLevelByIndex(index));
            }
        }

        private LevelButtonLayout ConfigureLevelButtonRoot(int levelCount)
        {
            int maxColumns = maxLevelSelectColumns > 0 ? maxLevelSelectColumns : 4;
            Vector2 cellSize = levelButtonCellSize.x > 0f && levelButtonCellSize.y > 0f ? levelButtonCellSize : new Vector2(150f, 44f);
            Vector2 spacing = levelButtonSpacing.x > 0f || levelButtonSpacing.y > 0f ? levelButtonSpacing : new Vector2(16f, 12f);
            int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, levelCount))), 1, maxColumns);
            int rows = Mathf.CeilToInt(Mathf.Max(1, levelCount) / (float)columns);
            float width = columns * cellSize.x + Mathf.Max(0, columns - 1) * spacing.x;
            float height = rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;

            RectTransform rootRect = levelButtonRoot as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = new Vector2(0f, 20f);
                rootRect.sizeDelta = new Vector2(width, height);
            }

            VerticalLayoutGroup verticalLayout = levelButtonRoot.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                verticalLayout.enabled = false;
            }

            GridLayoutGroup grid = levelButtonRoot.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.enabled = false;
            }

            return new LevelButtonLayout(columns, cellSize, spacing, width, height);
        }

        private static void PositionLevelButton(LevelSelectButton button, int index, LevelButtonLayout layout)
        {
            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            int column = index % layout.Columns;
            int row = index / layout.Columns;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = layout.CellSize;
            float x = -layout.Width * 0.5f + layout.CellSize.x * 0.5f + column * (layout.CellSize.x + layout.Spacing.x);
            float y = layout.Height * 0.5f - layout.CellSize.y * 0.5f - row * (layout.CellSize.y + layout.Spacing.y);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private readonly struct LevelButtonLayout
        {
            public LevelButtonLayout(int columns, Vector2 cellSize, Vector2 spacing, float width, float height)
            {
                Columns = Mathf.Max(1, columns);
                CellSize = cellSize;
                Spacing = spacing;
                Width = width;
                Height = height;
            }

            public int Columns { get; }
            public Vector2 CellSize { get; }
            public Vector2 Spacing { get; }
            public float Width { get; }
            public float Height { get; }
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
