using MenuLevelProgression;
using Platforming;
using UnityEngine;

namespace Traits
{
    [RequireComponent(typeof(TraitSlotContainer))]
    public sealed class PlayerTraitVisual : MonoBehaviour, ILevelScopedReset
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite slimeSprite;
        [SerializeField] private Sprite slimeCompressingSprite;
        [SerializeField] private Sprite slimeReadySprite;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color slimeFallbackColor = new Color(0.2f, 1f, 0.25f, 1f);
        [SerializeField] private Color slimeCompressingFallbackColor = new Color(0.1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color slimeReadyFallbackColor = new Color(1f, 0.85f, 0.15f, 1f);

        private TraitSlotContainer traits;
        private PlayerPlatformJump2D movement;
        private Sprite capturedNormalSprite;
        private Color capturedNormalColor;

        private void Awake()
        {
            traits = GetComponent<TraitSlotContainer>();
            movement = GetComponent<PlayerPlatformJump2D>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                capturedNormalSprite = spriteRenderer.sprite;
                capturedNormalColor = spriteRenderer.color;
                if (normalSprite == null)
                {
                    normalSprite = capturedNormalSprite;
                }

                normalColor = capturedNormalColor.a > 0f ? capturedNormalColor : Color.white;
            }
        }

        private void OnEnable()
        {
            if (traits != null)
            {
                traits.SlotsChanged += OnTraitsChanged;
            }
        }

        private void OnDisable()
        {
            if (traits != null)
            {
                traits.SlotsChanged -= OnTraitsChanged;
            }
        }

        private void LateUpdate()
        {
            Apply();
        }

        public void ResetForLevel()
        {
            Apply();
        }

        private void OnTraitsChanged(TraitSlotContainer changedContainer)
        {
            Apply();
        }

        private void Apply()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            bool hasSlime = traits != null && traits.HasTrait(TraitType.Slime);
            if (!hasSlime)
            {
                SetVisual(FirstSprite(normalSprite, capturedNormalSprite, spriteRenderer.sprite), normalColor);
                return;
            }

            if (movement != null && movement.IsChargeReady)
            {
                SetVisual(FirstSprite(slimeReadySprite, slimeSprite, normalSprite, capturedNormalSprite, spriteRenderer.sprite), slimeReadySprite != null ? Color.white : slimeReadyFallbackColor);
                return;
            }

            if (movement != null && movement.IsCompressing)
            {
                SetVisual(FirstSprite(slimeCompressingSprite, slimeSprite, normalSprite, capturedNormalSprite, spriteRenderer.sprite), slimeCompressingSprite != null ? Color.white : slimeCompressingFallbackColor);
                return;
            }

            SetVisual(FirstSprite(slimeSprite, normalSprite, capturedNormalSprite, spriteRenderer.sprite), slimeSprite != null ? Color.white : slimeFallbackColor);
        }

        private void SetVisual(Sprite sprite, Color color)
        {
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            if (color.a <= 0f)
            {
                color.a = 1f;
            }

            spriteRenderer.color = color;
            spriteRenderer.enabled = true;
        }

        private static Sprite FirstSprite(params Sprite[] sprites)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    return sprites[i];
                }
            }

            return null;
        }
    }
}
