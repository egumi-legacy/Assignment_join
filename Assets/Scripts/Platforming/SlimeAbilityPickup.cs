using UnityEngine;

namespace Platforming
{
    public sealed class SlimeAbilityPickup : MonoBehaviour
    {
        [SerializeField] private bool destroyAfterPickup = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerPlatformJump2D player = other.GetComponentInParent<PlayerPlatformJump2D>();
            if (player == null)
            {
                return;
            }

            player.GrantSlimeAbility();

            if (destroyAfterPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}
