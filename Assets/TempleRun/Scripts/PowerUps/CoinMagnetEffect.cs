using CrawfisSoftware.TempleRun.GameConfig;

namespace CrawfisSoftware.TempleRun.PowerUps
{
    /// <summary>
    /// CoinMagnet power-up. Enables coin attraction and sets the magnet radius to the
    /// definition's magnitude while active; disables it (radius 0) on removal.
    ///    Dependencies: Blackboard.CoinMagnetActive, Blackboard.CoinMagnetRadius
    /// </summary>
    public sealed class CoinMagnetEffect : PowerUpEffectBase
    {
        public override PowerUpType Type => PowerUpType.CoinMagnet;

        public override void Apply(PowerUpContext ctx)
        {
            ctx.Board.CoinMagnetActive = true;
            ctx.Board.CoinMagnetRadius = ctx.Definition.Magnitude;
        }

        public override void Remove(PowerUpContext ctx)
        {
            ctx.Board.CoinMagnetActive = false;
            ctx.Board.CoinMagnetRadius = 0f;
        }
    }
}
