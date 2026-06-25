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
        private readonly Collider2D[] groundHits = new Collider2D[8];
        private ContactFilter2D groundFilter;
        private SlimeJumpController slimeJump;
        private TraitSlotContainer traits;
        private bool jumpPressedThisFrame;

        public bool HasSlimeAbility => slimeJump != null && slimeJump.HasSlimeAbility;
        public bool IsCompressing => slimeJump != null && slimeJump.IsCompressing;
        public bool IsChargeReady => slimeJump != null && slimeJump.IsChargeReady;
        public bool HasControl { get; set; } = true;
        public bool IsGrounded { get; private set; }

        private bool HasForceAffected => traits == null || traits.HasTrait(TraitType.ForceAffected);

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
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

            IsGrounded = CheckGrounded();
            Vector2 moveInput = ReadMoveInput();
            bool canUseForceMovement = HasForceAffected;
            bool downHeld = canUseForceMovement && moveInput.y < -0.5f;
            bool jumpRequested = canUseForceMovement && jumpPressedThisFrame;

            JumpAction action = slimeJump.Tick(Time.fixedDeltaTime, IsGrounded, downHeld, jumpRequested, HasControl && canUseForceMovement);
            jumpPressedThisFrame = false;

            if (IsCompressing)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
            else if (HasControl)
            {
                body.linearVelocity = new Vector2(moveInput.x * moveSpeed, body.linearVelocity.y);
            }

            switch (action)
            {
                case JumpAction.NormalJump:
                    ApplyJump(normalJumpVelocity);
                    normalJumpReleased?.Invoke();
                    break;
                case JumpAction.SlimeBigJump:
                    ApplyJump(slimeBigJumpVelocity);
                    slimeBigJumpReleased?.Invoke();
                    break;
                case JumpAction.SlimeCompressionStarted:
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

        public void GrantSlimeAbility()
        {
            EnsureInitialized();
            slimeJump?.GrantSlimeAbility();
        }

        public void ResetForLevel()
        {
            EnsureInitialized();
            slimeJump.ResetForLevel();
            jumpPressedThisFrame = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
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
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useTriggers = false;
            slimeJump = new SlimeJumpController(slimeChargeDuration);
            traits = GetComponent<TraitSlotContainer>();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            jumpPressedThisFrame = true;
        }

        private void ApplyJump(float velocity)
        {
            Vector2 currentVelocity = body.linearVelocity;
            currentVelocity.y = velocity;
            body.linearVelocity = currentVelocity;
        }

        private bool CheckGrounded()
        {
            if (groundCheck == null)
            {
                return false;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit != null && !hit.isTrigger && !IsOwnCollider(hit))
                {
                    return true;
                }
            }

            return false;
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
    }
}
