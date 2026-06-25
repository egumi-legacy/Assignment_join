using NUnit.Framework;
using UnityEngine;
using Traits;

public sealed class TraitSlotContainerTests
{
    [Test]
    public void PlayerDefaultsCreateExpectedSlots()
    {
        GameObject player = new GameObject("Player");
        TraitSlotContainer container = player.AddComponent<TraitSlotContainer>();
        TraitSlotPreset preset = player.AddComponent<TraitSlotPreset>();

        preset.ApplyPreset();

        Assert.AreEqual(3, container.Slots.Count);
        Assert.AreEqual(TraitType.Collidable, container.Slots[0].Trait);
        Assert.AreEqual(TraitType.ForceAffected, container.Slots[1].Trait);
        Assert.AreEqual(TraitType.Empty, container.Slots[2].Trait);

        Object.DestroyImmediate(player);
    }

    [Test]
    public void SelectionCyclesThroughSlots()
    {
        GameObject obj = new GameObject("Object");
        TraitSlotContainer container = obj.AddComponent<TraitSlotContainer>();
        container.ConfigureSlots(new[]
        {
            new TraitSlot(TraitType.Collidable),
            new TraitSlot(TraitType.Empty),
            new TraitSlot(TraitType.ForceAffected)
        });

        container.SelectNext(1);
        Assert.AreEqual(1, container.SelectedIndex);

        container.SelectNext(1);
        Assert.AreEqual(2, container.SelectedIndex);

        container.SelectNext(1);
        Assert.AreEqual(0, container.SelectedIndex);

        Object.DestroyImmediate(obj);
    }

    [Test]
    public void SwapExchangesTraitsBetweenContainers()
    {
        GameObject player = new GameObject("Player");
        TraitSlotContainer playerContainer = player.AddComponent<TraitSlotContainer>();
        playerContainer.ConfigureSlots(new[]
        {
            new TraitSlot(TraitType.Empty),
            new TraitSlot(TraitType.ForceAffected)
        });

        GameObject wall = new GameObject("Wall");
        TraitSlotContainer wallContainer = wall.AddComponent<TraitSlotContainer>();
        wallContainer.ConfigureSlots(new[]
        {
            new TraitSlot(TraitType.Collidable)
        });

        TraitSwapResult result = playerContainer.TrySwap(0, wallContainer, 0);

        Assert.AreEqual(TraitSwapResult.Success, result);
        Assert.AreEqual(TraitType.Collidable, playerContainer.Slots[0].Trait);
        Assert.AreEqual(TraitType.Empty, wallContainer.Slots[0].Trait);

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(wall);
    }

    [Test]
    public void LockedSlotPreventsSwap()
    {
        GameObject player = new GameObject("Player");
        TraitSlotContainer playerContainer = player.AddComponent<TraitSlotContainer>();
        playerContainer.ConfigureSlots(new[]
        {
            new TraitSlot(TraitType.Empty)
        });

        GameObject wall = new GameObject("Wall");
        TraitSlotContainer wallContainer = wall.AddComponent<TraitSlotContainer>();
        wallContainer.ConfigureSlots(new[]
        {
            new TraitSlot(TraitType.Collidable, locked: true)
        });

        TraitSwapResult result = playerContainer.TrySwap(0, wallContainer, 0);

        Assert.AreEqual(TraitSwapResult.LockedSlot, result);
        Assert.AreEqual(TraitType.Empty, playerContainer.Slots[0].Trait);
        Assert.AreEqual(TraitType.Collidable, wallContainer.Slots[0].Trait);

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(wall);
    }
}
