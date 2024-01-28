using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class SplinePrefabSpawner : PrefabSpawnerAbstract
    {
        protected override void Awake()
        {
            base.Awake();
            Blackboard.Instance.TrackWidthOffset = 0.5f * _widthScale;
        }

        protected override void CreateTrack(float magnitude, Transform trackTransform, Direction endCapDirection)
        {
            var trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
            trackSegment.transform.localScale = new Vector3(_widthScale, 1, magnitude);
        }
    }
}