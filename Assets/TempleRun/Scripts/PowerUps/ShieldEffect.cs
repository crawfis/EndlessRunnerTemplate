using CrawfisSoftware.TempleRun.GameConfig;

namespace CrawfisSoftware.TempleRun.PowerUps
{
    /// <summary>
    /// Shield power-up. While active, absorbs a single obstacle hit: TryAbsorbObstacle publishes
    /// ObstacleRecovered and reports the hit as handled. Apply/Remove toggle the Blackboard flag
    /// (kept for any consumers that still read ShieldActive, e.g. VFX/UI).
    ///    Dependencies: Blackboard.ShieldActive
    ///    Publishes: TempleRunEvents.ObstacleRecovered (when it absorbs a hit)
    /// </summary>
    public sealed class ShieldEffect : PowerUpEffectBase
    {
        public override PowerUpType Type => PowerUpType.Shield;

        public override void Apply(PowerUpContext ctx)
        {
            ctx.Board.ShieldActive = true;
        }

        public override void Remove(PowerUpContext ctx)
        {
            ctx.Board.ShieldActive = false;
        }

        public override bool TryAbsorbObstacle(PowerUpContext ctx)
        {
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.ObstacleRecovered, this, null);
            return true;
        }
    }
}
