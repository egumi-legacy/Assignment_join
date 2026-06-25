using UnityEngine;

namespace Traits
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class TraitInteractionArea : MonoBehaviour
    {
        private void Reset()
        {
            Collider2D area = GetComponent<Collider2D>();
            area.isTrigger = true;
        }
    }
}
