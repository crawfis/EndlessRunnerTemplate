using System.IO;

using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.Utility.Testing
{
    /// <summary>
    /// Debug helper: subscribes to ALL published events (every domain) and appends each one to a
    /// plain-text file at the project root (debug_event_log.txt) for offline analysis. Auto-boots
    /// via RuntimeInitializeOnLoadMethod, so no scene wiring is needed - just enter Play mode.
    /// Writes with AutoFlush so the log survives an abrupt Play-mode stop.
    ///    Dependencies: EventsPublisher (global, all-domain)
    /// </summary>
    internal class DebugEventFileLogger : MonoBehaviour
    {
        private const string FileName = "debug_event_log.txt";

        private StreamWriter _writer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("[DebugEventFileLogger]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.DontSave;
            go.AddComponent<DebugEventFileLogger>();
        }

        private void Awake()
        {
            // Application.dataPath is <project>/Assets in the editor -> write to the project root.
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileName));
            _writer = new StreamWriter(path, append: false) { AutoFlush = true };
            _writer.WriteLine($"# Event log started  realtime={Time.realtimeSinceStartup:F3}  frame={Time.frameCount}");
            _writer.WriteLine($"# columns: realtime | frame | event | sender | data");

            if (EventsPublisher.Instance == null)
            {
                _writer.WriteLine("# ERROR: EventsPublisher.Instance was null at AfterSceneLoad - no events captured.");
                _writer.Flush();
                return;
            }
            EventsPublisher.Instance.SubscribeToAllEvents(LogEvent);
        }

        private void OnDestroy()
        {
            if (EventsPublisher.Instance != null)
                EventsPublisher.Instance.UnsubscribeToAllEvents(LogEvent);

            if (_writer != null)
            {
                _writer.WriteLine($"# Event log stopped  realtime={Time.realtimeSinceStartup:F3}  frame={Time.frameCount}");
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
        }

        private void LogEvent(string eventName, object sender, object data)
        {
            string senderName = sender?.GetType().Name ?? "null";
            string dataStr = data?.ToString() ?? "null";
            _writer?.WriteLine($"{Time.realtimeSinceStartup,10:F3} | f{Time.frameCount,-7} | {eventName,-52} | {senderName,-28} | {dataStr}");
        }
    }
}
