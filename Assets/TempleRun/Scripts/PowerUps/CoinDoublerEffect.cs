using CrawfisSoftware.TempleRun.GameConfig;

namespace CrawfisSoftware.TempleRun.PowerUps
{
    /// <summary>
    /// CoinDoubler power-up (phase A4 — proves the extension seam).
    /// Added as a single new file: implement the effect, append <see cref="PowerUpType.CoinDoubler"/>,
    /// register it in <see cref="PowerUpBuffController"/>'s effect list, and author a
    /// PowerUpDefinition asset with Type = CoinDoubler. No other controller logic changes.
    ///
    /// Behaviour: doubles the active score multiplier (magnitude x2) while active, restoring 1.0 on
    /// removal. Reuses <see cref="Blackboard.ActiveScoreMultiplier"/> so no new Blackboard field is
    /// needed. Dormant until a PowerUpDefinition of Type CoinDoubler is collected, so it does not
    /// alter existing behaviour.
    ///    Dependencies: Blackboard.ActiveScoreMultiplier
    /// </summary>
    public sealed class CoinDoublerEffect : PowerUpEffectBase
    {
        public override PowerUpType Type => PowerUpType.CoinDoubler;

        public override void Apply(PowerUpContext ctx)
        {
            ctx.Board.ActiveScoreMultiplier = ctx.Definition.Magnitude * 2f;
        }

        public override void Remove(PowerUpContext ctx)
        {
            ctx.Board.ActiveScoreMultiplier = 1.0f;
        }
    }
}
