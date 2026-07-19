using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Central buff manager and obstacle-hit gate.
    /// Applies/removes power-up buffs on the Blackboard and manages buff duration timers.
    /// Also gates ObstacleHit: if Shield is active, publishes ObstacleRecovered instead of PlayerFailingAtObstacle.
    ///    Dependencies: Blackboard, PowerUpDefinition
    ///    Subscribes: TempleRunEvents.PowerUpCollecting (destroy GO, publish PowerUpCollected)
    ///    Subscribes: TempleRunEvents.PowerUpActivating (apply buff)
    ///    Subscribes: TempleRunEvents.PowerUpDeactivating (remove buff)
    ///    Subscribes: TempleRunEvents.ObstacleHit (gate for Shield)
    ///    Subscribes: TempleRunEvents.TempleRunEnded (cleanup all buffs)
    ///    Publishes: TempleRunEvents.PowerUpCollected
    ///    Publishes: TempleRunEvents.PowerUpActivated
    ///    Publishes: TempleRunEvents.PowerUpDeactivated
    ///    Publishes: TempleRunEvents.PowerUpDeactivateRequested
    ///    Publishes: TempleRunEvents.PlayerFailingAtObstacle (when no shield)
    ///    Publishes: TempleRunEvents.ObstacleRecovered (when shield absorbs hit)
    /// </summary>
    internal class PowerUpBuffController : MonoBehaviour
    {
        private readonly Dictionary<PowerUpType, Coroutine> _activeBuffs = new();

        private void Awake()
        {
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

            // Cancel existing buff of the same type (reset timer)
            if (_activeBuffs.TryGetValue(definition.Type, out Coroutine existingCoroutine))
            {
                if (existingCoroutine != null)
                    StopCoroutine(existingCoroutine);
                RemoveBuff(definition.Type);
            }

            ApplyBuff(definition);

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

            RemoveBuff(definition.Type);
            _activeBuffs.Remove(definition.Type);

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.PowerUpDeactivated, this, definition);
        }

        /// <summary>
        /// Gates ObstacleHit: if Shield is active, absorbs the hit and publishes ObstacleRecovered.
        /// Otherwise, forwards to PlayerFailingAtObstacle (replacing the commented-out auto-chain).
        /// </summary>
        private void OnObstacleHit(string eventName, object sender, object data)
        {
            if (Blackboard.Instance.ShieldActive)
            {
                // Shield absorbs the hit
                EventsPublisherTempleRun.Instance.PublishEvent(
                    TempleRunEvents.ObstacleRecovered, this, null);
            }
            else
            {
                // No shield — forward to failure as the auto-chain previously did
                EventsPublisherTempleRun.Instance.PublishEvent(
                    TempleRunEvents.PlayerFailingAtObstacle, sender, data);
            }
        }

        /// <summary>
        /// Cleans up all active buffs when the game ends.
        /// </summary>
        private void OnTempleRunEnded(string eventName, object sender, object data)
        {
            foreach (var kvp in _activeBuffs)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
                RemoveBuff(kvp.Key);
            }
            _activeBuffs.Clear();
        }

        private void ApplyBuff(PowerUpDefinition definition)
        {
            switch (definition.Type)
            {
                case PowerUpType.SpeedBoost:
                    Blackboard.Instance.ActiveSpeedMultiplier = definition.Magnitude;
                    break;
                case PowerUpType.CoinMagnet:
                    Blackboard.Instance.CoinMagnetActive = true;
                    Blackboard.Instance.CoinMagnetRadius = definition.Magnitude;
                    break;
                case PowerUpType.Shield:
                    Blackboard.Instance.ShieldActive = true;
                    break;
                case PowerUpType.ScoreMultiplier:
                    Blackboard.Instance.ActiveScoreMultiplier = definition.Magnitude;
                    break;
            }
        }

        private void RemoveBuff(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.SpeedBoost:
                    Blackboard.Instance.ActiveSpeedMultiplier = 1.0f;
                    break;
                case PowerUpType.CoinMagnet:
                    Blackboard.Instance.CoinMagnetActive = false;
                    Blackboard.Instance.CoinMagnetRadius = 0f;
                    break;
                case PowerUpType.Shield:
                    Blackboard.Instance.ShieldActive = false;
                    break;
                case PowerUpType.ScoreMultiplier:
                    Blackboard.Instance.ActiveScoreMultiplier = 1.0f;
                    break;
            }
        }

        private IEnumerator BuffDurationTimer(PowerUpDefinition definition)
        {
            yield return new WaitForSeconds(definition.Duration);

            EventsPublisherTempleRun.Instance.PublishEvent(
                TempleRunEvents.PowerUpDeactivateRequested, this, definition);
        }
    }
}
