using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Attached to power-up prefabs to identify their type.
    /// The CollectableCollisionDetector reads this to pass the PowerUpDefinition in event data.
    /// </summary>
    internal class PowerUpIdentifier : MonoBehaviour
    {
        [SerializeField] private PowerUpDefinition _definition;

        public PowerUpDefinition Definition { get => _definition; set => _definition = value; }
    }
}
