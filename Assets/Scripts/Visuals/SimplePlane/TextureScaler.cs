using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    internal class TextureScaler : MonoBehaviour
    {
        [SerializeField] private GameObject _targetGameObject;
        private Material _materialToScale;
        private void Start()
        {
            //EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.ActiveTrackChanged, OnActiveTrack);
            _materialToScale = _targetGameObject.GetComponent<MeshRenderer>().material;
            float zScale = transform.localScale.z;
            _materialToScale.mainTextureScale = new Vector2(1, zScale);
            // Todo: capture current scale, etc.
        }
        private void OnActiveTrack(object sender, object data)
        {
            var (_, segmentDistance) = ((Direction direction, float segmentDistance))data;
            _materialToScale.mainTextureScale = new Vector2(1, segmentDistance);
        }
    }
}