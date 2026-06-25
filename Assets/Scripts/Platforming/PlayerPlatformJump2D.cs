using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Platforming
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerPlatformJump2D : MonoBehaviour
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
        private bool jumpPressedThisFrame;

        public bool HasSlimeAbility => slimeJump != null && slimeJump.HasSlimeAbility;
        public bool IsCompressing => slimeJump != null && slimeJump.IsCompressing;
        public bool IsChargeReady => slimeJump != null && slimeJump.IsChargeReady;
        public bool HasControl { get; set; } = true;
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownColliders = GetComponentsInChildren<Collider2D>();
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useTriggers = false;
            slimeJump = new SlimeJumpController(slimeChargeDuration);
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

        private void FixedUpdate()
        {
            IsGrounded = CheckGrounded();
            Vector2 moveInput = ReadMoveInput();
            bool downHeld = moveInput.y < -0.5f;

            JumpAction action = slimeJump.Tick(Time.fixedDeltaTime, IsGrounded, downHeld, jumpPressedThisFrame, HasControl);
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
            slimeJump.GrantSlimeAbility();
        }

        public void ResetForLevel()
        {
            slimeJump.ResetForLevel();
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

            int hitCount = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundFilter, groundHits);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = groundHits[i];
                if (hit != null && !IsOwnCollider(hit))
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
            if (moveAction == null)
            {
                return Vector2.zero;
            }

            return moveAction.action.ReadValue<Vector2>();
        }
    }
}
