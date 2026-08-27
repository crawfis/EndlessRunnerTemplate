using System.Collections.Generic;

using UnityEngine;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// The pausable gameplay clock. Everything that moves the player reads GameTime.deltaTime
    /// rather than UnityEngine.Time.deltaTime, so gameplay can be frozen without freezing the
    /// application.
    ///    Subscribes: TempleRunEvents.PlayerPaused / PlayerResumed  (the user's pause)
    ///    Subscribes: TempleRunEvents.PlayerFailing / PlayerFailed  (the post-failure hitch)
    ///    Subscribes: TempleRunEvents.TempleRunStarted              (a run starts unfrozen)
    /// </summary>
    public class GameTime : MonoBehaviour
    {
        public static GameTime Instance { get; private set; }

        /// <summary>
        /// Why gameplay time is currently frozen. Freezes are tracked as a SET, not a bool:
        /// a user pause and a post-failure hitch can overlap, and with a single latch whichever
        /// one ended first would thaw the other. Membership also makes a repeated hold or a
        /// duplicate release harmless.
        /// </summary>
        private enum FreezeReason { UserPause, Failure }

        private readonly HashSet<FreezeReason> _freezes = new HashSet<FreezeReason>();

        private float _timeScale = 1f;
        public float deltaTime
        {
            get
            {
                return _timeScale * UnityEngine.Time.deltaTime;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance);
            }
            Instance = this;

            TempleRunBus.Subscribe(TempleRunEvents.PlayerPaused, OnPlayerPause);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerResumed, OnPlayerResume);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerFailing, OnPlayerFailing);
            TempleRunBus.Subscribe(TempleRunEvents.PlayerFailed, OnPlayerFailed);
            TempleRunBus.Subscribe(TempleRunEvents.TempleRunStarted, OnRunStarted);
        }

        private void OnDestroy()
        {
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerPaused, OnPlayerPause);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerResumed, OnPlayerResume);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerFailing, OnPlayerFailing);
            TempleRunBus.Unsubscribe(TempleRunEvents.PlayerFailed, OnPlayerFailed);
            TempleRunBus.Unsubscribe(TempleRunEvents.TempleRunStarted, OnRunStarted);
        }

        private void OnPlayerPause(string eventName, object sender, object data)
        {
            Hold(FreezeReason.UserPause);
        }

        private void OnPlayerResume(string eventName, object sender, object data)
        {
            Release(FreezeReason.UserPause);
        }

        private void OnPlayerFailing(string eventName, object sender, object data)
        {
            Hold(FreezeReason.Failure);
        }

        private void OnPlayerFailed(string eventName, object sender, object data)
        {
            Release(FreezeReason.Failure);
        }

        private void OnRunStarted(string eventName, object sender, object data)
        {
            // A fresh run always starts moving, whatever state the previous one ended in.
            _freezes.Clear();
            Apply();
        }

        private void Hold(FreezeReason reason)
        {
            _freezes.Add(reason);
            Apply();
        }

        private void Release(FreezeReason reason)
        {
            _freezes.Remove(reason);
            Apply();
        }

        private void Apply()
        {
            _timeScale = _freezes.Count > 0 ? 0f : 1f;
        }
    }
}
