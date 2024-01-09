using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrawfisSoftware.AssetManagement
{
    /// <summary>
    /// This will add a menu item that will pop-up a window that will allow you to trigger an event within the editor.
    /// </summary>
    public class EventMenu : EditorWindow
    {
        private const string MENU_LOCATION = "Crawfis/Events/Event Publisher Menu";
        private static int eventToPublish = 0;

        [MenuItem(MENU_LOCATION, false, 0)]
        static void OpenWindow()
        {
            // Get existing open window or if none, make a new one:
            EventMenu window = (EventMenu)GetWindow(typeof(EventMenu), false, "Event Publisher", true);
            window.Show();
        }

        public void OnGUI()
        {
            var eventCollection = EventsPublisher.Instance.GetRegisteredEvents();
            string[] eventNameArray = eventCollection.ToArray();
            eventToPublish = EditorGUILayout.Popup(eventToPublish, eventNameArray);
            if (GUILayout.Button("Publish Event"))
            {
                EventsPublisher.Instance.PublishEvent(eventNameArray[eventToPublish], this, null);
            }
        }
    }
}