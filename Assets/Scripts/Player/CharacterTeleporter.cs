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
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.TeleportStarted, OnTeleportStarted);
            _yPosition = transform.localPosition.y;
        }

        private void OnTeleportStarted(object sender, object data)
        {
            var (teleportTime, splineData) = ((float, object))data;
            var (point1, point2, _) = ((Vector3 point1, Vector3 point2, Direction direction))splineData;
            // Create prefab from the two points.
            //var (point1, point2, _) = ((Vector3 point1, Vector3 point2, Direction direction))(splineData);
            Vector3 targetDirection = (point2 - point1).normalized;
            var targetPosition = new Vector3(point1.x, _yPosition, point1.z);
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            StartCoroutine(SmoothlyTeleport(teleportTime, targetPosition, targetRotation));
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
                //Quaternion rotation = Quaternion.RotateTowards(initialRotation, targetDirection, maxTurnRate * Time.deltaTime);
                Quaternion rotation = Quaternion.Slerp(initialRotation, targetDirection, t);
                _objectToMove.SetLocalPositionAndRotation(position, rotation);
                timeRemaining -= Time.deltaTime;
                yield return null;
            }
            _objectToMove.localPosition = targetPosition;
            _objectToMove.localRotation = targetDirection;
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.TeleportStarted, OnTeleportStarted);
        }
    }
}