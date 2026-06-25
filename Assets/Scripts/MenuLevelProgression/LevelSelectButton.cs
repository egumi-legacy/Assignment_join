using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MenuLevelProgression
{
    public sealed class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private string lockedSuffix = " (Locked)";

        private void Reset()
        {
            button = GetComponent<Button>();
            label = GetComponentInChildren<Text>();
        }

        public void Bind(string displayName, bool unlocked, UnityAction onClick)
        {
            if (label != null)
            {
                label.text = unlocked ? displayName : displayName + lockedSuffix;
            }

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = unlocked;
            if (unlocked && onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
        }
    }
}
