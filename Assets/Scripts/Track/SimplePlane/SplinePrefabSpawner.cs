using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class SplinePrefabSpawner : MonoBehaviour
    {
        [SerializeField] private float _widthScale = 1.0f;
        [SerializeField] private float _heightScale = 1.0f;
        [Tooltip("The prefab should have it's origin at the bottom-center with positive z-axis being the forward direction.")]
        [SerializeField] private GameObject _prefab;

        private Transform _parentTransform;
        private GameObject _currentTrack;
        private int _trackNumber = 1;
        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.SplinePointAdded, OnSplineChanged);
            var parent = new GameObject("Generated Level");
            _parentTransform = parent.transform;
        }

        private void OnSplineChanged(object sender, object data)
        {
            var splineCreator = sender as SplineCreator2D;
            if (splineCreator == null || splineCreator.LinearSpline.Count < 2) return;
            // Create prefab from the last two points.
            int count = splineCreator.LinearSpline.Count;
            Vector3 point1 = splineCreator.LinearSpline[count - 2];
            Vector3 point2 = splineCreator.LinearSpline[count - 1];
            float zScale = Mathf.Abs(Vector3.Distance(point1, point2));
            Vector3 direction = (point2 - point1).normalized;
            Vector3 widthOffset = -splineCreator.Offset * direction;

            // Rotation to look at point 2
            Quaternion rotation = Quaternion.LookRotation(direction); 
            _currentTrack = new GameObject(string.Format("Track {0:D2}", _trackNumber));
            Transform trackTransform = _currentTrack.transform;
            trackTransform.parent = _parentTransform;
            trackTransform.SetLocalPositionAndRotation(point1+widthOffset, rotation);
            var trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
            trackSegment.transform.localScale = new Vector3(1, 1, zScale);
            _trackNumber++;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.SplinePointAdded, OnSplineChanged);
        }
    }
}