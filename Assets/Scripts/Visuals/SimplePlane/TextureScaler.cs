using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class TextureScaler : MonoBehaviour
    {
        [SerializeField] private GameObject _targetGameObject;
        private Material _materialToScale;
        private void Start()
        {
            _materialToScale = _targetGameObject.GetComponent<MeshRenderer>().material;
            float zScale = transform.localScale.z;
            _materialToScale.mainTextureScale = new Vector2(1, zScale);
            // Todo: capture current scale, etc.
        }
    }
}