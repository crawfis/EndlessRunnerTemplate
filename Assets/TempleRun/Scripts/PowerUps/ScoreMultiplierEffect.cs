using CrawfisSoftware.TempleRun.GameConfig;

namespace CrawfisSoftware.TempleRun.PowerUps
{
    /// <summary>
    /// ScoreMultiplier power-up. Sets the Blackboard's active score multiplier to the
    /// definition's magnitude while active, restoring 1.0 on removal.
    ///    Dependencies: Blackboard.ActiveScoreMultiplier
    /// </summary>
    public sealed class ScoreMultiplierEffect : PowerUpEffectBase
    {
        public override PowerUpType Type => PowerUpType.ScoreMultiplier;

        public override void Apply(PowerUpContext ctx)
        {
            ctx.Board.ActiveScoreMultiplier = ctx.Definition.Magnitude;
        }

        public override void Remove(PowerUpContext ctx)
        {
            ctx.Board.ActiveScoreMultiplier = 1.0f;
        }
    }
}
