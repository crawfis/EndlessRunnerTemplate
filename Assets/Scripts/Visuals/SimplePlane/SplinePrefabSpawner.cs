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

        protected override void CreateTrack(Vector3 direction, Transform trackTransform)
        {
            float zScale = direction.magnitude;
            var trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
            trackSegment.transform.localScale = new Vector3(1, 1, zScale);
        }
    }
}