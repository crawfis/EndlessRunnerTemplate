using UnityEngine;
using CrawfisSoftware.AssetManagement;
namespace CrawfisSoftware.TempleRun
{
    public class VoxelPrefabSpawner : PrefabSpawnerAbstract
    {
        protected override void Awake()
        {
            base.Awake();
            // Half the visual track width — the corner-centring offset AxisAligned90Builder reads
            // when placing turn exit strips. The voxel prefab is _widthScale units wide, so the
            // exit tiles butt flush against the approach only when this matches. SplinePrefabSpawner
            // sets the same value for its (narrower) plane.
            Blackboard.Instance.TrackWidthOffset = 0.5f * _widthScale;
        }

        protected override void CreateTrack(float length, Transform trackTransform, Direction endCapDirection)
        {
            int numberOfVoxels = Mathf.FloorToInt(length / _lengthScale + 0.2f);
            for (int i = 0; i < numberOfVoxels; i++)
            {
                //GameObject trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
                GameObject trackSegment = InstantiationSingleton.CreateNewInstance(_prefab, true);
                trackSegment.transform.SetParent(trackTransform, false);
                trackSegment.transform.localPosition = new Vector3(0, 0, _lengthScale * i);
            }
        }
    }
}