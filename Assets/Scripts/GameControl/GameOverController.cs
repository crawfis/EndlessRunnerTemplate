using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.Assets.Scripts
{
    internal class GameOverController : MonoBehaviour
    {
        private void Start()
        {
            EventsPublisherTempleRun.Instance.SubscribeToEvent(KnownEvents.GameOver, OnGameOver);
        }

        private void OnGameOver(object sender, object data)
        {
            EventsPublisherTempleRun.Instance.UnsubscribeToEvent(KnownEvents.GameOver, OnGameOver);
            StartCoroutine(Quit());
        }
        private IEnumerator Quit()
        {
            yield return new WaitForSecondsRealtime(GameConstants.QuitDelay);
            // For a more complex game with many scenes, we may want to access the EventsPublisher (in another script)
            // and flush all events from it (calling Pop).
            // This shows the proper way to quit a game both in Editor and with a build
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }
}
