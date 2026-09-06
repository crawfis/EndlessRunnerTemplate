using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Moves the Character smoothly from the current position to the start of the new spline.
    ///    Dependency: EventsFor<TempleRunEvents>
    ///    Subscribes: TeleportStarted (data: TeleportInfo)
    /// </summary>
    public class CharacterTeleporter : MonoBehaviour
    {
        [SerializeField] private Transform _objectToMove;

        private float _yPosition;

        private void Awake()
        {
            TempleRunBus.Subscribe(TempleRunEvents.TeleportStarted, OnTeleportStarted);
            _yPosition = transform.localPosition.y;
        }

        private void OnTeleportStarted(string eventName, object sender, object data)
        {
            var teleport = (TeleportInfo)data;
            Vector3 targetDirection = teleport.Destination.Heading;
            // Land in the player's current lane, not on the centre line: offset the target
            // perpendicular to the new heading. Without this the turn dumps the player onto the
            // centre of the new segment regardless of the lane they were running in.
            Vector3 landing = teleport.Destination.Start;
            var targetPosition = new Vector3(landing.x, _yPosition, landing.z) + LaneOffset(targetDirection);
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            _ = SmoothlyTeleport(teleport.Duration, targetPosition, targetRotation);
        }

        // Matches MoveCharacterByDistance.GetLateralOffset so the position the teleport lands on is
        // exactly where distance-based movement resumes — no snap when the teleport ends. A centre
        // lane (offset 0) yields the zero vector, so no special-casing is needed.
        private static Vector3 LaneOffset(Vector3 direction)
        {
            float laneOffset = Blackboard.Instance.LaneChangeController.LateralLaneOffset;
            return laneOffset * Vector3.Cross(direction, Vector3.up).normalized;
        }

        private async Awaitable SmoothlyTeleport(float teleportTime, Vector3 targetPosition, Quaternion targetDirection)
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
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            _objectToMove.localPosition = targetPosition;
            _objectToMove.localRotation = targetDirection;
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.TeleportStarted, OnTeleportStarted);
        }
    }
}