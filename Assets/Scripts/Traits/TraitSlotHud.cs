using System.Text;
using UnityEngine;

namespace Traits
{
    public sealed class TraitSlotHud : MonoBehaviour
    {
        [SerializeField] private TraitInteractionController interaction;
        [SerializeField] private Vector2 position = new Vector2(16f, 16f);
        [SerializeField] private float width = 420f;

        private GUIStyle boxStyle;
        private GUIStyle selectedStyle;
        private GUIStyle normalStyle;

        private void Awake()
        {
            if (interaction == null)
            {
                interaction = FindFirstObjectByType<TraitInteractionController>();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (interaction == null)
            {
                return;
            }

            float margin = Mathf.Max(8f, position.x);
            float top = Mathf.Max(8f, position.y);
            float panelWidth = Mathf.Min(width, Mathf.Max(220f, (Screen.width - margin * 3f) * 0.5f));
            float panelHeight = 280f;

            StringBuilder playerBuilder = new StringBuilder();
            playerBuilder.AppendLine("主角特性槽");
            playerBuilder.AppendLine();
            AppendContainer(playerBuilder, interaction.PlayerTraits);
            GUI.Box(new Rect(margin, top, panelWidth, panelHeight), playerBuilder.ToString(), boxStyle);

            StringBuilder objectBuilder = new StringBuilder();
            if (interaction.HoveredTraits != null)
            {
                objectBuilder.AppendLine("物体特性槽");
                objectBuilder.AppendLine();
                AppendContainer(objectBuilder, interaction.HoveredTraits);
            }
            else
            {
                objectBuilder.AppendLine("物体特性槽");
                objectBuilder.AppendLine();
                objectBuilder.AppendLine("将鼠标移动到物体上查看特性");
            }

            GUI.Box(new Rect(Screen.width - panelWidth - margin, top, panelWidth, panelHeight), objectBuilder.ToString(), boxStyle);

            StringBuilder hintBuilder = new StringBuilder();
            hintBuilder.AppendLine("滚轮：选择槽    出现绿色虚线时可 Shift 交换 / E 获取到空槽    R：重开");
            if (ShouldShowSlimeHint())
            {
                hintBuilder.AppendLine("史莱姆：拥有史莱姆状态时按住下蓄力，变色后按跳可以大跳");
            }

            if (!string.IsNullOrEmpty(interaction.FeedbackMessage))
            {
                hintBuilder.AppendLine(interaction.FeedbackMessage);
            }

            float hintHeight = 28f + hintBuilder.ToString().Split('\n').Length * 22f;
            GUI.Box(new Rect(margin, Screen.height - hintHeight - margin, Screen.width - margin * 2f, hintHeight), hintBuilder.ToString(), boxStyle);
        }

        private bool ShouldShowSlimeHint()
        {
            return HasSlime(interaction.PlayerTraits) || HasSlime(interaction.HoveredTraits);
        }

        private static bool HasSlime(TraitSlotContainer container)
        {
            return container != null && container.HasTrait(TraitType.Slime);
        }

        private static void AppendContainer(StringBuilder builder, TraitSlotContainer container)
        {
            if (container == null || container.Slots.Count == 0)
            {
                builder.AppendLine("  （无特性槽）");
                return;
            }

            for (int i = 0; i < container.Slots.Count; i++)
            {
                TraitSlot slot = container.Slots[i];
                string selected = i == container.SelectedIndex ? ">" : " ";
                string locked = slot.Locked ? " 🔒" : string.Empty;
                builder.AppendLine($"  {selected} [{i + 1}] {FormatTrait(slot.Trait)}{locked}");
                builder.AppendLine($"      {FormatTraitDescription(slot.Trait)}");
            }
        }

        private static string FormatTrait(TraitType trait)
        {
            switch (trait)
            {
                case TraitType.Collidable:
                    return "可碰撞";
                case TraitType.ForceAffected:
                    return "可受力";
                case TraitType.Slime:
                    return "史莱姆";
                default:
                    return "空槽";
            }
        }

        private static string FormatTraitDescription(TraitType trait)
        {
            switch (trait)
            {
                case TraitType.Collidable:
                    return "阻挡/站立，可作为实体平台";
                case TraitType.ForceAffected:
                    return "受重力/推力影响，可常规跳跃";
                case TraitType.Slime:
                    return "绿色弹性；按住下蓄力后大跳";
                default:
                    return "可接收其他特性";
            }
        }

        private void EnsureStyles()
        {
            if (boxStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 16,
                padding = new RectOffset(12, 12, 12, 12)
            };
            selectedStyle = new GUIStyle(GUI.skin.label);
            normalStyle = new GUIStyle(GUI.skin.label);
        }
    }
}
