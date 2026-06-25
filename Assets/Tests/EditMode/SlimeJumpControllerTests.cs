using NUnit.Framework;
using Platforming;

public sealed class SlimeJumpControllerTests
{
    [Test]
    public void GroundedJumpWithoutSlimeTraitReturnsNormalJump()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);

        JumpAction action = controller.Tick(0.02f, isGrounded: true, downHeld: false, jumpPressed: true, hasControl: true, hasSlimeAbility: false);

        Assert.AreEqual(JumpAction.NormalJump, action);
    }

    [Test]
    public void ResetForLevelClearsCompressionState()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);
        controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        controller.ResetForLevel();

        Assert.IsFalse(controller.IsCompressing);
        Assert.IsFalse(controller.HasQueuedJump);
    }

    [Test]
    public void HoldingDownWithSlimeTraitStartsCompression()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);

        JumpAction action = controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        Assert.AreEqual(JumpAction.SlimeCompressionStarted, action);
        Assert.IsTrue(controller.IsCompressing);
    }

    [Test]
    public void HoldingDownWithoutSlimeTraitDoesNotStartCompression()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);

        JumpAction action = controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: false);

        Assert.AreEqual(JumpAction.None, action);
        Assert.IsFalse(controller.IsCompressing);
    }

    [Test]
    public void AirbornePlayerCannotStartCompression()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);

        JumpAction action = controller.Tick(0.02f, isGrounded: false, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        Assert.AreEqual(JumpAction.None, action);
        Assert.IsFalse(controller.IsCompressing);
    }

    [Test]
    public void ReleasingDownCancelsCompression()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);
        controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        JumpAction action = controller.Tick(0.02f, isGrounded: true, downHeld: false, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        Assert.AreEqual(JumpAction.SlimeCompressionCancelled, action);
        Assert.IsFalse(controller.IsCompressing);
    }

    [Test]
    public void TraitLossCancelsCompression()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);
        controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        JumpAction action = controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: false);

        Assert.AreEqual(JumpAction.SlimeCompressionCancelled, action);
        Assert.IsFalse(controller.IsCompressing);
    }

    [Test]
    public void ReadyCompressionReleasesSlimeBigJumpOnJumpPress()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);
        controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);
        controller.Tick(0.25f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        JumpAction action = controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: true, hasControl: true, hasSlimeAbility: true);

        Assert.AreEqual(JumpAction.SlimeBigJump, action);
        Assert.IsFalse(controller.IsCompressing);
    }

    [Test]
    public void EarlyJumpIsQueuedAndReleasedWhenChargeCompletes()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);
        controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        JumpAction earlyAction = controller.Tick(0.05f, isGrounded: true, downHeld: true, jumpPressed: true, hasControl: true, hasSlimeAbility: true);
        JumpAction releaseAction = controller.Tick(0.20f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        Assert.AreEqual(JumpAction.None, earlyAction);
        Assert.AreEqual(JumpAction.SlimeBigJump, releaseAction);
    }

    [Test]
    public void QueuedJumpClearsWhenCompressionCancels()
    {
        SlimeJumpController controller = new SlimeJumpController(0.25f);
        controller.Tick(0.02f, isGrounded: true, downHeld: true, jumpPressed: false, hasControl: true, hasSlimeAbility: true);
        controller.Tick(0.05f, isGrounded: true, downHeld: true, jumpPressed: true, hasControl: true, hasSlimeAbility: true);

        JumpAction action = controller.Tick(0.02f, isGrounded: true, downHeld: false, jumpPressed: false, hasControl: true, hasSlimeAbility: true);

        Assert.AreEqual(JumpAction.SlimeCompressionCancelled, action);
        Assert.IsFalse(controller.HasQueuedJump);
    }
}
