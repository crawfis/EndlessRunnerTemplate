using CrawfisSoftware.Events;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.GameFlow.Events
{
    /// <summary>
    /// Auto-chains GameFlow events. Entries marked with [AUTO] are active; others are published by controllers.
    ///
    /// ========================================================================================
    /// COMPLETE GAME FLOW TIMELINE (from actual event trace)
    /// ========================================================================================
    ///
    /// --- BOOT / INITIALIZATION (driven by UGS, see UGSAutoEventFlow + UGSGameFlowBridge) ---
    /// [UGS] UnityServicesInitialized
    /// [UGS] CheckForExistingSession -> CheckForExistingSessionSucceeded
    /// [UGS] PlayerAuthenticating -> PlayerAuthenticated
    /// [BRIDGE: UGS->GameFlow] PlayerAuthenticated -> GameplayReady
    /// [AUTO] GameplayReady -> MainMenuShowRequested
    /// [UGS] RemoteConfigFetching -> RemoteConfigFetched -> RemoteConfigUpdated
    /// [BRIDGE: UGS->GameFlow] RemoteConfigUpdated -> LoadingScreenHideRequested
    /// [AUTO] MainMenuShowRequested -> MainMenuShowing
    /// [AUTO] LoadingScreenHideRequested -> LoadingScreenHiding
    /// [Published] DifficultySettingsApplied
    /// [AUTO] DifficultyChangeRequested -> DifficultyChanging
    /// [Published] DifficultyChanged
    /// [Published] LoadingScreenShown -> LoadingScreenHidden
    /// [Published] MainMenuShown
    ///
    /// --- SIGN OUT / SIGN IN LOOP ---
    /// [UGS] PlayerSigningOut -> PlayerSignedOut
    /// [BRIDGE: UGS->GameFlow] PlayerSignedOut -> GameplayNotReady
    /// [Published] MainMenuHidden
    /// [UGS] PlayerSigningIn -> PlayerSignedIn -> PlayerAuthenticating -> PlayerAuthenticated
    /// [BRIDGE: UGS->GameFlow] PlayerAuthenticated -> GameplayReady
    /// [AUTO] GameplayReady -> MainMenuShowRequested -> MainMenuShowing
    /// [Published] MainMenuShown
    ///
    /// --- GAME START (user clicks Play) ---
    /// [Published] LevelSelectorShowRequested
    /// [AUTO] LevelSelectorShowRequested -> LevelSelectorShowing
    /// [Published] MainMenuHidden
    /// [Published] LevelSelectorShown
    /// User selects a level:
    /// [Published] LevelSelected (data: LevelConfig)
    /// [AUTO] LevelSelected -> GameScenesLoadRequested
    /// [AUTO] GameScenesLoadRequested -> GameScenesLoading
    /// [Published] LevelSelectorHidden
    /// [Published] DifficultyChanging -> DifficultyChanged
    /// [Published] GameConfigApplying -> GameConfigApplied
    /// [Published] GameScenesLoaded
    /// [AUTO] GameScenesLoaded -> GameStartRequested
    /// [AUTO] GameStartRequested -> GameStarting
    /// [AUTO] GameStarting -> CountdownStartRequested
    /// [AUTO] CountdownStartRequested -> CountdownStarting
    ///
    /// --- COUNTDOWN ---
    /// [Published] CountdownTick (repeats)
    /// [Published] CountdownEnding
    /// [Published] CountdownEnded
    /// [Published] GameStarted
    ///
    /// --- GAMEPLAY LOOP (see TempleRunAutoEventFlow for gameplay events) ---
    /// [TempleRun] LeftTurnRequested/RightTurnRequested -> LeftTurnSucceeded/RightTurnSucceeded
    /// [TempleRun] TrackSegmentCreated, SplineSegmentCreated
    /// [TempleRun] ActiveTrackChanging -> CurrentSplineChanging -> TeleportStarted -> TeleportEnded -> CurrentSplineChanged
    /// [TempleRun] PlayerFailing -> PlayerPause ... PlayerResume
    /// [TempleRun] PlayerDied
    ///
    /// --- GAME END (triggered by PlayerDied in controller) ---
    /// [Published] GameEnding
    /// [AUTO] GameEnding -> GameScenesUnloadRequested
    /// [BRIDGE: GameFlow->UGS] GameEnding -> ScoreUpdating
    /// [AUTO] GameScenesUnloadRequested -> GameScenesUnloading
    /// [UGS] ScoreUpdated
    /// [TempleRun] PlayerResume
    /// [Published] GameScenesUnloaded
    /// [Published] GameEnded
    /// [BRIDGE: GameFlow->UGS] GameEnded -> LeaderboardOpening
    ///
    /// --- POST-GAME -> BACK TO MAIN MENU (non-UGS) ---
    /// (In the UGS template this was a leaderboard/achievements/ad loop that ended in
    ///  PlayerAuthenticated -> GameplayReady. This template has no UGS, so GameEnded
    ///  re-signals GameplayReady directly.)
    /// [AUTO] GameEnded -> GameplayReady
    /// [AUTO] GameplayReady -> MainMenuShowRequested -> MainMenuShowing
    /// [Published] MainMenuShown
    ///
    /// --- QUIT ---
    /// [Published] QuitRequested
    /// [Published] Quitting
    /// [Published] Quitted
    ///
    /// ========================================================================================
    /// </summary>
    internal class GameFlowAutoEventFlow : MonoBehaviour
    {
        [SerializeField] private Dictionary<GameFlowEvents, GameFlowEvents> _autoGameFlow2GameFlowEvents = new Dictionary<GameFlowEvents, GameFlowEvents>()
        {
            // ================================================================================
            // LOADING SCREEN BRIDGES
            // ================================================================================
            { GameFlowEvents.LoadingScreenShowRequested, GameFlowEvents.LoadingScreenShowing },
            { GameFlowEvents.LoadingScreenHideRequested, GameFlowEvents.LoadingScreenHiding },
            // LoadingScreenShowing -> LoadingScreenShown: Published by LoadingScreenController
            // LoadingScreenHiding -> LoadingScreenHidden: Published by LoadingScreenController

            // ================================================================================
            // MAIN MENU BRIDGES
            // ================================================================================
            { GameFlowEvents.MainMenuShowRequested, GameFlowEvents.MainMenuShowing },
            { GameFlowEvents.MainMenuHideRequested, GameFlowEvents.MainMenuHiding },
            // MainMenuShowing -> MainMenuShown: Published by MainMenuPanelController
            // MainMenuHiding -> MainMenuHidden: Published by MainMenuPanelController

            // ================================================================================
            // LEVEL SELECTOR BRIDGES
            // ================================================================================
            { GameFlowEvents.LevelSelectorShowRequested, GameFlowEvents.LevelSelectorShowing },
            { GameFlowEvents.LevelSelectorHideRequested, GameFlowEvents.LevelSelectorHiding },
            // LevelSelectorShowing -> LevelSelectorShown: Published by LevelSelectorPanelController
            // LevelSelectorHiding -> LevelSelectorHidden: Published by LevelSelectorPanelController

            // ================================================================================
            // GAME SESSION BRIDGES
            // ================================================================================
            { GameFlowEvents.GameStartRequested, GameFlowEvents.GameStarting },
            // GameEndRequested -> GameEnding: Published by GameOverController (carries score data)
            // GameStarting -> GameStarted: Published after countdown ends
            // GameEnding -> GameEnded: Published after scenes unloaded

            // ================================================================================
            // SCENE LOADING BRIDGES
            // ================================================================================
            { GameFlowEvents.GameScenesLoadRequested, GameFlowEvents.GameScenesLoading },
            { GameFlowEvents.GameScenesUnloadRequested, GameFlowEvents.GameScenesUnloading },
            // GameScenesLoading -> GameScenesLoaded: Published by scene loader
            // GameScenesUnloading -> GameScenesUnloaded: Published by scene unloader

            // ================================================================================
            // CONFIG / DIFFICULTY BRIDGES
            // ================================================================================
            { GameFlowEvents.GameConfigChangeRequested, GameFlowEvents.GameConfigApplying },
            { GameFlowEvents.DifficultyChangeRequested, GameFlowEvents.DifficultyChanging },
            // GameConfigApplying -> GameConfigApplied: Published by config system
            // DifficultyChanging -> DifficultyChanged: Published by difficulty system
            // DifficultySettingsApplied: Published when RemoteConfig updates difficulty

            // ================================================================================
            // PAUSE / RESUME BRIDGES
            // ================================================================================
            { GameFlowEvents.PauseRequested, GameFlowEvents.Pausing },
            { GameFlowEvents.Pausing, GameFlowEvents.Paused },
            { GameFlowEvents.ResumeRequested, GameFlowEvents.Resuming },
            { GameFlowEvents.Resuming, GameFlowEvents.Resumed },

            // ================================================================================
            // SAVE / LOAD BRIDGES (optional, not currently active)
            // ================================================================================
            //{ GameFlowEvents.SaveLoadRequested, GameFlowEvents.SaveLoading },
            //{ GameFlowEvents.SaveLoading, GameFlowEvents.SaveLoaded },
            //{ GameFlowEvents.SaveRequested, GameFlowEvents.Saving },
            //{ GameFlowEvents.Saving, GameFlowEvents.Saved },

            // ================================================================================
            // QUIT BRIDGE (optional, currently published directly)
            // ================================================================================
            //{ GameFlowEvents.QuitRequested, GameFlowEvents.Quitting },

            // ================================================================================
            // FLOW ORCHESTRATION (cross-phase auto-chains)
            // ================================================================================

            // --- Boot -> Main Menu ---
            // After authentication completes, UGSGameFlowBridge fires GameplayReady
            { GameFlowEvents.GameplayReady, GameFlowEvents.MainMenuShowRequested },

            // --- Level Selected -> Scene Loading ---
            // After user selects a level, proceed to scene loading (existing flow)
            { GameFlowEvents.LevelSelected, GameFlowEvents.GameScenesLoadRequested },

            // --- Scenes Loaded -> Game Start ---
            // After scenes finish loading, start the game (TempleRun countdown lives in TempleRun domain)
            { GameFlowEvents.GameScenesLoaded, GameFlowEvents.GameStartRequested },

            // --- Player Death -> Game End ---
            // After game ends, unload gameplay scenes
            { GameFlowEvents.GameEnding, GameFlowEvents.GameScenesUnloadRequested },

            // --- Game Ended -> back to Main Menu ---
            // In the UGS template the post-game loop returned to the menu via
            // GameEnded -> LeaderboardOpening -> ... -> PlayerAuthenticated -> GameplayReady.
            // This template has no UGS, so close the loop directly: GameEnded re-signals
            // GameplayReady, which already auto-chains to MainMenuShowRequested -> MainMenuShowing.
            // (Previously this only happened via the Test_AutoFireEvent helper object in the scene.)
            { GameFlowEvents.GameEnded, GameFlowEvents.GameplayReady },
        };

        protected virtual void Awake()
        {
            EventsPublisherGameFlow.Instance.SubscribeToAllEnumEvents(AutoFireGameFlowEventFromGameFlowEvent);
        }

        protected virtual void OnDestroy()
        {
            EventsPublisherGameFlow.Instance.UnsubscribeToAllEnumEvents(AutoFireGameFlowEventFromGameFlowEvent);
        }

        private void AutoFireGameFlowEventFromGameFlowEvent(string eventName, object sender, object data)
        {
            ReadOnlySpan<char> input = eventName.AsSpan();
            int index = input.LastIndexOf('/');
            if (index < 0) return;
            string result = input.Slice(index + 1).ToString();
            GameFlowEvents gameFlowEvent = Enum.Parse<GameFlowEvents>(result);
            if (_autoGameFlow2GameFlowEvents.TryGetValue(gameFlowEvent, out GameFlowEvents autoEvent))
            {
                EventsPublisherGameFlow.Instance.PublishEvent(autoEvent, sender, data);
            }
        }
    }
}