using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Detects collisions between the player and collectables (coins and power-ups) using Unity trigger colliders.
    /// Publishes the appropriate collection event based on the collided object's tag.
    /// Unlike obstacles, collectables are always picked up regardless of jump/slide state.
    ///    Dependencies: PowerUpIdentifier (on power-up GameObjects)
    ///    Publishes: TempleRunEvents.CoinCollectRequested (data: coin GameObject)
    ///    Publishes: TempleRunEvents.PowerUpCollectRequested (data: PowerUpDefinition)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    internal class CollectableCollisionDetector : MonoBehaviour
    {
        [Tooltip("Tag used to identify coin GameObjects. Must match the tag set on coin prefabs.")]
        [SerializeField] private string _coinTag = "Coin";

        [Tooltip("Tag used to identify power-up GameObjects. Must match the tag set on power-up prefabs.")]
        [SerializeField] private string _powerUpTag = "PowerUp";

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_coinTag))
            {
                EventsPublisherTempleRun.Instance.PublishEvent(
                    TempleRunEvents.CoinCollectRequested, this, other.gameObject);
                return;
            }

            if (other.CompareTag(_powerUpTag))
            {
                var identifier = other.GetComponent<PowerUpIdentifier>();
                if (identifier != null && identifier.Definition != null)
                {
                    // Pass both the definition and the GameObject as a tuple so the controller can destroy the GO
                    EventsPublisherTempleRun.Instance.PublishEvent(
                        TempleRunEvents.PowerUpCollectRequested, this, (identifier.Definition, other.gameObject));
                }
                return;
            }
        }
    }
}
