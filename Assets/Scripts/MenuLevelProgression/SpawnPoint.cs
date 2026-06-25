using UnityEngine;

namespace MenuLevelProgression
{
    public sealed class SpawnPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.75f);
        }
#endif
    }
}
