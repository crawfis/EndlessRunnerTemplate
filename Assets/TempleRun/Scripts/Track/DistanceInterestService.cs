using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Registration-based distance notification service. Any component can register
    /// a distance threshold and receive a callback when the player reaches it.
    ///    Dependencies: EventsPublisherTempleRun, Blackboard.DistanceTracker
    ///    Subscribes: TempleRunEvents.DistanceUpdated
    /// </summary>
    public class DistanceInterestService : MonoBehaviour
    {
        public static DistanceInterestService Instance { get; private set; }

        // Sorted by distance ascending for efficient front-removal.
        private readonly List<float> _interests = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            Clear();
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Register a one-shot callback that fires when the player's cumulative
        /// distance reaches or exceeds <paramref name="distance"/>.
        /// </summary>
        public void Register(float distance)
        {
            // Insert in sorted order.
            int index = _interests.FindIndex(i => i >= distance);
            if (index < 0)
                _interests.Add(distance);
            else if (Mathf.Abs(_interests[index] - distance) <= 0.0001f) // Avoid duplicates
                _interests.Insert(index, distance);
        }


        /// <summary>
        /// Remove all registered interests.
        /// </summary>
        public void Clear()
        {
            _interests.Clear();
        }

        /// <summary>
        /// Check interests against the current distance
        /// </summary>
        public void Update()
        {
            float currentDistance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            while (_interests.Count > 0 && _interests[0] <= currentDistance)
            {
                var interest = _interests[0];
                _interests.RemoveAt(0);
                EventsPublisherTempleRun.Instance.PublishEvent(TempleRunEvents.DistanceUpdated, this, currentDistance);
            }
        }
    }
}
