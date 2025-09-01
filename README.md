# Endless Runner
_(based on the design goals explained in this template: `crawfis/TempleRun1-NoArt)_

This is an initial project template I use in my CSE 5912: Game Design and Development Capstone course at The Ohio State University
## Main Goals of This Repo
- Provide a file structure
- Provide some basic and useful Editor scripts
- Include some key packages, including the Event Publishing system
- Show an example of using additive scenes
- Show how to have a basic Bootstrap scene
- Show an example of using events for communication and keeping your code clean
- Show how to separate gameplay from the actual visual and audio elements

It relies on three other repos:
- `crawfis/EventsPublisher`: Event system for Unity
- `crawfis/RandomProvider`: Unity tool to set and create random seeds and control the random process
- `crawfis/GTMY.Audio`: Audio Manager Unity package that can use Addressables
---
## Scene Loading Overview
When running, the **BootStrap** scene is loaded first. It additively adds two scenes:
- `TempleRunTrackPCG`: to create the abstract track creation
- `TempleRunGameplay`: has most of the controllers (distance, turning, and others)
`TempleRunGameplay` once loaded will load the remaining **5 other scenes** dealing with graphics, sound, environment and audio. The **8 scenes** and their hierarchies are shown below.


The file KnownEvents has a list of all of the events used in this simple example. There really should be several more: TurnCancelled, TurningStarted, ...

---
## Scenes Loaded when running
### BootStrap
- Temp Camera – needed to avoid problems on Android.
- Global
- Blackboard – Not the best Software Engineering, but useful.
- RandomProvider – Seed, store and provide reproducibility
- EventsPublisher – The main event pump to learn and focus on
- QuitController – Clean up and quit gracefully
- GameConfig – ScriptableObject based game control
- LoadGamePlay – Controls what scenes to load next
- Ullnput – Unity’s Event System and UI Input Module
### TempleRunGameplay
- Controllers
- GameController – Initialize your game and handle player death, etc.
- PauseController – Handle Pause, Resume and Toggle
- DistanceController – Temple Run distance tracker
- TurnController – Handle Turn requests
- CollisionController – Handle failed turns
- DeathWatcher – Player dying and number of lives
- AI – Automatically play / test the game
- TeleportController – Handle a successful turn request
- InputController – Handle the various Input Actions: Play controls and Game controls.
- LoadVisuals – Controls what scenes to load next
### TempleRunTrackPCG
- PathCreation (only one should be enabled)
- TrackManager – Random track lengths
- VoxelTrackManager – Integer constrained track lengths
- TrackLengthList – Track lengths from a specified set (ScriptableObject)
- TrackController – Handles events and direction changes, preserving the overall spline of the track.
### TempleRunVisuals
- Character
- Virtual Camera
- Capsule
### TempleRunTrackVisuals
- TrackSpawner – A plane prefab with a script to stretch it to the track segment size.
- VoxelSpawner – Spawn enough prefabs based on the track length.
- StartingPlatform – A visual to see as the scene loads. Can be deleted.
- Generated Level – Parent object for the resulting track generation
### TempleRunGuiOverlay
- UI – A UI Document that just shows the distance travelled
### TempleRunEnvironment
- Environment
- Main Camera
- Directional Light
- Global Volume
### TempleRunSfx
- Music – One version of the music player available in the Audio package
- Metronome – A speed-based metronome
- TurnFeedback – Sounds to play on successful turns
- Cleanup – Resets the singletons
---
## Game Development Tasks
First, integrate a **main menu**. A starter package project for this that has localization support is here. Use **UI Toolkit** for everything.
- `crawfis/ConsumerUI_RxGames`: Unity Localization Sample
I will talk about how to use **Google Sheets (or Excel)** to create new localization tables, and the scripts included here to fetch translation tables from Google Sheets if there is interest. This example also shows how to load dynamic UI elements (ignore the fake data for that).
You should also create an initial **Credit screen**. Data drive and update it when you use any 3rd party assets. Keep track of asset licenses as well (or url’s to them). Also add these tasks to this scrum:
- Change the **Company** and **Product** and version number (use `0.1.0` format) in the Player Settings.
- Change the **Root namespace** in the Editor Settings
- Determine each team member’s interests. Pick several of the features below to learn about and implement. A few extra features to just research and plan out for future reference. Focus on reusable components while learning some good code structure and how to keep separation of concerns in your software.
Pick ones that you are interested in programming and/or may be used in your final (undefined) game. Create a scrum session for these features or user stories and then break down into (very) small tasks. Tasks for research and design should also be included, particularly for more complex or unknown tasks. The artifacts you develop show your team what you have accomplished.
