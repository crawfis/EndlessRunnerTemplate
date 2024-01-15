using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Updates the UXML document for the current distances. Could be broken into different classes.
    /// </summary>
    internal class GUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private float _trackDistance;

        private Label _leftDeathDistanceLabel;
        private Label _rightDeathDistanceLabel;
        private Label _totalDistanceLabel;
        private Direction _nextTrackDirection;
        private void Awake()
        {
            var root = _uiDocument.rootVisualElement;
            _leftDeathDistanceLabel = root.Q<Label>("_leftDeathDistance");
            _rightDeathDistanceLabel = root.Q<Label>("_rightDeathDistance");
            _totalDistanceLabel = root.Q<Label>("_totalDistanceLabel" +
                "");
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);
        }

        private void Update()
        {
            float distance = Blackboard.Instance.DistanceTracker.DistanceTravelled;
            int displayDistance = (int)(distance + 0.5f);
            _totalDistanceLabel.text = displayDistance.ToString() + "m";
            int _distanceUntilDeath = (int)(_trackDistance - distance);

            _leftDeathDistanceLabel.text = (_nextTrackDirection == Direction.Right) ? "" : _distanceUntilDeath.ToString();
            _rightDeathDistanceLabel.text = (_nextTrackDirection == Direction.Left) ? "" : _distanceUntilDeath.ToString();
        }

        private void OnTrackChanged(object sender, object data)
        {
            var tuple = (ValueTuple<Direction, float>)data;
            _nextTrackDirection = tuple.Item1;
            _trackDistance += tuple.Item2;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.ActiveTrackChanged, OnTrackChanged);

        }
    }
}