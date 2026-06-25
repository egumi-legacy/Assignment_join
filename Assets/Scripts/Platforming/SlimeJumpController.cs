using System;

namespace Platforming
{
    public enum JumpAction
    {
        None,
        NormalJump,
        SlimeBigJump,
        SlimeCompressionStarted,
        SlimeCompressionReady,
        SlimeCompressionCancelled
    }

    public sealed class SlimeJumpController
    {
        private readonly float chargeDuration;

        private bool wasDownHeld;
        private bool hasQueuedJump;
        private float chargeTimer;

        public SlimeJumpController(float chargeDuration)
        {
            if (chargeDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(chargeDuration), "Charge duration must be greater than zero.");
            }

            this.chargeDuration = chargeDuration;
        }

        public bool IsCompressing { get; private set; }
        public bool IsChargeReady { get; private set; }
        public bool HasQueuedJump => hasQueuedJump;
        public float ChargeProgress => Math.Min(chargeTimer / chargeDuration, 1f);

        public void ResetForLevel()
        {
            CancelCompression();
            wasDownHeld = false;
        }

        public JumpAction CancelForTraitLoss()
        {
            hasQueuedJump = false;
            return CancelCompression() ? JumpAction.SlimeCompressionCancelled : JumpAction.None;
        }

        public JumpAction StartCompressionFromSurface()
        {
            if (IsCompressing)
            {
                return JumpAction.None;
            }

            StartCompression();
            wasDownHeld = true;
            return JumpAction.SlimeCompressionStarted;
        }

        public JumpAction Tick(float deltaTime, bool isGrounded, bool downHeld, bool jumpPressed, bool hasControl, bool hasSlimeAbility)
        {
            if (!hasControl || !isGrounded)
            {
                wasDownHeld = downHeld;
                return CancelCompression() ? JumpAction.SlimeCompressionCancelled : JumpAction.None;
            }

            if (!hasSlimeAbility)
            {
                wasDownHeld = downHeld;
                if (CancelCompression())
                {
                    return JumpAction.SlimeCompressionCancelled;
                }

                return jumpPressed && !downHeld ? JumpAction.NormalJump : JumpAction.None;
            }

            if (IsCompressing)
            {
                if (!downHeld)
                {
                    wasDownHeld = false;
                    return CancelCompression() ? JumpAction.SlimeCompressionCancelled : JumpAction.None;
                }

                if (jumpPressed && !IsChargeReady)
                {
                    hasQueuedJump = true;
                }

                if (!IsChargeReady)
                {
                    chargeTimer += Math.Max(deltaTime, 0f);
                    if (chargeTimer >= chargeDuration)
                    {
                        IsChargeReady = true;
                        if (hasQueuedJump)
                        {
                            ClearCompression();
                            wasDownHeld = downHeld;
                            return JumpAction.SlimeBigJump;
                        }

                        wasDownHeld = downHeld;
                        return JumpAction.SlimeCompressionReady;
                    }
                }

                if (jumpPressed && IsChargeReady)
                {
                    ClearCompression();
                    wasDownHeld = downHeld;
                    return JumpAction.SlimeBigJump;
                }

                wasDownHeld = downHeld;
                return JumpAction.None;
            }

            if (downHeld && !wasDownHeld)
            {
                StartCompression();
                if (jumpPressed)
                {
                    hasQueuedJump = true;
                }

                wasDownHeld = downHeld;
                return JumpAction.SlimeCompressionStarted;
            }

            wasDownHeld = downHeld;

            if (jumpPressed)
            {
                return JumpAction.NormalJump;
            }

            return JumpAction.None;
        }

        private void StartCompression()
        {
            IsCompressing = true;
            IsChargeReady = false;
            hasQueuedJump = false;
            chargeTimer = 0f;
        }

        private bool CancelCompression()
        {
            if (!IsCompressing)
            {
                return false;
            }

            ClearCompression();
            return true;
        }

        private void ClearCompression()
        {
            IsCompressing = false;
            IsChargeReady = false;
            hasQueuedJump = false;
            chargeTimer = 0f;
        }
    }
}
