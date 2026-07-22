using CrawfisSoftware.TempleRun.GameConfig;

namespace CrawfisSoftware.TempleRun.PowerUps
{
    /// <summary>
    /// Convenience base for <see cref="IPowerUpEffect"/> implementations. Requires concrete
    /// effects to supply <see cref="Type"/>, <see cref="Apply"/> and <see cref="Remove"/>, and
    /// provides a no-op <see cref="TryAbsorbObstacle"/> so only effects that need it override it.
    ///    Dependencies: PowerUpContext (Blackboard + PowerUpDefinition)
    /// </summary>
    public abstract class PowerUpEffectBase : IPowerUpEffect
    {
        public abstract PowerUpType Type { get; }

        public abstract void Apply(PowerUpContext ctx);

        public abstract void Remove(PowerUpContext ctx);

        public virtual bool TryAbsorbObstacle(PowerUpContext ctx) => false;
    }
}
