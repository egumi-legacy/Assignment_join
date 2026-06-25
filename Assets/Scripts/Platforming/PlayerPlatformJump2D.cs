using MenuLevelProgression;
using Traits;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Platforming
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerPlatformJump2D : MonoBehaviour, ILevelScopedReset
    {
        [Header("Grounding")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.12f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Jump Tuning")]
        [SerializeField] private float normalJumpVelocity = 8f;
        [SerializeField] private float slimeBigJumpVelocity = 11.3f;
        [SerializeField] private float slimeChargeDuration = 0.25f;
        [SerializeField] private float slimePlatformComboJumpVelocity = 13.5f;
        [SerializeField] private float maxSlimeJumpVelocity = 14f;
        [SerializeField] private float slimeSurfaceMemory = 0.12f;

        [Header("Slime Surface Defaults")]
        [SerializeField] private float defaultSlimeMinImpactSpeed = 3f;
        [SerializeField] private float defaultSlimeRestitution = 1.05f;
        [SerializeField] private float defaultSlimeMinBounceVelocity = 7f;
        [SerializeField] private float defaultSlimeMaxBounceVelocity = 18f;
        [SerializeField, Range(0f, 1f)] private float defaultSlimeAbsorbSuppression = 1f;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("Feedback")]
        public UnityEvent compressionStarted;
        public UnityEvent compressionReady;
        public UnityEvent compressionCancelled;
        public UnityEvent slimeBigJumpReleased;
        public UnityEvent normalJumpReleased;

        private Rigidbody2D body;
        private Collider2D[] ownColliders;
        private SlimeJumpController slimeJump;
        private TraitSlotContainer traits;
        private bool jumpPressedThisFrame;
        private Vector2 moveInput;
        private Vector2 previousVelocity;
        private float lastSlimeSurfaceTime = -999f;
        private SlimeSurfaceSettings currentSlimeSurfaceSettings;

        public bool HasSlimeAbility => HasSlimeTrait;
        public bool IsCompressing => slimeJump != null && slimeJump.IsCompressing;
        public bool IsChargeReady => slimeJump != null && slimeJump.IsChargeReady;
        public bool HasControl { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public bool IsHoldingDown => moveInput.y < -0.5f;
        public Vector2 PreviousVelocity => previousVelocity;

        private bool HasForceAffected => traits == null || traits.HasTrait(TraitType.ForceAffected);
        private bool HasSlimeTrait => traits != null && traits.HasTrait(TraitType.Slime);
        private bool IsOnRecentSlimeSurface => Time.time - lastSlimeSurfaceTime <= slimeSurfaceMemory;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            if (traits != null)
            {
                traits.SlotsChanged += OnTraitSlotsChanged;
            }

            if (moveAction != null)
            {
                moveAction.action.Enable();
            }

            if (jumpAction != null)
            {
                jumpAction.action.Enable();
                jumpAction.action.performed += OnJumpPerformed;
            }
        }

        private void OnDisable()
        {
            if (traits != null)
            {
                traits.SlotsChanged -= OnTraitSlotsChanged;
            }

            if (jumpAction != null)
            {
                jumpAction.action.performed -= OnJumpPerformed;
                jumpAction.action.Disable();
            }

            if (moveAction != null)
            {
                moveAction.action.Disable();
            }
        }

        private void Update()
        {
            if (jumpAction == null && ReadJumpPressedThisFrame())
            {
                jumpPressedThisFrame = true;
            }
        }

        private void FixedUpdate()
        {
            EnsureInitialized();
            if (body == null || slimeJump == null)
            {
                return;
            }

            bool wasGrounded = IsGrounded;
            previousVelocity = body.linearVelocity;
            GroundInfo ground = CheckGrounded();
            IsGrounded = ground.IsGrounded;
            UpdateCurrentSlimeSurface(ground);

            moveInput = ReadMoveInput();
            bool canUseForceMovement = HasForceAffected;
            bool canUseSlimeJump = canUseForceMovement && HasSlimeTrait;
            bool downHeld = canUseForceMovement && moveInput.y < -0.5f;
            bool jumpRequested = canUseForceMovement && jumpPressedThisFrame;

            if (!wasGrounded && IsGrounded)
            {
                HandleSlimeSurfaceLanding(downHeld);
            }

            JumpAction action = slimeJump.Tick(Time.fixedDeltaTime, IsGrounded, downHeld, jumpRequested, HasControl && canUseForceMovement, canUseSlimeJump);
            jumpPressedThisFrame = false;

            if (IsCompressing)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
            else if (HasControl)
            {
                body.linearVelocity = new Vector2(moveInput.x * moveSpeed, body.linearVelocity.y);
            }

            HandleJumpAction(action);
        }

        public void GrantSlimeAbility()
        {
            EnsureInitialized();
            if (traits == null)
            {
                return;
            }

            traits.TryAddTraitToFirstEmpty(TraitType.Slime);
        }

        public void ResetForLevel()
        {
            EnsureInitialized();
            slimeJump.ResetForLevel();
            jumpPressedThisFrame = false;
            lastSlimeSurfaceTime = -999f;
            currentSlimeSurfaceSettings = null;
            IsGrounded = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                previousVelocity = Vector2.zero;
            }
        }

        private void EnsureInitialized()
        {
            if (body != null && slimeJump != null)
            {
                return;
            }

            body = GetComponent<Rigidbody2D>();
            ownColliders = GetComponentsInChildren<Collider2D>();
            slimeJump = new SlimeJumpController(slimeChargeDuration);
            traits = GetComponent<TraitSlotContainer>();
        }

        private void OnTraitSlotsChanged(TraitSlotContainer changedContainer)
        {
            if (changedContainer != traits || slimeJump == null || HasSlimeTrait)
            {
                return;
            }

            HandleJumpAction(slimeJump.CancelForTraitLoss());
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            jumpPressedThisFrame = true;
        }

        private void HandleJumpAction(JumpAction action)
        {
            switch (action)
            {
                case JumpAction.NormalJump:
                    ApplyJump(normalJumpVelocity);
                    normalJumpReleased?.Invoke();
                    break;
                case JumpAction.SlimeBigJump:
                    ApplyJump(GetSlimeJumpVelocity());
                    slimeBigJumpReleased?.Invoke();
                    break;
                case JumpAction.SlimeCompressionStarted:
                    ShowSlimeSurfaceFeedback();
                    compressionStarted?.Invoke();
                    break;
                case JumpAction.SlimeCompressionReady:
                    compressionReady?.Invoke();
                    break;
                case JumpAction.SlimeCompressionCancelled:
                    compressionCancelled?.Invoke();
                    break;
            }
        }

        private float GetSlimeJumpVelocity()
        {
            if (!IsOnRecentSlimeSurface)
            {
                return slimeBigJumpVelocity;
            }

            float comboVelocity = currentSlimeSurfaceSettings != null ? currentSlimeSurfaceSettings.ComboJumpVelocity : slimePlatformComboJumpVelocity;
            float comboMax = currentSlimeSurfaceSettings != null ? currentSlimeSurfaceSettings.MaxComboJumpVelocity : maxSlimeJumpVelocity;
            float velocity = Mathf.Max(slimeBigJumpVelocity, comboVelocity);
            return Mathf.Min(velocity, Mathf.Max(slimeBigJumpVelocity, comboMax));
        }

        private void ApplyJump(float velocity)
        {
            Vector2 currentVelocity = body.linearVelocity;
            currentVelocity.y = velocity;
            body.linearVelocity = currentVelocity;
        }

        private void HandleSlimeSurfaceLanding(bool downHeld)
        {
            if (!IsOnRecentSlimeSurface || !HasControl || !HasForceAffected)
            {
                return;
            }

            SurfaceConfig config = GetCurrentSurfaceConfig();
            if (!config.EnableSurfaceBounce)
            {
                return;
            }

            float incomingSpeed = Mathf.Max(0f, -previousVelocity.y);
            if (incomingSpeed < config.MinImpactSpeed)
            {
                return;
            }

            ShowSlimeSurfaceFeedback();

            if (downHeld)
            {
                if (HasSlimeTrait)
                {
                    HandleJumpAction(slimeJump.StartCompressionFromSurface());
                }

                if (config.AbsorbSuppression >= 1f)
                {
                    return;
                }
            }

            float bounceVelocity = Mathf.Clamp(incomingSpeed * config.Restitution, config.MinBounceVelocity, config.MaxBounceVelocity);
            if (downHeld)
            {
                bounceVelocity *= Mathf.Clamp01(1f - config.AbsorbSuppression);
            }

            if (bounceVelocity > 0f)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.y = Mathf.Max(velocity.y, bounceVelocity);
                body.linearVelocity = velocity;
            }
        }

        private void UpdateCurrentSlimeSurface(GroundInfo ground)
        {
            if (!ground.IsGrounded || ground.Traits == null || !ground.Traits.HasTrait(TraitType.Slime))
            {
                currentSlimeSurfaceSettings = null;
                return;
            }

            lastSlimeSurfaceTime = Time.time;
            currentSlimeSurfaceSettings = ground.Settings;
        }

        private SurfaceConfig GetCurrentSurfaceConfig()
        {
            if (currentSlimeSurfaceSettings == null)
            {
                return new SurfaceConfig(
                    true,
                    defaultSlimeMinImpactSpeed,
                    defaultSlimeRestitution,
                    defaultSlimeMinBounceVelocity,
                    defaultSlimeMaxBounceVelocity,
                    defaultSlimeAbsorbSuppression);
            }

            return new SurfaceConfig(
                currentSlimeSurfaceSettings.EnableSurfaceBounce,
                currentSlimeSurfaceSettings.MinImpactSpeed,
                currentSlimeSurfaceSettings.Restitution,
                currentSlimeSurfaceSettings.MinBounceVelocity,
                currentSlimeSurfaceSettings.MaxBounceVelocity,
                currentSlimeSurfaceSettings.AbsorbSuppression);
        }

        private void ShowSlimeSurfaceFeedback()
        {
            if (currentSlimeSurfaceSettings != null)
            {
                currentSlimeSurfaceSettings.ShowCompressedFeedback();
            }
        }

        private GroundInfo CheckGrounded()
        {
            if (groundCheck == null)
            {
                return GroundInfo.None;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.isTrigger || IsOwnCollider(hit))
                {
                    continue;
                }

                TraitSlotContainer groundTraits = hit.GetComponentInParent<TraitSlotContainer>();
                SlimeSurfaceSettings settings = hit.GetComponentInParent<SlimeSurfaceSettings>();
                return new GroundInfo(true, hit, groundTraits, settings);
            }

            return GroundInfo.None;
        }

        private bool IsOwnCollider(Collider2D hit)
        {
            if (ownColliders == null)
            {
                return false;
            }

            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (hit == ownColliders[i])
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2 ReadMoveInput()
        {
            if (moveAction != null)
            {
                return moveAction.action.ReadValue<Vector2>();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            float vertical = 0f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        private bool ReadJumpPressedThisFrame()
        {
            if (jumpAction != null || Keyboard.current == null)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
        }

        private readonly struct GroundInfo
        {
            public static readonly GroundInfo None = new GroundInfo(false, null, null, null);

            public GroundInfo(bool isGrounded, Collider2D collider, TraitSlotContainer traits, SlimeSurfaceSettings settings)
            {
                IsGrounded = isGrounded;
                Collider = collider;
                Traits = traits;
                Settings = settings;
            }

            public bool IsGrounded { get; }
            public Collider2D Collider { get; }
            public TraitSlotContainer Traits { get; }
            public SlimeSurfaceSettings Settings { get; }
        }

        private readonly struct SurfaceConfig
        {
            public SurfaceConfig(bool enableSurfaceBounce, float minImpactSpeed, float restitution, float minBounceVelocity, float maxBounceVelocity, float absorbSuppression)
            {
                EnableSurfaceBounce = enableSurfaceBounce;
                MinImpactSpeed = Mathf.Max(0f, minImpactSpeed);
                Restitution = Mathf.Max(0f, restitution);
                MinBounceVelocity = Mathf.Max(0f, minBounceVelocity);
                MaxBounceVelocity = Mathf.Max(MinBounceVelocity, maxBounceVelocity);
                AbsorbSuppression = Mathf.Clamp01(absorbSuppression);
            }

            public bool EnableSurfaceBounce { get; }
            public float MinImpactSpeed { get; }
            public float Restitution { get; }
            public float MinBounceVelocity { get; }
            public float MaxBounceVelocity { get; }
            public float AbsorbSuppression { get; }
        }
    }
}
