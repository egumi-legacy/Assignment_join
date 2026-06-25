using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MenuLevelProgression
{
    public sealed class LevelLoader : MonoBehaviour
    {
        private Scene loadedLevelScene;

        public string CurrentSceneName => loadedLevelScene.IsValid() ? loadedLevelScene.name : string.Empty;
        public bool HasLoadedLevel => loadedLevelScene.IsValid() && loadedLevelScene.isLoaded;

        public IEnumerator LoadLevel(LevelDefinition level, Transform player)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (string.IsNullOrWhiteSpace(level.SceneName))
            {
                Debug.LogError($"Level {level.LevelId} has no scene name.", this);
                yield break;
            }

            yield return UnloadCurrentLevel();

            AsyncOperation load = SceneManager.LoadSceneAsync(level.SceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"Failed to start loading level scene '{level.SceneName}'. Check Build Settings.", this);
                yield break;
            }

            while (!load.isDone)
            {
                yield return null;
            }

            loadedLevelScene = SceneManager.GetSceneByName(level.SceneName);
            if (!loadedLevelScene.IsValid() || !loadedLevelScene.isLoaded)
            {
                Debug.LogError($"Loaded scene '{level.SceneName}' is not valid.", this);
                yield break;
            }

            SceneManager.SetActiveScene(loadedLevelScene);
            MovePlayerToSpawn(player);
            ResetPlayerForLevel(player);
        }

        public IEnumerator ReloadLevel(LevelDefinition level, Transform player)
        {
            yield return LoadLevel(level, player);
        }

        public IEnumerator UnloadCurrentLevel()
        {
            if (!HasLoadedLevel)
            {
                loadedLevelScene = default;
                yield break;
            }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(loadedLevelScene);
            if (unload != null)
            {
                while (!unload.isDone)
                {
                    yield return null;
                }
            }

            loadedLevelScene = default;
        }

        private void MovePlayerToSpawn(Transform player)
        {
            if (player == null)
            {
                Debug.LogError("Cannot place player because no player Transform is assigned.", this);
                return;
            }

            SpawnPoint spawnPoint = FindObjectOfType<SpawnPoint>();
            if (spawnPoint == null)
            {
                Debug.LogError($"Scene '{CurrentSceneName}' has no SpawnPoint. Player was not moved into the level.", this);
                return;
            }

            player.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
        }

        private void ResetPlayerForLevel(Transform player)
        {
            if (player == null)
            {
                return;
            }

            ILevelScopedReset[] resetters = player.GetComponentsInChildren<ILevelScopedReset>(true);
            for (int i = 0; i < resetters.Length; i++)
            {
                resetters[i].ResetForLevel();
            }
        }
    }
}
