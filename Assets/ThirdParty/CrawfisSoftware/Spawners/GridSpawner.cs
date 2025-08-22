using CrawfisSoftware.AssetManagement;

using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.Spawners
{
    internal class GridSpawner : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _prefabs;
        [SerializeField] private Vector2Int _gridDimensions;
        [SerializeField] private GameObject _gameTemplate;
        [SerializeField] private bool _createPrefabs = true;

        private void Start()
        {
            //InstantiationSingleton.ResetInstanceCount();
            Generate();
        }
        public void Generate()
        {
            for (int i = 0; i < _gridDimensions.x; i++)
            {
                float x = -50.0f + i * 100.0f / _gridDimensions.x;
                float deltaY = 100.0f / _gridDimensions.y;
                float y = -50.0f;
                for (int j = 0; j < _gridDimensions.y; j++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, _prefabs.Count);
                    GameObject prefab = _prefabs[randomIndex];
                    GameObject instance = InstantiationSingleton.CreateNewInstance(prefab, _createPrefabs);
                    instance.transform.position = new Vector3(x, 0, y);
                    instance.CopyScripts(_gameTemplate);
                    y += deltaY;
                }
            }
        }
    }
}