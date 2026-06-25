using UnityEngine;

namespace MenuLevelProgression
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Flow == null)
            {
                Debug.LogError("LevelExit was triggered, but no GameRoot/GameFlowController is active.", this);
                return;
            }

            root.Flow.CompleteCurrentLevel();
        }
    }
}
