using CrawfisSoftware.Events;
using CrawfisSoftware.TempleRun.GameConfig;

using System.Threading;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

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

        // One token source covers both restart (a new lane change cancels the lerp in flight)
        // and destroy, so the lerp never needs destroyCancellationToken as well.
        private CancellationTokenSource _cts;

        private void Start()
        {
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangingLeft, OnLaneChangingLeft);
            TempleRunBus.Subscribe(
                TempleRunEvents.LaneChangingRight, OnLaneChangingRight);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(
                TempleRunEvents.LaneChangingLeft, OnLaneChangingLeft);
            TempleRunBus.Unsubscribe(
                TempleRunEvents.LaneChangingRight, OnLaneChangingRight);

            _cts?.Cancel();
        }

        private void OnLaneChangingLeft(string eventName, object sender, object data)
        {
            int targetLane = (int)data;
            float targetOffset = -targetLane * Blackboard.Instance.LaneConfig.LaneWidth;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = LerpToOffset(targetOffset, TempleRunEvents.LaneChangedLeft, data, _cts.Token);
        }

        private void OnLaneChangingRight(string eventName, object sender, object data)
        {
            int targetLane = (int)data;
            float targetOffset = -targetLane * Blackboard.Instance.LaneConfig.LaneWidth;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = LerpToOffset(targetOffset, TempleRunEvents.LaneChangedRight, data, _cts.Token);
        }

        private async Awaitable LerpToOffset(float targetOffset, TempleRunEvents completionEvent, object data, CancellationToken token)
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
                await Awaitable.NextFrameAsync(token);
            }

            // Snap to exact target
            _laneChangeController.LateralLaneOffset = targetOffset;

            TempleRunBus.Publish(completionEvent, this, data);
        }
    }
}
