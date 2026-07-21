using CrawfisSoftware.TempleRun.GameConfig;

namespace CrawfisSoftware.TempleRun.PowerUps
{
    /// <summary>
    /// A self-contained power-up strategy. Each concrete effect owns the "what happens"
    /// for a single <see cref="PowerUpType"/>: it writes/clears the relevant Blackboard buff
    /// state on Apply/Remove and may optionally intercept an obstacle hit.
    /// The <see cref="PowerUpBuffController"/> keeps the cross-cutting responsibilities
    /// (registry, duration timers, re-collect-resets-timer rule, event plumbing).
    ///    Dependencies: PowerUpContext (Blackboard + PowerUpDefinition)
    /// </summary>
    public interface IPowerUpEffect
    {
        /// <summary>The power-up type this effect handles. Used as the registry key.</summary>
        PowerUpType Type { get; }

        /// <summary>Called on activation (and on re-activation after a Remove when re-collected).</summary>
        void Apply(PowerUpContext ctx);

        /// <summary>Called on expiry / cleanup to restore the default (un-buffed) state.</summary>
        void Remove(PowerUpContext ctx);

        /// <summary>
        /// Optional hook letting an effect (e.g. Shield) absorb an obstacle hit without the
        /// controller special-casing it. Return true if the hit was absorbed (and publish any
        /// recovery event); the default is a no-op that lets the hit fall through to failure.
        /// </summary>
        bool TryAbsorbObstacle(PowerUpContext ctx) => false;
    }

    /// <summary>
    /// Immutable state an <see cref="IPowerUpEffect"/> reads/writes: the shared runtime
    /// <see cref="Blackboard"/> and the <see cref="PowerUpDefinition"/> driving this activation.
    /// <see cref="Definition"/> may be null on the obstacle-hit / cleanup paths, where no single
    /// definition is in scope; effects invoked there (Remove, TryAbsorbObstacle) must not read it.
    /// </summary>
    public readonly struct PowerUpContext
    {
        public readonly Blackboard Board;
        public readonly PowerUpDefinition Definition;

        public PowerUpContext(Blackboard board, PowerUpDefinition definition)
        {
            Board = board;
            Definition = definition;
        }
    }
}
