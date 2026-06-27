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

        [Header("Range Feedback")]
        [SerializeField] private bool showInRangeLine = true;
        [SerializeField] private Color inRangeLineColor = new Color(0.35f, 1f, 0.35f, 0.95f);
        [SerializeField] private float lineWidth = 0.045f;
        [SerializeField] private float dashLength = 0.22f;
        [SerializeField] private float dashGap = 0.14f;
        [SerializeField] private int maxDashSegments = 32;
        [SerializeField] private float playerAnchorYOffset = 0.35f;
        [SerializeField] private string lineSortingLayerName = "Default";
        [SerializeField] private int lineSortingOrder = 20;

        private bool wasShiftHeld;
        private bool wasQuickTakeHeld;
        private string feedbackMessage;
        private float feedbackUntil;
        private Collider2D hoveredCollider;
        private Vector2 interactionClosestPoint;
        private float interactionDistance;
        private bool hoveredTargetInRange;
        private Transform lineRoot;
        private LineRenderer[] lineSegments;
        private Material lineMaterial;

        public TraitSlotContainer PlayerTraits => playerTraits;
        public TraitSlotContainer HoveredTraits { get; private set; }
        public Collider2D HoveredCollider => hoveredCollider;
        public Vector2 InteractionClosestPoint => interactionClosestPoint;
        public float InteractionDistance => interactionDistance;
        public bool HoveredTargetInRange => hoveredTargetInRange;
        public float MaxSwapDistance => Mathf.Max(0f, maxSwapDistance);
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
            HandleQuickTakeInput();
            UpdateInteractionLine();
        }

        public void ClearHover()
        {
            ClearHoverTarget();
            wasShiftHeld = false;
            wasQuickTakeHeld = false;
            HideInteractionLine();
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
            ClearHoverTarget();
            if (worldCamera == null || Mouse.current == null)
            {
                return;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition, hoverLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                TraitSlotContainer candidate = hit.GetComponentInParent<TraitSlotContainer>();
                if (candidate != null && candidate != playerTraits)
                {
                    HoveredTraits = candidate;
                    hoveredCollider = hit;
                    RefreshInteractionDistance();
                    return;
                }
            }
        }

        private void ClearHoverTarget()
        {
            HoveredTraits = null;
            hoveredCollider = null;
            interactionClosestPoint = Vector2.zero;
            interactionDistance = 0f;
            hoveredTargetInRange = false;
        }

        private void RefreshInteractionDistance()
        {
            if (HoveredTraits == null || hoveredCollider == null || playerTransform == null)
            {
                hoveredTargetInRange = false;
                return;
            }

            Vector2 playerAnchor = GetPlayerAnchorPosition();
            interactionClosestPoint = hoveredCollider.ClosestPoint(playerAnchor);
            interactionDistance = Vector2.Distance(playerAnchor, interactionClosestPoint);
            hoveredTargetInRange = interactionDistance <= MaxSwapDistance;
        }

        private Vector2 GetPlayerAnchorPosition()
        {
            if (playerTransform == null)
            {
                return transform.position;
            }

            Vector3 position = playerTransform.position;
            position.y += playerAnchorYOffset;
            return position;
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

        private void HandleQuickTakeInput()
        {
            if (Keyboard.current == null || playerTraits == null)
            {
                return;
            }

            bool quickTakeHeld = Keyboard.current.eKey.isPressed;
            if (quickTakeHeld && !wasQuickTakeHeld)
            {
                if (!IsHoveredTargetInRange())
                {
                    ShowFeedback("距离太远，不能获取特性");
                }
                else if (HoveredTraits == null)
                {
                    ShowFeedback("需要先悬停一个有特性的物体");
                }
                else
                {
                    TraitSwapResult result = HoveredTraits.TryMoveSelectedTraitToFirstEmpty(playerTraits);
                    ShowFeedback(result);
                }
            }

            wasQuickTakeHeld = quickTakeHeld;
        }

        private bool IsHoveredTargetInRange()
        {
            if (HoveredTraits == null)
            {
                return true;
            }

            if (hoveredCollider == null || playerTransform == null)
            {
                return false;
            }

            return hoveredTargetInRange;
        }

        private void UpdateInteractionLine()
        {
            if (!showInRangeLine || !hoveredTargetInRange || HoveredTraits == null || hoveredCollider == null || playerTransform == null)
            {
                HideInteractionLine();
                return;
            }

            EnsureLineSegments();
            Vector2 start = GetPlayerAnchorPosition();
            Vector2 end = interactionClosestPoint;
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.01f)
            {
                HideInteractionLine();
                return;
            }

            Vector2 direction = delta / distance;
            float cursor = 0f;
            int segmentIndex = 0;
            float safeDashLength = Mathf.Max(0.01f, dashLength);
            float safeDashGap = Mathf.Max(0f, dashGap);
            while (cursor < distance && segmentIndex < lineSegments.Length)
            {
                float next = Mathf.Min(cursor + safeDashLength, distance);
                LineRenderer segment = lineSegments[segmentIndex];
                segment.enabled = true;
                segment.startColor = inRangeLineColor;
                segment.endColor = inRangeLineColor;
                segment.startWidth = lineWidth;
                segment.endWidth = lineWidth;
                segment.SetPosition(0, start + direction * cursor);
                segment.SetPosition(1, start + direction * next);
                cursor = next + safeDashGap;
                segmentIndex++;
            }

            for (int i = segmentIndex; i < lineSegments.Length; i++)
            {
                lineSegments[i].enabled = false;
            }
        }

        private void EnsureLineSegments()
        {
            int segmentCount = Mathf.Max(1, maxDashSegments);
            if (lineSegments != null && lineSegments.Length == segmentCount)
            {
                return;
            }

            if (lineRoot == null)
            {
                GameObject root = new GameObject("TraitInteractionDashedLine");
                root.transform.SetParent(transform, false);
                lineRoot = root.transform;
            }

            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }

                lineMaterial = shader != null ? new Material(shader) : null;
            }

            if (lineSegments != null)
            {
                for (int i = 0; i < lineSegments.Length; i++)
                {
                    if (lineSegments[i] != null)
                    {
                        Destroy(lineSegments[i].gameObject);
                    }
                }
            }

            lineSegments = new LineRenderer[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                GameObject segmentObject = new GameObject($"DashSegment{i + 1:00}");
                segmentObject.transform.SetParent(lineRoot, false);
                LineRenderer segment = segmentObject.AddComponent<LineRenderer>();
                segment.positionCount = 2;
                segment.useWorldSpace = true;
                segment.textureMode = LineTextureMode.Stretch;
                segment.alignment = LineAlignment.View;
                segment.numCapVertices = 2;
                segment.startWidth = lineWidth;
                segment.endWidth = lineWidth;
                segment.startColor = inRangeLineColor;
                segment.endColor = inRangeLineColor;
                segment.sortingLayerName = lineSortingLayerName;
                segment.sortingOrder = lineSortingOrder;
                if (lineMaterial != null)
                {
                    segment.material = lineMaterial;
                }

                segment.enabled = false;
                lineSegments[i] = segment;
            }
        }

        private void HideInteractionLine()
        {
            if (lineSegments == null)
            {
                return;
            }

            for (int i = 0; i < lineSegments.Length; i++)
            {
                if (lineSegments[i] != null)
                {
                    lineSegments[i].enabled = false;
                }
            }
        }

        private void ShowFeedback(TraitSwapResult result)
        {
            switch (result)
            {
                case TraitSwapResult.Success:
                    ShowFeedback("操作成功");
                    break;
                case TraitSwapResult.LockedSlot:
                    ShowFeedback("该特性槽已锁定，不能移动或交换");
                    break;
                case TraitSwapResult.MissingContainer:
                    ShowFeedback("需要先悬停一个有特性的物体");
                    break;
                case TraitSwapResult.EmptySourceSlot:
                    ShowFeedback("选中的物体槽是空槽");
                    break;
                case TraitSwapResult.NoEmptySlot:
                    ShowFeedback("主角没有可用空槽");
                    break;
                default:
                    ShowFeedback("不能操作当前特性槽");
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
