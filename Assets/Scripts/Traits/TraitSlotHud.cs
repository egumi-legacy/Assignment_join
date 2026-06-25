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

            StringBuilder playerBuilder = new StringBuilder();
            playerBuilder.AppendLine("主角特性槽");
            playerBuilder.AppendLine("滚轮：选择主角槽");
            playerBuilder.AppendLine("Shift：交换选中槽");
            playerBuilder.AppendLine();
            AppendContainer(playerBuilder, "主角", interaction.PlayerTraits);
            GUI.Box(new Rect(position.x, position.y, width, 180f), playerBuilder.ToString(), boxStyle);

            StringBuilder objectBuilder = new StringBuilder();
            if (interaction.HoveredTraits != null)
            {
                objectBuilder.AppendLine("物体特性槽");
                objectBuilder.AppendLine("悬停时滚轮：选择物体槽");
                objectBuilder.AppendLine();
                AppendContainer(objectBuilder, interaction.HoveredTraits.DisplayName, interaction.HoveredTraits);
            }
            else
            {
                objectBuilder.AppendLine("物体特性槽");
                objectBuilder.AppendLine("将鼠标移动到物体上查看特性");
            }

            GUI.Box(new Rect(Screen.width - width - position.x, position.y, width, 180f), objectBuilder.ToString(), boxStyle);

            if (!string.IsNullOrEmpty(interaction.FeedbackMessage))
            {
                GUI.Box(new Rect(position.x, position.y + 190f, width * 2f + 24f, 56f), interaction.FeedbackMessage, boxStyle);
            }
        }

        private static void AppendContainer(StringBuilder builder, string title, TraitSlotContainer container)
        {
            builder.AppendLine(title);
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
