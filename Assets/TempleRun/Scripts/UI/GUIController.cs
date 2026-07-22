using CrawfisSoftware.Events;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Updates the UXML document for the current distances. Could be broken into different classes.
    ///    Dependencies: PanelRenderer (HUD overlay), Blackboard, DistanceTracker, EventsPublisherTempleRun
    ///    Subscribes: ActiveTrackChanging
    /// </summary>
    public class GUIController : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _panel;

        private float _trackDistance;

        private VisualElement _root;
        private Label _leftDeathDistanceLabel;
        private Label _rightDeathDistanceLabel;
        private Label _totalDistanceLabel;
        private Direction _nextTrackDirection;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.ActiveTrackChanging, OnTrackChanging);
        }

        private void OnEnable() => _panel.RegisterUIReloadCallback(OnUIReload);

        private void OnDisable() => _panel.UnregisterUIReloadCallback(OnUIReload);

        // The PanelRenderer surfaces its visual tree only through this callback (it has no
        // root-tree property). Cache the HUD labels here; Update() guards until they arrive.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            _leftDeathDistanceLabel = root.Q<Label>("_leftDeathDistance");
            _rightDeathDistanceLabel = root.Q<Label>("_rightDeathDistance");
            _totalDistanceLabel = root.Q<Label>("_totalDistanceLabel");
        }

        private void Update()
        {
            if (_totalDistanceLabel == null || _leftDeathDistanceLabel == null || _rightDeathDistanceLabel == null)
                return;

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