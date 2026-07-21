using CrawfisSoftware.TempleRun.GameConfig;

namespace CrawfisSoftware.TempleRun.PowerUps
{
    /// <summary>
    /// SpeedBoost power-up. Sets the Blackboard's active speed multiplier to the definition's
    /// magnitude while active, restoring 1.0 on removal.
    ///    Dependencies: Blackboard.ActiveSpeedMultiplier
    /// </summary>
    public sealed class SpeedBoostEffect : PowerUpEffectBase
    {
        public override PowerUpType Type => PowerUpType.SpeedBoost;

        public override void Apply(PowerUpContext ctx)
        {
            ctx.Board.ActiveSpeedMultiplier = ctx.Definition.Magnitude;
        }

        public override void Remove(PowerUpContext ctx)
        {
            ctx.Board.ActiveSpeedMultiplier = 1.0f;
        }
    }
}
