using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class TextureScaler : MonoBehaviour
    {
        [SerializeField] private GameObject _targetGameObject;
        private Material _materialToScale;
        private void Start()
        {
            // Assuming a texture surface, tiles the texture according the newly scaled up prefab.
            _materialToScale = _targetGameObject.GetComponent<MeshRenderer>().material;
            _materialToScale.mainTextureScale = new Vector2(transform.localScale.x, transform.localScale.z);
            // Todo: capture current scale, etc.
        }
    }
}