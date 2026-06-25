using UnityEngine;

namespace MenuLevelProgression
{
    [RequireComponent(typeof(GameFlowController))]
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public GameFlowController Flow { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Flow = GetComponent<GameFlowController>();
        }
    }
}
