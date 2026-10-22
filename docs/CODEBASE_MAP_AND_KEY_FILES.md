# Codebase Map and Key Files

Where things live: the commands, critical paths, namespaces, every key file by area, and the
folder tree. The rules are in [AGENTS.md](../AGENTS.md); the design is in
[ARCHITECTURE.md](ARCHITECTURE.md).

## Quick reference

```
Play in Editor:     Enter Play Mode from Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only
                    (or: unity cmd editor_play / editor_stop)
Event Logging:      CrawfisSoftware > Events > Log Events   (same menu: Clear Now,
                    List Current Subscribers, Clear Events on Exiting Play Mode)
List Domains:       CrawfisSoftware > Events > List Domains   (per domain: prefix, enum,
                    member / payload / sticky / replay counts — EventsPublisher 2.5.0+)
Track Authoring:    Edit the TrackSegmentSO / TrackSegmentRegistrySO / TrackLevelSO /
                    TrackLevelRegistrySO assets under Assets/TempleRun/Scriptables/Track/
                    (one asset per segment in Track/Segments/) via the Inspector
```

## Critical paths

| Purpose | Path |
|---------|------|
| Entry Scene | `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only` |
| GameFlow Events | `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs` |
| TempleRun Events | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `UserInitiatedEvents.cs` |
| Countdown Events | `Assets/Countdown/Scripts/Events/CountdownEvents.cs` |
| Event Bus | `EventsFor<T>` from the `com.crawfissoftware.eventspublisher` package (no per-domain publisher files) |
| Auto-Event Flow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs`, `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`, `Assets/Countdown/Scripts/Events/CountdownAutoEventFlow.cs`, `Assets/_Common/Events/AutoEventFlowBase.cs` |
| Cross-Domain Bridge | `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`, `Assets/GameFlow/Scripts/CountdownSpecific/CountdownGameFlowBridge.cs`, `Assets/Countdown/Scripts/TempleRunSpecific/Countdown2TempleRunBridge.cs` |
| Game State | `Assets/GameFlow/Scripts/Config/GameState.cs`, `Assets/TempleRun/Scripts/Config/Blackboard.cs` |

## Namespaces
```
CrawfisSoftware.Events            - Event-system core: EventsFor<T>/EventId<T> (package) + UserInitiatedEvents + EventHistory
                                    + EventChainDispatcher/AutoEventFlowBase (_Common/Events)
CrawfisSoftware.TempleRun         - Gameplay logic (TempleRunEvents enum, Blackboard, player controllers,
                                    most of Track/ and all of TrackVisuals/, GUIController)
CrawfisSoftware.TempleRun.Events  - TempleRun auto-event flow + Input2TempleRunAutoEventBridge
CrawfisSoftware.TempleRun.Track   - Segment-selection seam only (Track/Selection/); .Track.Geometry holds the spline builders
CrawfisSoftware.TempleRun.*       - Per-area: .Input, .Audio, .PowerUps, .GameConfig, .Editor
CrawfisSoftware.Countdown         - Ceremony logic (CountdownController)
CrawfisSoftware.Countdown.Events  - CountdownEvents, CountdownAutoEventFlow, Countdown2TempleRunBridge
CrawfisSoftware.Countdown.UI      - CountdownUIController
CrawfisSoftware.GameFlow          - Application lifecycle (incl. QuitController, LoadSceneAdditively)
CrawfisSoftware.GameFlow.Events   - GameFlowEvents, GameFlowAutoEventFlow, TempleRunGameFlowBridge, CountdownGameFlowBridge
CrawfisSoftware.GameFlow.UI       - UI controllers
CrawfisSoftware.GameFlow.*        - Per-area: .Config, .GameConfig, .GameControl (UnloadNonActiveScenes only), .SceneManagement
CrawfisSoftware.Config            - Shared, domain-neutral config (DifficultyConfig)
CrawfisSoftware.Utility / .Utilities / .Utility.Testing / .Test - _Common utilities and test helpers
CrawfisSoftware.Scriptables / .AssetManagement / .Spawners - vendored ThirdParty code
```

One known stray declares a namespace that doesn't match its folder:
`Assets/_Common/Utility/DebugLog.cs` (namespace `CrawfisSoftware.TempleRun`). Use the
declared namespace when referencing it. (`PlayerPrefKeys.cs` was the other; it moved to
`Assets/TempleRun/Scripts/Config/` to match its namespace and its only caller.)

## Key files reference

| Category | Files |
|----------|-------|
| **GameFlow Domain** | |
| Event Enums | `Assets/GameFlow/Scripts/Events/GameFlowEvents.cs` |
| Event Bus | `EventsFor<GameFlowEvents>`, aliased as `GameFlowBus` |
| Auto-Event Flow | `Assets/GameFlow/Scripts/Events/GameFlowAutoEventFlow.cs` |
| Bridge | `Assets/GameFlow/Scripts/TempleRunSpecific/TempleRunGameFlowBridge.cs`, `Assets/GameFlow/Scripts/CountdownSpecific/CountdownGameFlowBridge.cs` |
| Game State / Config | `Assets/GameFlow/Scripts/Config/GameState.cs`, `GameConstants.cs`, `LevelConfig.cs`, `LevelConfigApplier.cs`, `LevelRegistry.cs`, `LevelProgressManager.cs`, `LevelProgressData.cs` |
| UI Controllers | `Assets/GameFlow/Scripts/UI/MainMenuController.cs`, `MainMenuPanelController.cs`, `LevelSelectorController.cs`, `LevelSelectorPanelController.cs`, `GameFlowUIPanelController.cs` (loading screen + game-over overlay; the countdown overlay lives in the Countdown domain) |
| UI Toolkit Assets | `Assets/GameFlow/UI Toolkit/UI/UXML/` (MainMenu, LevelSelector, LoadingScreen, GameOver overlay) + USS; `Assets/TempleRun/UI Toolkit/TempleRunDistances.uxml`; `Assets/Countdown/UI Toolkit/Countdown.uxml`; `Assets/UI Toolkit/PlainUI_Styles.uss` (shared stylesheet, used by the TempleRun distance HUD) |
| Game Control | `Assets/GameFlow/Scripts/GameControl/QuitController.cs`, `UnloadNonActiveScenes.cs`, `LoadSceneAdditively.cs` |
| Scene Management | `Assets/GameFlow/Scripts/SceneManagement/DynamicLevelSceneLoader.cs`, `FireEventAfterSceneLoads.cs`, `FireEventWhenSceneCloses.cs`, `CloseSceneOnEvent.cs`, `LoadSceneAfterGameControlEvent.cs` |
| **TempleRun Domain** | |
| Event Enums | `Assets/TempleRun/Scripts/Events/TempleRunEvents.cs`, `UserInitiatedEvents.cs` |
| Event Bus | `EventsFor<TempleRunEvents>` / `EventsFor<UserInitiatedEvents>`, aliased as `TempleRunBus` / `UserInputBus` |
| Auto-Event Flow | `Assets/TempleRun/Scripts/Events/TempleRunAutoEventFlow.cs`, `Input2TempleRunAutoEventBridge.cs` |
| Config | `Assets/TempleRun/Scripts/Config/Blackboard.cs`, `TempleRunGameConfig.cs`, `GameDifficultyManager.cs`, `DifficultySettings.cs`, `SetGameDifficulty.cs`, `LoadDefaultGameConfigs.cs`, `PlayerPrefKeys.cs`, `SpawnPrefabRegistry.cs`, `TempleRunConstants.cs`, per-mechanic configs (`CoinConfig.cs`, `DashConfig.cs`, `JumpConfig.cs`, `LaneConfig.cs`, `SlideConfig.cs`, `PowerUpDefinition.cs`, `PowerUpType.cs`) |
| Player Controllers | `Assets/TempleRun/Scripts/Player/TurnController.cs` (the turn gate only), `JumpController.cs`, `SlideController.cs`, `DashController.cs`, `LaneChangeController.cs`, `PlayerLifeController.cs`, `PowerUpBuffController.cs`, `DistanceController.cs`, `MoveCharacterByDistance.cs`, `PauseController.cs`, `PlayerPauseController.cs`, `AIController.cs` |
| Player Support | `Assets/TempleRun/Scripts/Player/` — collision detectors (`ObstacleCollisionDetector.cs`, `CollectableCollisionDetector.cs`, `TurnCollisionDetector.cs`), `CoinCollectionController.cs`, motion shaping (`JumpArcController.cs`, `SlideArcController.cs`, `DashSpeedController.cs`, `LaneOffsetController.cs`), failure/teleport (`PlayerFailedController.cs`, `PlayerFailureAutoTurnController.cs`, `TeleportController.cs`, `CharacterTeleporter.cs`, `TeleportInfo.cs`); `Assets/TempleRun/Scripts/GameTime.cs` (pausable gameplay clock) |
| Power-Up Effects | `Assets/TempleRun/Scripts/PowerUps/IPowerUpEffect.cs`, `PowerUpEffectBase.cs`, `SpeedBoostEffect.cs`, `ScoreMultiplierEffect.cs`, `CoinMagnetEffect.cs`, `CoinDoublerEffect.cs`, `ShieldEffect.cs` |
| Track Generation | `Assets/TempleRun/Scripts/Track/TurnCommitController.cs` (takes a turn from Starting through the Either-junction commit to Ending), `TrackManager.cs` (+ `TrackManagerAbstract.cs`, `TrackManagerForTiles.cs`, `TrackManagerList.cs` variants), `PathProvider.cs`, `SegmentTransitionController.cs`, `SegmentAdvanceTrigger.cs`, `TrackSegmentLibrary.cs`, `TrackLibraryLoader.cs`, `TrackSegmentInfo.cs`, `Direction.cs`, `DistanceTracker.cs`, `DistanceInterestService.cs`, `SegmentGeometryData.cs`, `SplineSection.cs`; SO classes `TrackSegmentSO.cs`, `TrackSegmentRegistrySO.cs`, `TrackLevelSO.cs`, `TrackLevelRegistrySO.cs` |
| Segment Selection | `Assets/TempleRun/Scripts/Track/Selection/` — `ISegmentSelector.cs` + `ISegmentPool.cs` (pluggable policy seam), `WeightedDifficultySelector.cs` (default, wired in `TrackManager`), `DistanceRampSelector.cs`, `WaveSelector.cs`, `AuthoredSequenceSelector.cs` |
| Track Geometry | `Assets/TempleRun/Scripts/Track/Geometry/` — `IPathSegmentBuilder.cs`, `AxisAligned90Builder.cs`, `ArcTurnBuilder.cs`, `PathPose.cs`, `PathSpan.cs`, `PathSegmentResult.cs`, `CardinalDirections.cs` |
| Spawners | `Assets/TempleRun/Scripts/Track/SpawnerBase.cs`, `CoinSpawner.cs`, `ObstacleSpawner.cs`, `PowerUpSpawner.cs`, `PowerUpIdentifier.cs` |
| Track Visuals | `Assets/TempleRun/Scripts/TrackVisuals/PrefabSpawnerAbstract.cs`, `SimplePlane/SplinePrefabSpawner.cs`, `SimplePlane/TextureScaler.cs`, `Voxels/VoxelPrefabSpawner.cs`, `Voxels/VoxelLaneTrackSpawner.cs` (lane-stretched voxels; the only visual that draws both T-junction arms) |
| Gameplay UI | `Assets/TempleRun/Scripts/UI/GUIController.cs` (distance HUD via UXML) |
| Audio / Animation | `Assets/TempleRun/Scripts/Audio/Metronome.cs`, `TurnAudioFeedback.cs`, `SetMusicPlayer.cs`, `CleanupAudioSingletons.cs` (uses the `com.gamesthatmoveyou.audiomanager` package); `Assets/TempleRun/Scripts/Animation/CapsuleAnimationLink.cs` |
| Track Data | `Assets/TempleRun/Scriptables/Track/` — `Segments/*.asset` (one `TrackSegmentSO` per segment), `TrackSegmentRegistry.asset` (the pool), `TrackLevel_01..05_*.asset` (`TrackLevelSO` per level), `TrackLevelRegistry.asset` |
| Input | `Assets/TempleRun/Scripts/Input/MovementInputActions.cs`, `SwipeDetectorActions.cs`, `DashInputActions.cs`, `AccelerometerInputActions.cs`, `PauseQuitInputActions.cs`; `GameControls.cs` + `LeftRightJumpSlide.cs` are **source-generated** from the `.inputactions` assets — regenerate, don't hand-edit |
| Editor Tools | `Assets/TempleRun/Editor/TrackDataImporter.cs` (one-shot JSON -> SO importer; menu `CrawfisSoftware > Track > Import JSON -> ScriptableObjects`) |
| **Countdown Domain** | |
| Event Enum | `Assets/Countdown/Scripts/Events/CountdownEvents.cs` (`CountdownStartRequested` 0 … `CountdownEnded` 5) |
| Event Bus | `EventsFor<CountdownEvents>`, aliased as `CountdownBus` |
| Auto-Event Flow | `Assets/Countdown/Scripts/Events/CountdownAutoEventFlow.cs` (`StartRequested -> Starting`, `Ending -> Ended`) |
| Bridges | `Assets/GameFlow/Scripts/CountdownSpecific/CountdownGameFlowBridge.cs` (GameFlow -> Countdown), `Assets/Countdown/Scripts/TempleRunSpecific/Countdown2TempleRunBridge.cs` (Countdown -> TempleRun) |
| Ceremony / UI | `Assets/Countdown/Scripts/CountdownController.cs`, `Assets/Countdown/Scripts/UI/CountdownUIController.cs`, `Assets/Countdown/UI Toolkit/Countdown.uxml` |
| **Shared/Common** | |
| Auto-Event Base | `Assets/_Common/Events/AutoEventFlowBase.cs` — `EventChainDispatcher<TSource, TDest>` + `AutoEventFlowBase<TSource, TDest>`; the one dispatch implementation, shared by every flow and bridge class |
| Shared Config | `Assets/_Common/Config/DifficultyConfig.cs` |
| Utilities | `Assets/_Common/Utility/Logger.cs`, `EventLoggerDump.cs`, `DebugEventFileLogger.cs`, `DebugLog.cs`, `TimedEvent.cs`, `TextureExtensions.cs`, `Wait.cs` (`Wait.ForSecondsRealtime`, the unscaled Awaitable wait); `Assets/_Common/Events/EventHistory.cs`; test helpers in `Assets/_Common/Test/` |
| Vendored | `Assets/ThirdParty/CrawfisSoftware/` — Random providers, AssetManagement helpers, editor tools (screenshots, play-first-scene, dev-build toggle), `Spawners/GridSpawner.cs`, `ScriptableInt.cs` |

## Folder structure

Four primary domains with clear separation of concerns:

```
Assets/
├── _Common/                          # Shared infrastructure
│   ├── Config/                       # DifficultyConfig (domain-neutral, namespace CrawfisSoftware.Config)
│   ├── Events/                       # AutoEventFlowBase + EventChainDispatcher, EventHistory
│   ├── Test/                         # Test_AutoFireEvent, Test_AutoFireEventOnStart
│   └── Utility/                      # Logger, EventLoggerDump, DebugEventFileLogger, DebugLog, TimedEvent, TextureExtensions, Wait
│
├── Countdown/                        # Session-ceremony domain (the pre-run 3...2...1)
│   ├── Scripts/
│   │   ├── Events/                   # CountdownEvents, CountdownAutoEventFlow
│   │   ├── TempleRunSpecific/        # Countdown2TempleRunBridge (CountdownEnded -> PlayerActivateRequested)
│   │   ├── UI/                       # CountdownUIController
│   │   └── CountdownController.cs    # Runs the ceremony: Starting, Started, Tick, Ending
│   └── UI Toolkit/                   # Countdown.uxml (the overlay)
│
├── GameFlow/                         # Application lifecycle domain
│   ├── Scripts/
│   │   ├── Events/                   # GameFlowEvents, GameFlowAutoEventFlow
│   │   ├── TempleRunSpecific/        # TempleRunGameFlowBridge (bridges TempleRun <-> GameFlow)
│   │   ├── CountdownSpecific/        # CountdownGameFlowBridge (GameStarting -> CountdownStartRequested)
│   │   ├── Config/                   # GameState, GameConstants, LevelConfig(+Applier), LevelRegistry, LevelProgressManager/Data
│   │   ├── GameControl/              # QuitController, UnloadNonActiveScenes, LoadSceneAdditively
│   │   ├── UI/                       # MainMenu + LevelSelector controllers and panel controllers, GameFlowUIPanelController
│   │   └── SceneManagement/          # DynamicLevelSceneLoader, FireEventAfterSceneLoads/WhenSceneCloses, CloseSceneOnEvent, LoadSceneAfterGameControlEvent
│   ├── Scenes/Boot/                  # 0_BootStrap_Game_Only -> Game_Boot_0_Test_Initialization -> Game_Boot_1_UI -> Game_Boot_2_Play
│   └── UI Toolkit/                   # UXML/USS: MainMenu, LevelSelector, LoadingScreen, GameOver overlay
│
├── TempleRun/                        # Gameplay domain
│   ├── Scripts/
│   │   ├── Events/                   # TempleRunEvents, UserInitiatedEvents, TempleRunAutoEventFlow, Input2TempleRunAutoEventBridge
│   │   ├── Config/                   # Blackboard, TempleRunGameConfig, GameDifficultyManager, PlayerPrefKeys, per-mechanic configs, SpawnPrefabRegistry
│   │   ├── Player/                   # Turn/Jump/Slide/Dash/Lane/Life controllers, collision detectors, distance, pause, teleport, AI
│   │   ├── PowerUps/                 # IPowerUpEffect strategy: PowerUpEffectBase + five concrete effects
│   │   ├── Track/                    # TrackManager (+variants), PathProvider, SegmentTransitionController, spawners, SO classes, TrackLibraryLoader
│   │   │   ├── Geometry/             # IPathSegmentBuilder, AxisAligned90Builder, ArcTurnBuilder, PathPose, PathSpan, PathSegmentResult, CardinalDirections
│   │   │   └── Selection/            # ISegmentSelector/ISegmentPool + Weighted/DistanceRamp/Wave/AuthoredSequence selectors
│   │   ├── TrackVisuals/             # PrefabSpawnerAbstract; SimplePlane/ (spline plane) and Voxels/ (incl. VoxelLaneTrackSpawner)
│   │   ├── Input/                    # Movement/Swipe/Dash/Accelerometer/PauseQuit actions; generated GameControls + LeftRightJumpSlide
│   │   ├── UI/                       # GUIController (distance HUD)
│   │   ├── Audio/                    # Metronome, TurnAudioFeedback, SetMusicPlayer, CleanupAudioSingletons
│   │   ├── Animation/                # CapsuleAnimationLink
│   │   └── GameTime.cs               # Pausable gameplay clock (singleton)
│   ├── Scenes/                       # Gameplay/: TempleRunGameplay, TempleRunTrackPCG, TempleRunTrackVisuals, TempleRunPlayerVisuals,
│   │                                 #   TempleRunObstacles, TempleRunCollectables, TempleRunEnvironment, TempleRunSfx, TempleRunGuiOverlay;
│   │                                 #   PrefabScene (authoring-only, not in Build Settings)
│   ├── Scriptables/                  # Per-mechanic config assets; Track/ (Segments/*.asset, TrackSegmentRegistry, TrackLevel_01..05,
│   │                                 #   TrackLevelRegistry); Levels/ (GameFlow's LevelConfig assets + LevelRegistry live here)
│   ├── UI Toolkit/                   # TempleRunDistances.uxml
│   └── Editor/                       # TrackDataImporter (one-shot JSON -> SO converter)
│
├── UI Toolkit/                       # PlainUI_Styles.uss (shared stylesheet; the TempleRun distance HUD uses it)
│
└── ThirdParty/CrawfisSoftware/       # Vendored utilities: Random providers, AssetManagement helpers, editor tools, GridSpawner
```
