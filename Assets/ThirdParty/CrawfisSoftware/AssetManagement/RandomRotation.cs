using UnityEngine;

namespace Crawfis.CSE5559.Lab1
{
    internal class RandomRotation : MonoBehaviour
    {
        // Bug: If using PrefabUtility.InstantiatePrefab() the transform is (may be?) reset to the prefab's position after this is called.
        // Using Start works or adding this component after the prefab is instantiated works.
        private void Start()
        {
            float randomY = UnityEngine.Random.Range(0.0f, 360.0f);
            transform.localRotation = Quaternion.Euler(0, randomY, 0);
        }
    }
}