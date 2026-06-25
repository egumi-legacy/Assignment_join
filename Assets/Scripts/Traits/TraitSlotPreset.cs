using UnityEngine;

namespace Traits
{
    [RequireComponent(typeof(TraitSlotContainer))]
    public sealed class TraitSlotPreset : MonoBehaviour
    {
        public enum PresetKind
        {
            PlayerDefaults,
            CollidableObject,
            LockedCollidableObject
        }

        [SerializeField] private PresetKind presetKind;

        private void Awake()
        {
            ApplyPreset();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ApplyPreset();
        }

        public void ApplyPreset()
        {
            TraitSlotContainer container = GetComponent<TraitSlotContainer>();
            switch (presetKind)
            {
                case PresetKind.PlayerDefaults:
                    container.ConfigureSlots(new[]
                    {
                        new TraitSlot(TraitType.Collidable),
                        new TraitSlot(TraitType.ForceAffected),
                        new TraitSlot(TraitType.Empty)
                    });
                    break;
                case PresetKind.LockedCollidableObject:
                    container.ConfigureSlots(new[]
                    {
                        new TraitSlot(TraitType.Collidable, locked: true)
                    });
                    break;
                default:
                    container.ConfigureSlots(new[]
                    {
                        new TraitSlot(TraitType.Collidable)
                    });
                    break;
            }
        }
    }
}
