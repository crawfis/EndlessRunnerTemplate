using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Handles coin collection logic: increments session coin count, applies score multiplier,
    /// destroys the coin GameObject, and publishes CoinCollected.
    ///    Dependencies: Blackboard
    ///    Subscribes: TempleRunEvents.CoinCollecting (process coin pickup)
    ///    Publishes: TempleRunEvents.CoinCollected (data: int sessionCoinCount)
    /// </summary>
    internal class CoinCollectionController : MonoBehaviour
    {
        private void Awake()
        {
            TempleRunBus.Subscribe(
                TempleRunEvents.CoinCollecting, OnCoinCollecting);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.CoinCollecting, OnCoinCollecting);
        }

        private void OnCoinCollecting(string eventName, object sender, object data)
        {
            GameObject coinGO = data as GameObject;

            // Determine coin value from CoinConfig
            CoinConfig coinConfig = Blackboard.Instance.CoinConfig;
            int baseValue = coinConfig != null ? coinConfig.CoinValue : 1;

            // Apply active score multiplier from power-ups
            int adjustedValue = Mathf.RoundToInt(baseValue * Blackboard.Instance.ActiveScoreMultiplier);
            Blackboard.Instance.SessionCoinCount += adjustedValue;

            // Destroy the collected coin
            if (coinGO != null)
            {
                Destroy(coinGO);
            }

            // Publish CoinCollected with current session total
            TempleRunBus.Publish(
                TempleRunEvents.CoinCollected, this, Blackboard.Instance.SessionCoinCount);
        }
    }
}
