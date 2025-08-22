using UnityEngine;

namespace CrawfisSoftware.Utility
{
    public class RandomColors : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private int _materialIndex = 0;
        private void Start()
        {
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }
            Color randomColor = new Color(Random.value, Random.value, Random.value);
            //meshRenderer.material.color = randomColor;
            _meshRenderer.materials[_materialIndex].color = randomColor;
        }
    }
}