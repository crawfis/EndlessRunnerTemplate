using CrawfisSoftware.Events;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.Utility
{
    class TimedEvent : MonoBehaviour
    {
        [SerializeField] private float _delayInSeconds = 1.0f;
        [SerializeField] private string _eventName = "ERROR";
        [SerializeField] private bool _useRealtime = true;

        private void Start()
        {
            StartCoroutine(FireEvent());
        }
        private IEnumerator FireEvent()
        {
            if (_useRealtime)
            {
                yield return new WaitForSecondsRealtime(_delayInSeconds);
            }
            else
            {
                yield return new WaitForSeconds(_delayInSeconds);
            }
                EventsPublisher.Instance.PublishEvent(_eventName, this, null);
            yield break;
        }
    }
}