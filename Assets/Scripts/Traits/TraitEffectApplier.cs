using System.Collections.Generic;
using Platforming;
using UnityEngine;

namespace Traits
{
    [RequireComponent(typeof(TraitSlotContainer))]
    public sealed class TraitEffectApplier : MonoBehaviour
    {
        [SerializeField] private bool affectColliders = true;
        [SerializeField] private bool affectRigidbody = true;
        [SerializeField] private bool affectVisuals = true;
        [SerializeField, Range(0.05f, 1f)] private float nonCollidableAlpha = 0.35f;

        private TraitSlotContainer container;
        private Collider2D[] colliders;
        private bool[] initialTriggerStates;
        private SpriteRenderer[] spriteRenderers;
        private Rigidbody2D body;
        private PlayerPlatformJump2D playerMovement;
        private float initialGravityScale;
        private RigidbodyType2D initialBodyType;

        private void Awake()
        {
            container = GetComponent<TraitSlotContainer>();
            colliders = GetGameplayColliders();
            initialTriggerStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                initialTriggerStates[i] = colliders[i] != null && colliders[i].isTrigger;
            }
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            playerMovement = GetComponent<PlayerPlatformJump2D>();
            if (body != null)
            {
                initialGravityScale = body.gravityScale;
                initialBodyType = body.bodyType;
            }
        }

        private void OnEnable()
        {
            if (container != null)
            {
                container.SlotsChanged += Apply;
                Apply(container);
            }
        }

        private void OnDisable()
        {
            if (container != null)
            {
                container.SlotsChanged -= Apply;
            }
        }

        public void Apply(TraitSlotContainer changedContainer)
        {
            bool hasCollidable = container.HasTrait(TraitType.Collidable);
            bool hasForceAffected = container.HasTrait(TraitType.ForceAffected);

            if (affectColliders)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider2D collider = colliders[i];
                    if (collider == null)
                    {
                        continue;
                    }

                    collider.enabled = true;
                    collider.isTrigger = hasCollidable ? initialTriggerStates[i] : true;
                }
            }

            if (affectRigidbody && body != null)
            {
                if (hasForceAffected)
                {
                    body.bodyType = initialBodyType;
                    body.gravityScale = initialGravityScale;
                }
                else
                {
                    body.gravityScale = 0f;
                    if (playerMovement != null)
                    {
                        body.bodyType = RigidbodyType2D.Dynamic;
                    }
                    else
                    {
                        body.linearVelocity = Vector2.zero;
                        body.angularVelocity = 0f;
                        body.bodyType = RigidbodyType2D.Kinematic;
                    }
                }
            }

            if (affectVisuals)
            {
                float alpha = hasCollidable ? 1f : nonCollidableAlpha;
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    SpriteRenderer spriteRenderer = spriteRenderers[i];
                    if (spriteRenderer == null)
                    {
                        continue;
                    }

                    Color color = spriteRenderer.color;
                    color.a = alpha;
                    spriteRenderer.color = color;
                }
            }
        }

        private Collider2D[] GetGameplayColliders()
        {
            Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
            List<Collider2D> gameplayColliders = new List<Collider2D>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                Collider2D collider = allColliders[i];
                if (collider != null && collider.GetComponent<TraitInteractionArea>() == null)
                {
                    gameplayColliders.Add(collider);
                }
            }

            return gameplayColliders.ToArray();
        }
    }
}
