using UnityEngine;
using CrawfisSoftware.AssetManagement;
namespace CrawfisSoftware.TempleRun
{
    public class VoxelPrefabSpawner : PrefabSpawnerAbstract
    {
        protected override void CreateTrack(float length, Transform trackTransform, Direction endCapDirection)
        {
            int numberOfVoxels = Mathf.FloorToInt(length / _heightScale + 0.2f);
            for (int i = 0; i < numberOfVoxels; i++)
            {
                //GameObject trackSegment = Instantiate<GameObject>(_prefab, trackTransform);
                GameObject trackSegment = InstantiationSingleton.CreateNewInstance(_prefab, true);
                trackSegment.transform.SetParent(trackTransform, false);
                trackSegment.transform.localPosition = new Vector3(0, 0, _heightScale * i);
            }
        }
    }
}