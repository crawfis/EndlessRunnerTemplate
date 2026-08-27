using CrawfisSoftware.TempleRun;

using System.Collections.Generic;

using UnityEngine;
using GameFlowBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.GameFlow.Events.GameFlowEvents>;
using TempleRunBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.TempleRun.TempleRunEvents>;

namespace CrawfisSoftware.GameFlow.Events
{
    internal class TempleRunGameFlowBridge : MonoBehaviour
    {
        private Dictionary<TempleRunEvents, GameFlowEvents> _autoTempleRun2GameFlowEvents = new Dictionary<TempleRunEvents, GameFlowEvents>()
        {
            // TempleRun paused -> request GameFlow pause (for menus/UI)
            { TempleRunEvents.PlayerPaused, GameFlowEvents.PauseRequested },

            // TempleRun resumed -> request GameFlow resume. The counterpart to the line above:
            // without it nothing ever publishes GameFlowEvents.ResumeRequested, so the
            // ResumeRequested -> Resuming -> Resumed chain never runs and GameState.IsGamePaused
            // stays true forever once the player has paused even once.
            // PlayerResumed is also published after death (PlayerLifeController) and after a
            // teleport (TeleportController). Both are harmless here: the only subscriber to
            // GameFlowEvents.Resumed is GameState.OnResume, which idempotently clears the flag.
            { TempleRunEvents.PlayerResumed, GameFlowEvents.ResumeRequested },

            // Countdown ended -> game officially started (absorbed from GameController)
            { TempleRunEvents.CountdownEnded, GameFlowEvents.GameStarted },

            // Player died -> game ending (absorbed from GameController)
            { TempleRunEvents.TempleRunEnded, GameFlowEvents.GameEnding },
        };

        private Dictionary<GameFlowEvents, TempleRunEvents> _autoGameFlow2TempleRunEvents = new Dictionary<GameFlowEvents, TempleRunEvents>()
        {
            // Bridge start: when the broader game signals started, fire TempleRun start requested
            { GameFlowEvents.GameStarted, TempleRunEvents.TempleRunStartRequested },

            // GameFlow starting -> kick off countdown in TempleRun
            { GameFlowEvents.GameStarting, TempleRunEvents.CountdownStartRequested },

            // Config/scenes bridged to TempleRun domain
            { GameFlowEvents.GameConfigApplied, TempleRunEvents.TempleRunConfigApplied },
            { GameFlowEvents.LevelApplied, TempleRunEvents.TempleRunLevelApplied },
            { GameFlowEvents.GameScenesLoaded, TempleRunEvents.TempleRunScenesReady },

            // The selected level's difficulty table -> the TempleRun difficulty system, which
            // resolves the player's chosen difficulty against it. The level owns the set of
            // configs; the difficulty preference selects one from that set.
            { GameFlowEvents.DifficultySettingsApplied, TempleRunEvents.TempleRunDifficultySettingsApplied },
        };

        protected virtual void Awake()
        {
            TempleRunBus.SubscribeToAll(AutoFireGameFlowEventFromTempleRunEvent);
            GameFlowBus.SubscribeToAll(AutoFireTempleRunEventFromGameFlowEvent);
        }

        protected virtual void OnDestroy()
        {
            TempleRunBus.UnsubscribeFromAll(AutoFireGameFlowEventFromTempleRunEvent);
            GameFlowBus.UnsubscribeFromAll(AutoFireTempleRunEventFromGameFlowEvent);
        }

        private void AutoFireGameFlowEventFromTempleRunEvent(string eventName, object sender, object data)
        {
            if (!TempleRunBus.TryGetEnum(eventName, out TempleRunEvents templeRunEvent)) return;
            if (_autoTempleRun2GameFlowEvents.TryGetValue(templeRunEvent, out GameFlowEvents autoEvent))
            {
                GameFlowBus.Publish(autoEvent, sender, data);
            }
        }

        private void AutoFireTempleRunEventFromGameFlowEvent(string eventName, object sender, object data)
        {
            if (!GameFlowBus.TryGetEnum(eventName, out GameFlowEvents gameflowEvent)) return;
            if (_autoGameFlow2TempleRunEvents.TryGetValue(gameflowEvent, out TempleRunEvents autoEvent))
            {
                TempleRunBus.Publish(autoEvent, this, data);
            }
        }
    }
}