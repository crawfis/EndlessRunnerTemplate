using CrawfisSoftware.Events;
using CrawfisSoftware.TempleRun.GameConfig;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Smoothly interpolates the lateral lane offset when lane change events fire.
    /// Writes to LaneChangeController.LateralLaneOffset each frame during the lerp, then
    /// publishes the completion event when done.
    ///    Dependencies: Blackboard, LaneConfig, LaneChangeController
    ///    Subscribes: TempleRunEvents.LaneChangingLeft, LaneChangingRight
    ///    Publishes: TempleRunEvents.LaneChangedLeft, LaneChangedRight
    /// </summary>
    internal class LaneOffsetController : MonoBehaviour
    {
        [SerializeField] private LaneChangeController _laneChangeController;
        private Coroutine _lerpCoroutine;

        private void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.LaneChangingLeft, OnLaneChangingLeft);
            EventsPublisherTempleRun.Instance.SubscribeToEvent(
                TempleRunEvents.LaneChangingRight, OnLaneChangingRight);
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.LaneChangingLeft, OnLaneChangingLeft);
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(
                TempleRunEvents.LaneChangingRight, OnLaneChangingRight);

            if (_lerpCoroutine != null)
                StopCoroutine(_lerpCoroutine);
        }

        private void OnLaneChangingLeft(string eventName, object sender, object data)
        {
            int targetLane = (int)data;
            float targetOffset = -targetLane * Blackboard.Instance.LaneConfig.LaneWidth;
            _lerpCoroutine = StartCoroutine(
                LerpToOffset(targetOffset, TempleRunEvents.LaneChangedLeft, data));
        }

        private void OnLaneChangingRight(string eventName, object sender, object data)
        {
            int targetLane = (int)data;
            float targetOffset = -targetLane * Blackboard.Instance.LaneConfig.LaneWidth;
            _lerpCoroutine = StartCoroutine(
                LerpToOffset(targetOffset, TempleRunEvents.LaneChangedRight, data));
        }

        private IEnumerator LerpToOffset(float targetOffset, TempleRunEvents completionEvent, object data)
        {
            LaneConfig config = Blackboard.Instance.LaneConfig;
            float startOffset = _laneChangeController.LateralLaneOffset;
            float duration = config.LaneChangeDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = config.LaneChangeCurve.Evaluate(t);
                _laneChangeController.LateralLaneOffset = Mathf.Lerp(startOffset, targetOffset, curvedT);
                yield return null;
            }

            // Snap to exact target
            _laneChangeController.LateralLaneOffset = targetOffset;
            _lerpCoroutine = null;

            EventsPublisherTempleRun.Instance.PublishEvent(completionEvent, this, data);
        }
    }
}
