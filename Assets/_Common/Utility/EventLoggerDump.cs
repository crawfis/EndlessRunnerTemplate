using CrawfisSoftware.Events;
using CrawfisSoftware.GameFlow.Events;

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using static UnityEngine.Rendering.GPUSort;

namespace CrawfisSoftware.Utility.Testing
{
    internal class EventLoggerDump : MonoBehaviour
    {
        [SerializeField] GameFlowEvents _dumpTriggerEvent = GameFlowEvents.QuitRequested;
        private StringBuilder _sb = new StringBuilder();
        private void Awake()
        {
            EventsPublisherGameFlow.Instance.SubscribeToEvent(_dumpTriggerEvent, DumpLogs);
            EventsPublisher.Instance.SubscribeToAllEvents(LogEvent);
            _sb.AppendLine("Sequence of events:");
        }

        private void OnDestroy()
        {
            EventsPublisher.Instance.UnsubscribeToAllEvents(LogEvent);
            EventsPublisherGameFlow.Instance.UnsubscribeToEvent(_dumpTriggerEvent, DumpLogs);
        }

        private void LogEvent(string eventName, object arg2, object arg3)
        {
            _sb.AppendLine(eventName);
        }

        private void DumpLogs(string eventName, object sender, object data)
        {
            Debug.Log(_sb.ToString());
        }
    }
}