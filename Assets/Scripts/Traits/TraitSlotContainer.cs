using System;
using System.Collections.Generic;
using UnityEngine;

namespace Traits
{
    public sealed class TraitSlotContainer : MonoBehaviour
    {
        [SerializeField] private bool initializePlayerDefaults;
        [SerializeField] private string displayName;
        [SerializeField] private List<TraitSlot> slots = new List<TraitSlot>();
        [SerializeField] private int selectedIndex;

        public event Action<TraitSlotContainer> SlotsChanged;
        public event Action<TraitSlotContainer> SelectionChanged;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public IReadOnlyList<TraitSlot> Slots => slots;
        public int SelectedIndex => selectedIndex;
        public TraitSlot SelectedSlot => IsValidIndex(selectedIndex) ? slots[selectedIndex] : null;

        private void Awake()
        {
            EnsureDefaultPlayerSlotsIfEmpty();
            ClampSelection();
        }

        public void ConfigureSlots(IEnumerable<TraitSlot> newSlots)
        {
            slots.Clear();
            slots.AddRange(newSlots);
            ClampSelection();
            NotifyChanged();
        }

        public bool HasTrait(TraitType trait)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Trait == trait)
                {
                    return true;
                }
            }

            return false;
        }

        public void SelectNext(int direction)
        {
            if (slots.Count == 0 || direction == 0)
            {
                return;
            }

            selectedIndex = PositiveModulo(selectedIndex + Math.Sign(direction), slots.Count);
            SelectionChanged?.Invoke(this);
        }

        public TraitSwapResult TrySwapSelectedWith(TraitSlotContainer other)
        {
            if (other == null)
            {
                return TraitSwapResult.MissingContainer;
            }

            return TrySwap(selectedIndex, other, other.selectedIndex);
        }

        public TraitSwapResult TrySwap(int ownIndex, TraitSlotContainer other, int otherIndex)
        {
            if (other == null)
            {
                return TraitSwapResult.MissingContainer;
            }

            if (!IsValidIndex(ownIndex) || !other.IsValidIndex(otherIndex))
            {
                return TraitSwapResult.InvalidSlot;
            }

            TraitSlot ownSlot = slots[ownIndex];
            TraitSlot otherSlot = other.slots[otherIndex];
            if (ownSlot.Locked || otherSlot.Locked)
            {
                return TraitSwapResult.LockedSlot;
            }

            TraitType previousOwnTrait = ownSlot.Trait;
            ownSlot.Trait = otherSlot.Trait;
            otherSlot.Trait = previousOwnTrait;

            NotifyChanged();
            other.NotifyChanged();
            return TraitSwapResult.Success;
        }

        public void NotifyChanged()
        {
            SlotsChanged?.Invoke(this);
        }

        private void EnsureDefaultPlayerSlotsIfEmpty()
        {
            if (slots.Count > 0 || !initializePlayerDefaults)
            {
                return;
            }

            slots.Add(new TraitSlot(TraitType.Collidable));
            slots.Add(new TraitSlot(TraitType.ForceAffected));
            slots.Add(new TraitSlot(TraitType.Empty));
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < slots.Count;
        }

        private void ClampSelection()
        {
            if (slots.Count == 0)
            {
                selectedIndex = 0;
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, slots.Count - 1);
        }

        private static int PositiveModulo(int value, int length)
        {
            return ((value % length) + length) % length;
        }
    }
}
