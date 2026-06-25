using System.Collections.Generic;
using Platforming;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Traits
{
    [RequireComponent(typeof(TraitSlotContainer))]
    public sealed class TraitEffectApplier : MonoBehaviour
    {
        [SerializeField] private bool affectColliders = true;
        [SerializeField] private bool affectRigidbody = true;
        [SerializeField] private bool affectVisuals = true;
        [SerializeField, Range(0.05f, 1f)] private float nonCollidableAlpha = 0.35f;
        [SerializeField] private Color slimeTint = new Color(0.35f, 1f, 0.35f, 1f);

        private TraitSlotContainer container;
        private Collider2D[] colliders;
        private bool[] initialTriggerStates;
        private SpriteRenderer[] spriteRenderers;
        private Color[] initialSpriteColors;
        private Tilemap[] tilemaps;
        private Color[] initialTilemapColors;
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
            initialSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                initialSpriteColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
            }

            tilemaps = GetComponentsInChildren<Tilemap>();
            initialTilemapColors = new Color[tilemaps.Length];
            for (int i = 0; i < tilemaps.Length; i++)
            {
                initialTilemapColors[i] = tilemaps[i] != null ? tilemaps[i].color : Color.white;
            }

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
            bool hasSlime = container.HasTrait(TraitType.Slime);

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
                float alphaMultiplier = hasCollidable ? 1f : nonCollidableAlpha;
                ApplySpriteVisuals(hasSlime, alphaMultiplier);
                ApplyTilemapVisuals(hasSlime, alphaMultiplier);
            }
        }

        private void ApplySpriteVisuals(bool hasSlime, float alphaMultiplier)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                spriteRenderer.color = BuildTraitColor(initialSpriteColors[i], hasSlime, alphaMultiplier);
            }
        }

        private void ApplyTilemapVisuals(bool hasSlime, float alphaMultiplier)
        {
            for (int i = 0; i < tilemaps.Length; i++)
            {
                Tilemap tilemap = tilemaps[i];
                if (tilemap == null)
                {
                    continue;
                }

                tilemap.color = BuildTraitColor(initialTilemapColors[i], hasSlime, alphaMultiplier);
            }
        }

        private Color BuildTraitColor(Color baseColor, bool hasSlime, float alphaMultiplier)
        {
            Color color = baseColor;
            if (hasSlime)
            {
                color.r *= slimeTint.r;
                color.g *= slimeTint.g;
                color.b *= slimeTint.b;
            }

            color.a = baseColor.a * alphaMultiplier;
            return color;
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
