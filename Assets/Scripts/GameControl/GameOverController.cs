using CrawfisSoftware.Events;

using System.Collections;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Handles quitting.
    ///    Dependency: EventsPublisherTempleRun
    ///    Subscribes: GameOver - Currently it quits the application.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        private void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, OnGameOver);
        }

        private void OnGameOver(string EventName, object sender, object data)
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, OnGameOver);
            StartCoroutine(Quit());
        }
        private IEnumerator Quit()
        {
            yield return new WaitForSecondsRealtime(GameConstants.QuitDelay);
            // This shows the proper way to quit a game both in Editor and with a build
#if UNITY_EDITOR
            // Needed in Unity editor to clear any subscribers who forgot to unsubscribe.
            // Useful to put a breakpoint here to see if there are any subscribers left.
            EventsPublisher.Instance.Clear();
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }
}
