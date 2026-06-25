using System;
using UnityEngine;

namespace Traits
{
    [Serializable]
    public sealed class TraitSlot
    {
        [SerializeField] private TraitType trait;
        [SerializeField] private bool locked;

        public TraitSlot(TraitType trait, bool locked = false)
        {
            this.trait = trait;
            this.locked = locked;
        }

        public TraitType Trait
        {
            get => trait;
            set => trait = value;
        }

        public bool Locked
        {
            get => locked;
            set => locked = value;
        }

        public bool IsEmpty => trait == TraitType.Empty;
    }
}
