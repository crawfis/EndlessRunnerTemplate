using System.Collections;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Moves the Character smoothly from the current position to the start of the new spline.
    ///    Dependency: EventsPublisherTempleRun
    ///    Subscribes: TeleportStarted
    /// </summary>
    public class CharacterTeleporter : MonoBehaviour
    {
        [SerializeField] private Transform _objectToMove;

        private float _yPosition;

        private void Awake()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(TempleRunEvents.TeleportStarted, OnTeleportStarted);
            _yPosition = transform.localPosition.y;
        }

        private void OnTeleportStarted(string eventName, object sender, object data)
        {
            var (teleportTime, splineData) = ((float, object))data;
            var (point1, point2, _, _) = ((Vector3, Vector3, Direction, float))splineData;
            Vector3 targetDirection = (point2 - point1).normalized;
            // Land in the player's current lane, not on the centre line: offset the target
            // perpendicular to the new heading. Without this the turn dumps the player onto the
            // centre of the new segment regardless of the lane they were running in.
            var targetPosition = new Vector3(point1.x, _yPosition, point1.z) + LaneOffset(targetDirection);
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            StartCoroutine(SmoothlyTeleport(teleportTime, targetPosition, targetRotation));
        }

        // Matches MoveCharacterByDistance.GetLateralOffset so the position the teleport lands on is
        // exactly where distance-based movement resumes — no snap when the teleport ends. A centre
        // lane (offset 0) yields the zero vector, so no special-casing is needed.
        private static Vector3 LaneOffset(Vector3 direction)
        {
            float laneOffset = Blackboard.Instance.LaneChangeController.LateralLaneOffset;
            return laneOffset * Vector3.Cross(direction, Vector3.up).normalized;
        }

        private IEnumerator SmoothlyTeleport(float teleportTime, Vector3 targetPosition, Quaternion targetDirection)
        {
            float timeRemaining = teleportTime;
            float maxTurnRate = 90f / teleportTime;
            Vector3 initialPosition = _objectToMove.localPosition;
            Quaternion initialRotation = _objectToMove.localRotation;
            while (timeRemaining > 0)
            {
                float t = (1f - timeRemaining / teleportTime);
                Vector3 position = Vector3.Lerp(initialPosition, targetPosition, t);
                //Quaternion rotation = Quaternion.RotateTowards(initialRotation, targetDirection, maxTurnRate * GameTime.Instance.deltaTime);
                Quaternion rotation = Quaternion.Slerp(initialRotation, targetDirection, t);
                _objectToMove.SetLocalPositionAndRotation(position, rotation);
                timeRemaining -= GameTime.Instance.deltaTime;
                yield return null;
            }
            _objectToMove.localPosition = targetPosition;
            _objectToMove.localRotation = targetDirection;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(TempleRunEvents.TeleportStarted, OnTeleportStarted);
        }
    }
}