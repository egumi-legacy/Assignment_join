using MenuLevelProgression;
using UnityEngine;

namespace Platforming
{
    public sealed class SlimeSurfaceSettings : MonoBehaviour, ILevelScopedReset
    {
        [SerializeField] private bool enableSurfaceBounce = true;
        [SerializeField] private float minImpactSpeed = 3f;
        [SerializeField] private float restitution = 1.05f;
        [SerializeField] private float minBounceVelocity = 7f;
        [SerializeField] private float maxBounceVelocity = 18f;
        [SerializeField, Range(0f, 1f)] private float absorbSuppression = 1f;
        [SerializeField] private float comboJumpVelocity = 22f;
        [SerializeField] private float maxComboJumpVelocity = 23f;
        [SerializeField] private float compressedFeedbackDuration = 0.12f;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 compressedScale = new Vector3(1.08f, 0.86f, 1f);

        private Vector3 defaultScale;
        private float compressedUntil;

        public bool EnableSurfaceBounce => enableSurfaceBounce;
        public float MinImpactSpeed => Mathf.Max(0f, minImpactSpeed);
        public float Restitution => Mathf.Max(0f, restitution);
        public float MinBounceVelocity => Mathf.Max(0f, minBounceVelocity);
        public float MaxBounceVelocity => Mathf.Max(MinBounceVelocity, maxBounceVelocity);
        public float AbsorbSuppression => Mathf.Clamp01(absorbSuppression);
        public float ComboJumpVelocity => Mathf.Max(0f, comboJumpVelocity);
        public float MaxComboJumpVelocity => Mathf.Max(ComboJumpVelocity, maxComboJumpVelocity);

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            defaultScale = visualRoot.localScale;
        }

        private void Update()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localScale = Time.time < compressedUntil ? Vector3.Scale(defaultScale, compressedScale) : defaultScale;
        }

        public void ShowCompressedFeedback()
        {
            compressedUntil = Time.time + Mathf.Max(0.01f, compressedFeedbackDuration);
        }

        public void ResetForLevel()
        {
            compressedUntil = 0f;
            if (visualRoot != null)
            {
                visualRoot.localScale = defaultScale;
            }
        }
    }
}
