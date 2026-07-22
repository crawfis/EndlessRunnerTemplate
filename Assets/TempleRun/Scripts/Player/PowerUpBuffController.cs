using CrawfisSoftware.TempleRun.GameConfig;
using CrawfisSoftware.TempleRun.PowerUps;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Central buff manager and obstacle-hit gate.
    /// Delegates the per-type buff logic to <see cref="IPowerUpEffect"/> strategies (one per
    /// <see cref="PowerUpType"/>) and owns the cross-cutting concerns: the active-buff duration
    /// timers, the re-collect-resets-timer rule, and the event plumbing.
    /// Also gates ObstacleHit: it asks each active effect to TryAbsorbObstacle (Shield publishes
    /// ObstacleRecovered and absorbs the hit); if none absorb, it publishes PlayerFailingAtObstacle.
    ///    Dependencies: Blackboard, PowerUpDefinition, IPowerUpEffect
    ///    Subscribes: TempleRunEvents.PowerUpCollecting (destroy GO, publish PowerUpCollected)
    ///    Subscribes: TempleRunEvents.PowerUpActivating (apply buff)
    ///    Subscribes: TempleRunEvents.PowerUpDeactivating (remove buff)
    ///    Subscribes: TempleRunEvents.ObstacleHit (gate for Shield)
    ///    Subscribes: TempleRunEvents.TempleRunEnded (cleanup all buffs)
    ///    Publishes: TempleRunEvents.PowerUpCollected
    ///    Publishes: TempleRunEvents.PowerUpActivated
    ///    Publishes: TempleRunEvents.PowerUpDeactivated
    ///    Publishes: TempleRunEvents.PowerUpDeactivateRequested
    ///    Publishes: TempleRunEvents.PlayerFailingAtObstacle (when no effect absorbs)
    ///    Publishes: TempleRunEvents.ObstacleRecovered (via ShieldEffect when a shield absorbs a hit)
    /// </summary>
    internal class PowerUpBuffController : MonoBehaviour
    {
        private readonly Dictionary<PowerUpType, Coroutine> _activeBuffs = new();

        /// <summary>
        /// The known power-up strategies. Instantiated in code (rather than a
        /// <c>[SerializeReference]</c> list) so the registry is deterministic and needs no
        /// per-scene inspector wiring — see the M1 report for the trade-off. Add a new power-up by
        /// adding its effect class here (plus a PowerUpType value and a PowerUpDefinition asset).
        /// </summary>
        private static readonly IPowerUpEffect[] _effects =
        {
            new SpeedBoostEffect(),
            new CoinMagnetEffect(),
            new ShieldEffect(),
            new ScoreMultiplierEffect(),
            new CoinDoublerEffect(),
        };

        /// <summary>Registry keyed by <see cref="PowerUpType"/>, built from <see cref="_effects"/>.</summary>
        private readonly Dictionary<PowerUpType, IPowerUpEffect> _effectRegistry = new();

        private void Awake()
        {
            foreach (IPowerUpEffect effect in _effects)
            {
                _effectRegistry[effect.Type] = effect;
            }

            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.PowerUpCollecting, OnPowerUpCollecting);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.PowerUpActivating, OnPowerUpActivating);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.PowerUpDeactivating, OnPowerUpDeactivating);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.ObstacleHit, OnObstacleHit);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.TempleRunEnded, OnTempleRunEnded);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.PowerUpCollecting, OnPowerUpCollecting);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.PowerUpActivating, OnPowerUpActivating);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.PowerUpDeactivating, OnPowerUpDeactivating);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.ObstacleHit, OnObstacleHit);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.TempleRunEnded, OnTempleRunEnded);
        }

        /// <summary>
        /// Handles PowerUpCollecting: destroys the power-up GO and publishes PowerUpCollected.
        /// Data in: (PowerUpDefinition, GameObject) tuple from CollectableCollisionDetector.
        /// Data out: PowerUpDefinition (forwarded through auto-chain to PowerUpActivating).
        /// </summary>
        private void OnPowerUpCollecting(string eventName, object sender, object data)
        {
            if (data is not (PowerUpDefinition definition, GameObject powerUpGO)) return;

            if (powerUpGO != null)
            {
                Destroy(powerUpGO);
            }

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.PowerUpCollected, this, definition);
        }

        /// <summary>
        /// Applies the buff defined by the PowerUpDefinition to the Blackboard.
        /// If the same buff type is already active, resets its timer.
        /// </summary>
        private void OnPowerUpActivating(string eventName, object sender, object data)
        {
            PowerUpDefinition definition = data as PowerUpDefinition;
            if (definition == null) return;

            PowerUpContext ctx = new PowerUpContext(Blackboard.Instance, definition);

            // Cancel existing buff of the same type (reset timer)
            if (_activeBuffs.TryGetValue(definition.Type, out Coroutine existingCoroutine))
            {
                if (existingCoroutine != null)
                    StopCoroutine(existingCoroutine);
                RemoveEffect(definition.Type, ctx);
            }

            ApplyEffect(definition.Type, ctx);

            // Start duration timer
            Coroutine timerCoroutine = StartCoroutine(BuffDurationTimer(definition));
            _activeBuffs[definition.Type] = timerCoroutine;

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.PowerUpActivated, this, definition);
        }

        /// <summary>
        /// Removes the buff and publishes PowerUpDeactivated.
        /// </summary>
        private void OnPowerUpDeactivating(string eventName, object sender, object data)
        {
            PowerUpDefinition definition = data as PowerUpDefinition;
            if (definition == null) return;

            PowerUpContext ctx = new PowerUpContext(Blackboard.Instance, definition);
            RemoveEffect(definition.Type, ctx);
            _activeBuffs.Remove(definition.Type);

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.PowerUpDeactivated, this, definition);
        }

        /// <summary>
        /// Gates ObstacleHit: asks each currently-active effect to absorb the hit. If any absorbs
        /// it (Shield publishes ObstacleRecovered and returns true), the hit is handled. Otherwise
        /// forwards to PlayerFailingAtObstacle with the original sender/data.
        /// </summary>
        private void OnObstacleHit(string eventName, object sender, object data)
        {
            // Definition is not meaningful for a hit; absorb hooks (Shield) do not read it.
            PowerUpContext ctx = new PowerUpContext(Blackboard.Instance, null);

            // Enumerate the immutable registry (never mutated after Awake) and skip inactive
            // effects, so a subscriber reacting to a published event can safely touch _activeBuffs.
            foreach (KeyValuePair<PowerUpType, IPowerUpEffect> entry in _effectRegistry)
            {
                if (!_activeBuffs.ContainsKey(entry.Key)) continue;
                if (entry.Value.TryAbsorbObstacle(ctx))
                    return; // Absorbed; the effect published its own recovery event.
            }

            // No effect absorbed the hit — forward to failure as before.
            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.PlayerFailingAtObstacle, sender, data);
        }

        /// <summary>
        /// Cleans up all active buffs when the game ends.
        /// </summary>
        private void OnTempleRunEnded(string eventName, object sender, object data)
        {
            PowerUpContext ctx = new PowerUpContext(Blackboard.Instance, null);
            foreach (var kvp in _activeBuffs)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
                RemoveEffect(kvp.Key, ctx);
            }
            _activeBuffs.Clear();
        }

        /// <summary>Applies the registered effect for <paramref name="type"/>, if any.</summary>
        private void ApplyEffect(PowerUpType type, PowerUpContext ctx)
        {
            if (_effectRegistry.TryGetValue(type, out IPowerUpEffect effect))
                effect.Apply(ctx);
        }

        /// <summary>Removes (restores defaults for) the registered effect for <paramref name="type"/>, if any.</summary>
        private void RemoveEffect(PowerUpType type, PowerUpContext ctx)
        {
            if (_effectRegistry.TryGetValue(type, out IPowerUpEffect effect))
                effect.Remove(ctx);
        }

        private IEnumerator BuffDurationTimer(PowerUpDefinition definition)
        {
            yield return new WaitForSeconds(definition.Duration);

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.PowerUpDeactivateRequested, this, definition);
        }
    }
}
