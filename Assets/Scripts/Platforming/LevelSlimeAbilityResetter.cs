using UnityEngine;
using UnityEngine.SceneManagement;

namespace Platforming
{
    public sealed class LevelSlimeAbilityResetter : MonoBehaviour
    {
        private void Start()
        {
            ResetPlayersInScene();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetPlayersInScene();
        }

        public void ResetPlayersInScene()
        {
            PlayerPlatformJump2D[] players = FindObjectsByType<PlayerPlatformJump2D>(FindObjectsSortMode.None);
            foreach (PlayerPlatformJump2D player in players)
            {
                player.ResetForLevel();
            }
        }
    }
}
