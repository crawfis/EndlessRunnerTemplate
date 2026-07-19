using CrawfisSoftware.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Updates the UXML document for the current distances. Could be broken into different classes.
    ///    Dependencies: Blackboard, DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: ActiveTrackChanging
    /// </summary>
    public class GUIController : MonoBehaviour
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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
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

        private void OnTrackChanging(string EventName, object sender, object data)
        {
            var trackSegment = (TrackSegmentInfo)data;
            _nextTrackDirection = trackSegment.Direction;
            _trackDistance += trackSegment.Length;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);

        }
    }
}