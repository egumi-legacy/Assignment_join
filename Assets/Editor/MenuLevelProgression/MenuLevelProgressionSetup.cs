#if UNITY_EDITOR
using System.IO;
using MenuLevelProgression;
using Platforming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

namespace MenuLevelProgression.Editor
{
    public static class MenuLevelProgressionSetup
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string Level01ScenePath = "Assets/Scenes/Level01.unity";
        private const string Level02ScenePath = "Assets/Scenes/Level02.unity";

        private static int validationStep;
        private static double validationDeadline;
        private static GameFlowController validationFlow;
        private const string ValidationActiveKey = "MenuLevelProgression.ValidationActive";
        private const string ValidationStepKey = "MenuLevelProgression.ValidationStep";
        private const string ValidationDeadlineKey = "MenuLevelProgression.ValidationDeadline";

        [InitializeOnLoadMethod]
        private static void RestoreValidationAfterReload()
        {
            if (!SessionState.GetBool(ValidationActiveKey, false))
            {
                return;
            }

            validationStep = SessionState.GetInt(ValidationStepKey, 0);
            validationDeadline = SessionState.GetFloat(ValidationDeadlineKey, 0f);
            validationFlow = null;
            EditorApplication.update -= ValidateDemoFlowUpdate;
            EditorApplication.update += ValidateDemoFlowUpdate;
        }

        [MenuItem("Tools/Menu Level Progression/Build Demo Scenes")]
        public static void BuildDemoScenes()
        {
            EnsureScenesFolder();
            CreateLevelScene(Level01ScenePath, "Level01", new Vector3(7f, 0.5f, 0f));
            CreateLevelScene(Level02ScenePath, "Level02", new Vector3(9f, 1.5f, 0f));
            CreateBootstrapScene();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            Debug.Log("Menu Level Progression demo scenes created and added to Build Settings.");
        }

        [MenuItem("Tools/Menu Level Progression/Validate Demo Flow")]
        public static void ValidateDemoFlow()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before starting Menu Level Progression validation.");
                return;
            }

            BuildDemoScenes();
            validationStep = 0;
            validationDeadline = EditorApplication.timeSinceStartup + 15d;
            validationFlow = null;
            SessionState.SetBool(ValidationActiveKey, true);
            SessionState.SetInt(ValidationStepKey, validationStep);
            SessionState.SetFloat(ValidationDeadlineKey, (float)validationDeadline);
            EditorApplication.update -= ValidateDemoFlowUpdate;
            EditorApplication.update += ValidateDemoFlowUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void ValidateDemoFlowUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= ValidateDemoFlowUpdate;
                return;
            }

            if (EditorApplication.timeSinceStartup > validationDeadline)
            {
                FailValidation("Timed out while validating menu-level progression flow.");
                return;
            }

            if (validationFlow == null)
            {
                validationFlow = Object.FindObjectOfType<GameFlowController>();
                if (validationFlow == null)
                {
                    return;
                }
            }

            switch (validationStep)
            {
                case 0:
                    validationFlow.ClearProgressForDebug();
                    validationFlow.StartGame();
                    SetValidationStep(1);
                    break;
                case 1:
                    if (SceneManager.GetSceneByName("Level01").isLoaded)
                    {
                        validationFlow.SkipCurrentLevel();
                        SetValidationStep(2);
                    }
                    break;
                case 2:
                    if (validationFlow.State == GameFlowState.CompletionReward && validationFlow.Progress.HighestUnlockedLevel >= 2)
                    {
                        validationFlow.LoadNextLevel();
                        SetValidationStep(3);
                    }
                    break;
                case 3:
                    if (SceneManager.GetSceneByName("Level02").isLoaded && validationFlow.State == GameFlowState.Playing)
                    {
                        Debug.Log("Menu Level Progression validation passed: StartGame loaded Level01, Skip unlocked reward flow, Next Level loaded Level02.");
                        ClearValidationState();
                        EditorApplication.update -= ValidateDemoFlowUpdate;
                        EditorApplication.isPlaying = false;
                    }
                    break;
            }
        }

        private static void FailValidation(string message)
        {
            Debug.LogError(message);
            ClearValidationState();
            EditorApplication.update -= ValidateDemoFlowUpdate;
            EditorApplication.isPlaying = false;
        }

        private static void SetValidationStep(int step)
        {
            validationStep = step;
            SessionState.SetInt(ValidationStepKey, validationStep);
        }

        private static void ClearValidationState()
        {
            SessionState.EraseBool(ValidationActiveKey);
            SessionState.EraseInt(ValidationStepKey);
            SessionState.EraseFloat(ValidationDeadlineKey);
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static void CreateBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("GameRoot");
            GameRoot gameRoot = root.AddComponent<GameRoot>();
            LevelLoader loader = root.GetComponent<LevelLoader>();
            GameFlowController flow = root.GetComponent<GameFlowController>();

            GameObject player = CreatePlayer();
            GameObject uiRoot = CreateUI(flow);
            GameFlowUI ui = uiRoot.GetComponent<GameFlowUI>();

            SetObjectReference(flow, "player", player.transform);
            SetObjectReference(flow, "ui", ui);
            SetLevels(flow);

            Selection.activeGameObject = root;
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.25f, 0.85f, 0.35f, 1f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.2f);

            PlayerPlatformJump2D jump = player.AddComponent<PlayerPlatformJump2D>();
            GameObject groundCheck = new GameObject("groundCheck");
            groundCheck.transform.SetParent(player.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            SetObjectReference(jump, "groundCheck", groundCheck.transform);

            return player;
        }

        private static GameObject CreateUI(GameFlowController flow)
        {
            GameObject canvasObject = new GameObject("GameFlowCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            GameFlowUI ui = canvasObject.AddComponent<GameFlowUI>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject mainMenu = CreatePanel(canvasObject.transform, "MainMenuPanel");
            CreateLabel(mainMenu.transform, "Title", "Slime Platform Demo", new Vector2(0f, 150f), 32);
            Button start = CreateButton(mainMenu.transform, "StartButton", "Start Game", new Vector2(0f, 70f));
            Button cont = CreateButton(mainMenu.transform, "ContinueButton", "Continue", new Vector2(0f, 20f));
            Button select = CreateButton(mainMenu.transform, "LevelSelectButton", "Level Select", new Vector2(0f, -30f));

            GameObject levelSelect = CreatePanel(canvasObject.transform, "LevelSelectPanel");
            CreateLabel(levelSelect.transform, "LevelSelectTitle", "Select Level", new Vector2(0f, 150f), 28);
            GameObject levelButtonRoot = new GameObject("LevelButtonRoot", typeof(RectTransform));
            levelButtonRoot.transform.SetParent(levelSelect.transform, false);
            RectTransform levelRootRect = levelButtonRoot.GetComponent<RectTransform>();
            levelRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRootRect.sizeDelta = new Vector2(260f, 180f);
            levelRootRect.anchoredPosition = new Vector2(0f, 30f);
            VerticalLayoutGroup layout = levelButtonRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            Button levelTemplateButton = CreateButton(levelButtonRoot.transform, "LevelButtonTemplate", "Level", Vector2.zero);
            LevelSelectButton levelButtonPrefab = levelTemplateButton.gameObject.AddComponent<LevelSelectButton>();
            Button levelBack = CreateButton(levelSelect.transform, "LevelSelectBackButton", "Back", new Vector2(0f, -160f));

            GameObject gameplay = CreatePanel(canvasObject.transform, "GameplayPanel", new Color(0f, 0f, 0f, 0f));
            CreateLabel(gameplay.transform, "GameplayHint", "Esc: Pause", new Vector2(-300f, 200f), 18);

            GameObject pause = CreatePanel(canvasObject.transform, "PausePanel");
            CreateLabel(pause.transform, "PauseTitle", "Paused", new Vector2(0f, 150f), 28);
            Button resume = CreateButton(pause.transform, "ResumeButton", "Resume", new Vector2(0f, 80f));
            Button restart = CreateButton(pause.transform, "RestartButton", "Restart", new Vector2(0f, 30f));
            Button returnSelect = CreateButton(pause.transform, "ReturnLevelSelectButton", "Level Select", new Vector2(0f, -20f));
            Button returnMain = CreateButton(pause.transform, "ReturnMainMenuButton", "Main Menu", new Vector2(0f, -70f));
            Button skip = CreateButton(pause.transform, "SkipLevelButton", "Skip Level", new Vector2(0f, -120f));

            GameObject reward = CreatePanel(canvasObject.transform, "CompletionRewardPanel");
            Text rewardText = CreateLabel(reward.transform, "RewardMessageText", "恭喜通关！", new Vector2(0f, 120f), 32);
            Button next = CreateButton(reward.transform, "NextLevelButton", "Next Level", new Vector2(0f, 40f));
            Button rewardSelect = CreateButton(reward.transform, "RewardLevelSelectButton", "Level Select", new Vector2(0f, -10f));
            Button rewardMain = CreateButton(reward.transform, "RewardMainMenuButton", "Main Menu", new Vector2(0f, -60f));

            SetObjectReference(ui, "mainMenuPanel", mainMenu);
            SetObjectReference(ui, "levelSelectPanel", levelSelect);
            SetObjectReference(ui, "pausePanel", pause);
            SetObjectReference(ui, "completionRewardPanel", reward);
            SetObjectReference(ui, "gameplayPanel", gameplay);
            SetObjectReference(ui, "startButton", start);
            SetObjectReference(ui, "continueButton", cont);
            SetObjectReference(ui, "levelSelectButton", select);
            SetObjectReference(ui, "levelButtonRoot", levelButtonRoot.transform);
            SetObjectReference(ui, "levelButtonPrefab", levelButtonPrefab);
            SetObjectReference(ui, "levelSelectBackButton", levelBack);
            SetObjectReference(ui, "resumeButton", resume);
            SetObjectReference(ui, "restartButton", restart);
            SetObjectReference(ui, "returnToLevelSelectButton", returnSelect);
            SetObjectReference(ui, "returnToMainMenuButton", returnMain);
            SetObjectReference(ui, "skipLevelButton", skip);
            SetObjectReference(ui, "rewardMessageText", rewardText);
            SetObjectReference(ui, "nextLevelButton", next);
            SetObjectReference(ui, "rewardLevelSelectButton", rewardSelect);
            SetObjectReference(ui, "rewardMainMenuButton", rewardMain);

            return canvasObject;
        }

        private static void CreateLevelScene(string scenePath, string sceneName, Vector3 exitPosition)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = camera.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;

            GameObject spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(-6f, 1f, 0f);
            spawn.AddComponent<SpawnPoint>();

            CreateLevelBox("Ground", new Vector3(0f, -1f, 0f), new Vector3(14f, 1f, 1f), new Color(0.35f, 0.35f, 0.35f, 1f));
            CreateLevelBox("Platform", new Vector3(2f, 1f, 0f), new Vector3(3f, 0.35f, 1f), new Color(0.45f, 0.45f, 0.45f, 1f));

            GameObject exit = new GameObject("LevelExit");
            exit.transform.position = exitPosition;
            BoxCollider2D exitCollider = exit.AddComponent<BoxCollider2D>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector2(1f, 2f);
            exit.AddComponent<LevelExit>();
            SpriteRenderer exitRenderer = exit.AddComponent<SpriteRenderer>();
            exitRenderer.color = Color.yellow;

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static GameObject CreateLevelBox(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject box = new GameObject(name);
            box.transform.position = position;
            box.transform.localScale = scale;
            SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
            renderer.color = color;
            BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            return box;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            return CreatePanel(parent, name, new Color(0f, 0f, 0f, 0.65f));
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 40f);
            rect.anchoredPosition = position;
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            CreateLabel(buttonObject.transform, "Text", text, Vector2.zero, 18);
            return button;
        }

        private static Text CreateLabel(Transform parent, string name, string text, Vector2 position, int fontSize)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 40f);
            rect.anchoredPosition = position;
            Text label = labelObject.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }

        private static void SetLevels(GameFlowController flow)
        {
            SerializedObject serialized = new SerializedObject(flow);
            SerializedProperty levels = serialized.FindProperty("levels");
            levels.arraySize = 2;
            SetLevel(levels.GetArrayElementAtIndex(0), 1, "Level 1", "Level01");
            SetLevel(levels.GetArrayElementAtIndex(1), 2, "Level 2", "Level02");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLevel(SerializedProperty level, int id, string displayName, string sceneName)
        {
            level.FindPropertyRelative("levelId").intValue = id;
            level.FindPropertyRelative("displayName").stringValue = displayName;
            level.FindPropertyRelative("sceneName").stringValue = sceneName;
        }

        private static void SetObjectReference(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"Missing serialized field '{fieldName}' on {target}.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(Level01ScenePath, true),
                new EditorBuildSettingsScene(Level02ScenePath, true)
            };
        }
    }
}
#endif
