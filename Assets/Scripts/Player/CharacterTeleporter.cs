using System.Collections;
using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
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
            var (teleportTime, (point1, point2, _)) = ((float, (Vector3 point1, Vector3 point2, Direction direction)))data;
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
            while (timeRemaining > 0)
            {
                _objectToMove.localPosition = Vector3.Lerp(_objectToMove.localPosition, targetPosition, (1f - timeRemaining / teleportTime));
                _objectToMove.localRotation = Quaternion.RotateTowards(_objectToMove.localRotation, targetDirection, 1);
                timeRemaining -= Time.deltaTime;
                yield return null;
            }
            _objectToMove.localPosition = targetPosition;
            _objectToMove.localRotation = targetDirection;
            //EventsPublisherTempleRun.Instance.PublishEvent(KnownEvents.TeleportEnded, this, (targetPosition, targetDirection));
        }

        private void OnDestroy()
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.CurrentSplineChanging, OnTeleportStarted);
        }
    }
}