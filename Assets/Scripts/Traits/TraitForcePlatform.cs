using Platforming;
using UnityEngine;

namespace Traits
{
    [RequireComponent(typeof(TraitSlotContainer))]
    public sealed class TraitForcePlatform : MonoBehaviour
    {
        [SerializeField] private bool applyUpwardForce;
        [SerializeField] private bool applyOnlyWhenForceAffected = true;
        [SerializeField] private float upwardVelocity = 6f;

        private TraitSlotContainer traits;

        private void Awake()
        {
            traits = GetComponent<TraitSlotContainer>();
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            ApplyUpwardForce(collision.collider);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ApplyUpwardForce(other);
        }

        private void ApplyUpwardForce(Collider2D other)
        {
            if (!applyUpwardForce)
            {
                return;
            }

            if (applyOnlyWhenForceAffected && traits != null && !traits.HasTrait(TraitType.ForceAffected))
            {
                return;
            }

            PlayerPlatformJump2D player = other.GetComponentInParent<PlayerPlatformJump2D>();
            if (player == null)
            {
                return;
            }

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            if (velocity.y < upwardVelocity)
            {
                velocity.y = upwardVelocity;
                body.linearVelocity = velocity;
            }
        }
    }
}
