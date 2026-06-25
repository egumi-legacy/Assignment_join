using UnityEngine;
using UnityEngine.InputSystem;

namespace Traits
{
    public sealed class TraitInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TraitSlotContainer playerTraits;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private LayerMask hoverLayers = ~0;
        [SerializeField] private float maxSwapDistance = 4f;

        private bool wasShiftHeld;
        private string feedbackMessage;
        private float feedbackUntil;

        public TraitSlotContainer PlayerTraits => playerTraits;
        public TraitSlotContainer HoveredTraits { get; private set; }
        public string FeedbackMessage => Time.time <= feedbackUntil ? feedbackMessage : string.Empty;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveCameraIfNeeded();
            UpdateHoverTarget();
            HandleScrollSelection();
            HandleSwapInput();
        }

        public void ClearHover()
        {
            HoveredTraits = null;
            wasShiftHeld = false;
        }

        private void ResolveReferences()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (playerTraits == null)
            {
                playerTraits = GetComponent<TraitSlotContainer>();
            }

            if (playerTransform == null && playerTraits != null)
            {
                playerTransform = playerTraits.transform;
            }
        }

        private void ResolveCameraIfNeeded()
        {
            if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            {
                worldCamera = Camera.main;
            }
        }

        private void UpdateHoverTarget()
        {
            HoveredTraits = null;
            if (worldCamera == null || Mouse.current == null)
            {
                return;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition, hoverLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                TraitSlotContainer candidate = hits[i].GetComponentInParent<TraitSlotContainer>();
                if (candidate != null && candidate != playerTraits)
                {
                    HoveredTraits = candidate;
                    return;
                }
            }
        }

        private void HandleScrollSelection()
        {
            if (Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            int direction = scroll > 0f ? -1 : 1;
            if (HoveredTraits != null)
            {
                HoveredTraits.SelectNext(direction);
            }
            else if (playerTraits != null)
            {
                playerTraits.SelectNext(direction);
            }
        }

        private void HandleSwapInput()
        {
            if (Keyboard.current == null || playerTraits == null)
            {
                return;
            }

            bool shiftHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            if (shiftHeld && !wasShiftHeld)
            {
                if (!IsHoveredTargetInRange())
                {
                    ShowFeedback("距离太远，不能交换");
                }
                else
                {
                    TraitSwapResult result = playerTraits.TrySwapSelectedWith(HoveredTraits);
                    ShowFeedback(result);
                }
            }

            wasShiftHeld = shiftHeld;
        }

        private bool IsHoveredTargetInRange()
        {
            if (HoveredTraits == null || playerTransform == null)
            {
                return true;
            }

            return Vector2.Distance(playerTransform.position, HoveredTraits.transform.position) <= Mathf.Max(0f, maxSwapDistance);
        }

        private void ShowFeedback(TraitSwapResult result)
        {
            switch (result)
            {
                case TraitSwapResult.Success:
                    ShowFeedback("交换成功");
                    break;
                case TraitSwapResult.LockedSlot:
                    ShowFeedback("该特性槽已锁定，不能交换");
                    break;
                case TraitSwapResult.MissingContainer:
                    ShowFeedback("需要先悬停一个有特性的物体");
                    break;
                default:
                    ShowFeedback("不能交换当前特性槽");
                    break;
            }
        }

        private void ShowFeedback(string message)
        {
            feedbackMessage = message;
            feedbackUntil = Time.time + 1.5f;
        }
    }
}
