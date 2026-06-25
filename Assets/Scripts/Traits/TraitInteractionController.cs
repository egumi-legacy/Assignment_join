using UnityEngine;
using UnityEngine.InputSystem;

namespace Traits
{
    public sealed class TraitInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TraitSlotContainer playerTraits;
        [SerializeField] private LayerMask hoverLayers = ~0;

        private bool wasShiftHeld;
        private string feedbackMessage;
        private float feedbackUntil;

        public TraitSlotContainer PlayerTraits => playerTraits;
        public TraitSlotContainer HoveredTraits { get; private set; }
        public string FeedbackMessage => Time.time <= feedbackUntil ? feedbackMessage : string.Empty;

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (playerTraits == null)
            {
                playerTraits = GetComponent<TraitSlotContainer>();
            }
        }

        private void Update()
        {
            UpdateHoverTarget();
            HandleScrollSelection();
            HandleSwapInput();
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
                TraitSwapResult result = playerTraits.TrySwapSelectedWith(HoveredTraits);
                ShowFeedback(result);
            }

            wasShiftHeld = shiftHeld;
        }

        private void ShowFeedback(TraitSwapResult result)
        {
            switch (result)
            {
                case TraitSwapResult.Success:
                    feedbackMessage = "交换成功";
                    break;
                case TraitSwapResult.LockedSlot:
                    feedbackMessage = "该特性槽已锁定，不能交换";
                    break;
                case TraitSwapResult.MissingContainer:
                    feedbackMessage = "需要先悬停一个有特性的物体";
                    break;
                default:
                    feedbackMessage = "不能交换当前特性槽";
                    break;
            }

            feedbackUntil = Time.time + 1.5f;
        }
    }
}
