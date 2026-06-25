using NUnit.Framework;
using Traits;

public sealed class TraitSlotTests
{
    [Test]
    public void SwapExchangesTraits()
    {
        TraitSlot player = new TraitSlot(TraitType.Empty);
        TraitSlot target = new TraitSlot(TraitType.Collidable);

        TraitType previous = player.Trait;
        player.Trait = target.Trait;
        target.Trait = previous;

        Assert.AreEqual(TraitType.Collidable, player.Trait);
        Assert.AreEqual(TraitType.Empty, target.Trait);
    }

    [Test]
    public void LockedSlotReportsLocked()
    {
        TraitSlot slot = new TraitSlot(TraitType.Collidable, locked: true);

        Assert.IsTrue(slot.Locked);
        Assert.IsFalse(slot.IsEmpty);
    }
}
