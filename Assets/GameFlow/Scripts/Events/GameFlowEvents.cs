namespace CrawfisSoftware.GameFlow.Events
{
    public enum GameFlowEvents
    {
        // ---------- Loading Screen ----------
        LoadingScreenShowRequested = 0,
        LoadingScreenShowing = 1,
        LoadingScreenShown = 2,
        LoadingScreenHideRequested = 3,
        LoadingScreenHiding = 4,
        LoadingScreenHidden = 5,

        // ---------- Main Menu ----------
        MainMenuShowRequested = 10,
        MainMenuShowing = 11,
        MainMenuShown = 12,
        MainMenuHideRequested = 13,
        MainMenuHiding = 14,
        MainMenuHidden = 15,

        // ---------- Game Session (Menu <-> Run) ----------
        GameStartRequested = 20,
        GameStarting = 21,
        GameStarted = 22,

        GameEndRequested = 23,
        GameEnding = 24,
        GameEnded = 25,

        RestartRequested = 26,
        ReturnToMainMenuRequested = 27,

        // ---------- Scenes (Additive/Async friendly) ----------
        GameScenesUnloadRequested = 30,
        GameScenesUnloading = 31,
        GameScenesUnloaded = 32,
        GameScenesUnloadFailed = 33,

        GameScenesLoadRequested = 34,
        GameScenesLoading = 35,
        GameScenesLoaded = 36,
        GameScenesLoadFailed = 37,

        GameScenesActivating = 38,
        GameScenesActivated = 39,

        // ---------- Gameplay Lifecycle ----------
        GameplayPreparing = 50,   // pooling/spawn/warmup/bind systems
        GameplayReady = 51,       // safe to start countdown / accept start
        GameplayNotReady = 52,    // disable input / block start

        GameplayStarting = 53,    // enabling player control, etc.
        GameplayStarted = 54,
        GameplayEnding = 55,
        GameplayEnded = 56,

        // ---------- Pause Lifecycle ----------
        PauseRequested = 60,
        Pausing = 61,
        Paused = 62,

        ResumeRequested = 63,
        Resuming = 64,
        Resumed = 65,

        // ---------- Config / Difficulty ----------
        GameConfigChangeRequested = 80,
        GameConfigApplying = 81,
        GameConfigApplied = 82,
        GameConfigApplyFailed = 83,
        TrackConfigApplied = 85,              // data: string (Resources path of track level JSON)

        DifficultyChangeRequested = 90,
        DifficultyChanging = 91,
        DifficultyChanged = 92,
        DifficultyChangeFailed = 93,
        DifficultySettingsApplied = 94,

        // ---------- Save / Load (optional but useful hooks) ----------
        SaveLoadRequested = 100,
        SaveLoading = 101,
        SaveLoaded = 102,
        SaveLoadFailed = 103,

        SaveRequested = 110,
        Saving = 111,
        Saved = 112,
        SaveFailed = 113,

        // ---------- Quit ----------
        QuitRequested = 120,
        Quitting = 121,
        QuitCancelled = 122,
        QuitCompleted = 123,

        // ---------- Level Selector ----------
        LevelSelectorShowRequested = 130,
        LevelSelectorShowing = 131,
        LevelSelectorShown = 132,
        LevelSelectorHideRequested = 133,
        LevelSelectorHiding = 134,
        LevelSelectorHidden = 135,
        LevelSelected = 136,              // data: LevelConfig
        LevelUnlocked = 137,              // data: LevelConfig (newly unlocked)
        LevelProgressSaved = 138,
    }
}