// Remove this when making release versions to not include the test scene in the build
#define TEST_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using SuperspectiveUtils;
using Saving;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace LevelManagement {
	public static class LevelsEnumExt {
		public static string ToName(this Levels level) {
			return LevelManager.enumToSceneName[level];
		}

		public static Levels ToLevel(this string levelName) {
			return LevelManager.enumToSceneName[levelName];
		}

		public static string ToDisplayName(this Levels enumValue) {
			return LevelManager.instance.levels[enumValue].displayName;
		}
		
		public static bool IsTestingLevel(this Levels level) {
			return level is Levels.PortalTestScene or Levels.PowerTrailTestScene or Levels.TestScene or Levels.SuperspectiveObjectTestScene;
		}
		
		public static bool IsValid(this Levels enumValue) => enumValue is not Levels.InvalidLevel;
	}
	
	// When adding a new Level to this enum, make sure you also add it to the LevelManager inspector,
	// and add the scene to Build Settings as well
	// ALSO NOTE: Be careful not to fuck up the serialization
	// Next level: 40
	[Serializable]
	public enum Levels {
		InvalidLevel = -1,
		TitleCard = 29,
		ManagerScene = 0,
		TestScene = 1,
		PortalTestScene = 17,
		PowerTrailTestScene = 27,
		SuperspectiveObjectTestScene = 37,
		EmptyRoom = 2,
		HexPillarRoom = 3,
		Library = 4,
		Level3 = 5,
		TutorialHallway = 7,
		TutorialRoom = 8,
		Transition23 = 9,
		Transition34 = 10,
		Axis = 11,
		Fork = 12,
		ForkWhiteRoom = 13,
		ForkBlackRoom = 14,
		InvisFloor = 15,
		MetaEdgeDetection = 16,
		ForkCathedral = 18,
		ForkWhiteRoomBlackHallway = 19,
		RoseRoom = 20,
		RoseRoomExit = 38,
		RoseRoomExit2 = 39,
		TransitionWhiteRoomFork = 21,
		ForkOctagon = 22,
		ForkBlackRoom2 = 23,
		WhiteRoom1BackRoom = 24,
		BehindForkTransition = 25,
		ForkBlackRoom3 = 26,
		ForkCathedralTutorial = 6,
		GrowShrinkIntro = 28,
		GrowShrinkIntroBetweenWorlds = 30,
		GrowShrinkIntroDarkSide = 31,
		TransitionWhiteRoom3GrowShrinkIntro = 32,
		GrowShrink2 = 34,
		Ascension0 = 35,
		Ascension1 = 36,
		EdgeOfAUniverse = 33,
	}

	public class LevelManager : SingletonSuperspectiveObject<LevelManager, LevelManager.LevelManagerSave> {
		[OnValueChanged(nameof(ChangeLevelInEditor))]
		public Levels startingScene;

		public const Levels newGameStartingScene = Levels.Fork;

		bool initialized = false;

#region PlayerDefaultLocations
		private DefaultPlayerSettings _defaultPlayerSettings;

		private DefaultPlayerSettings DefaultPlayerSettings {
			get {
				if (_defaultPlayerSettings == null || _defaultPlayerSettings.IsEmpty) {
					_defaultPlayerSettings = DefaultPlayerSettings.LoadFromDisk();
				}
				
				return _defaultPlayerSettings;
			}
		}
		public bool defaultPlayerPosition = false;
		bool hasLoadedDefaultPlayerPosition = false;

		[Button("Set default player position")]
		void SetDefaultPlayerPositionForScene() {
			DefaultPlayerSettings.SetDefaultPlayerPositionForScene(Application.isPlaying ? activeSceneName.ToLevel() : startingScene);
		}

		public void LoadDefaultPlayerPosition(Levels level) {
			_defaultPlayerSettings = DefaultPlayerSettings.LoadFromDisk();
			if (DefaultPlayerSettings.PlayerSettingsByLevel.ContainsKey(level)) {
				DefaultPlayerSettings.PlayerSettingsByLevel[level].Apply();
				// ReSharper disable once Unity.NoNullPropagation (Player is never deleted)
				Player.instance.cameraFollow?.RecalculateWorldPositionLastFrame();
			}
		}
		
		string GetSceneName() {
			string sceneName = activeSceneName;
			if (!Application.isPlaying) {
				sceneName = startingScene.ToName();
			}

			return sceneName;
		}

		[Button("Load default player position")]
		public void ChangeLevelInEditor() {
#if !UNITY_EDITOR
			return;
#endif

			if (!defaultPlayerPosition || hasLoadedDefaultPlayerPosition) return;
			
			string sceneName = GetSceneName();
			LoadDefaultPlayerPosition(sceneName.ToLevel());
			
			// Hijacking this to display level banner on load, even when it's already the active scene
			LevelChangeBanner.instance.PlayBanner(enumToSceneName[sceneName]);
#if UNITY_EDITOR

			string GetScenePathByName(string sceneName) {
				foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes) {
					if (editorScene.enabled && Path.GetFileNameWithoutExtension(editorScene.path) == sceneName) {
						return editorScene.path;
					}
				}

				return default;
			}
			
			void LoadSceneInEditor(string sceneName) {
				try {
					Scene scene = EditorSceneManager.GetSceneByName(sceneName);
					if (!scene.IsValid()) {
						bool isPrototypeScene = sceneName.ToLevel().IsTestingLevel();
						scene = EditorSceneManager.OpenScene(GetScenePathByName(sceneName), OpenSceneMode.Additive);
					}
					EditorSceneManager.SetActiveScene(scene);
				}
				catch (Exception e) {
					Debug.LogError(e);
				}
			}
	
			void UnloadUnrelatedScenes(string sceneName) {
				HashSet<Levels> connectedLevels = allLevels.Find(l => l.level == startingScene).connectedLevels.ToHashSet();
				foreach (var level in allLevels) {
					if (level.level == Levels.ManagerScene || level.level == sceneName.ToLevel()) continue;
	
					try {
						if (!connectedLevels.Contains(level.level)) {
							Scene scene = EditorSceneManager.GetSceneByName(level.level.ToName());
							if (scene.IsValid()) {
								EditorSceneManager.SaveScene(scene);
								EditorSceneManager.CloseScene(scene, true);
							}
						}
					}
					catch (Exception e) {
						Debug.LogError(e);
					}
				}
			}
	
			if (!Application.isPlaying) {
				UnloadUnrelatedScenes(sceneName);
				foreach (var connectedLevel in allLevels.Find(l => l.level == startingScene).connectedLevels) {
					LoadSceneInEditor(connectedLevel.ToName());
				}
				LoadSceneInEditor(sceneName);
			}
	
	
			if (EditorApplication.isPlaying)
#endif
			hasLoadedDefaultPlayerPosition = true;
		}
#endregion
		
		[SerializeField]
		public List<Level> allLevels;
		// levels is allLevels, keyed by Level enum value, but with test scenes removed in build
		public Dictionary<Levels, Level> levels;
		public static readonly TwoWayDictionary<Levels, string> enumToSceneName = new TwoWayDictionary<Levels, string>() {
			{ Levels.InvalidLevel, "!!! Invalid Level !!!"}, // Flag for invalid level
			{ Levels.TitleCard, "__TitleCard" },
			{ Levels.ManagerScene, ManagerScene },
			{ Levels.TestScene, "_TestScene" },
			{ Levels.SuperspectiveObjectTestScene, "SuperspectiveObjectTest" },
			{ Levels.PortalTestScene, "PortalTestScene" },
			{ Levels.PowerTrailTestScene, "PowerTrailTestScene" },
			{ Levels.EmptyRoom, "_EmptyRoom" },
			{ Levels.HexPillarRoom, "_HexPillarRoom" },
			{ Levels.Library, "_Library" },
			{ Levels.Level3, "_Level3" },
			{ Levels.Axis, "_Axis" },
			{ Levels.Fork, "_Fork" },
			{ Levels.ForkOctagon, "_ForkOctagon" },
			{ Levels.ForkWhiteRoom, "_Fork_WhiteRoom" },
			{ Levels.WhiteRoom1BackRoom, "_WhiteRoom1_BackRoom" },
			{ Levels.TransitionWhiteRoomFork, "_TransitionWhiteRoom_Fork" },
			{ Levels.RoseRoom, "_RoseRoom" },
			{ Levels.RoseRoomExit, "_RoseRoomExit" },
			{ Levels.RoseRoomExit2, "_RoseRoomExit_2" },
			{ Levels.ForkCathedral, "_Fork_Cathedral" },
			{ Levels.ForkWhiteRoomBlackHallway, "_WhiteRoom_BlackHallway" },
			{ Levels.BehindForkTransition, "_BehindForkTransition" },
			{ Levels.ForkBlackRoom, "_Fork_BlackRoom" },
			{ Levels.ForkBlackRoom2, "_Fork_BlackRoom2" },
			{ Levels.InvisFloor, "_InvisFloor" },
			{ Levels.TutorialHallway, "_TutorialHallway" },
			{ Levels.TutorialRoom, "_TutorialRoom" },
			{ Levels.Transition23, "_Transition2_3" },
			{ Levels.Transition34, "_Transition3_4" },
			{ Levels.MetaEdgeDetection, "_Meta_EdgeDetection" },
			{ Levels.ForkBlackRoom3, "_Fork_BlackRoom3" },
			{ Levels.ForkCathedralTutorial, "_Fork_Cathedral_Tutorial" },
			{ Levels.GrowShrinkIntro, "_GrowShrinkIntro" },
			{ Levels.GrowShrinkIntroBetweenWorlds, "_GrowShrinkIntroBetweenWorlds" },
			{ Levels.GrowShrinkIntroDarkSide, "_GrowShrinkIntroDarkSide" },
			{ Levels.TransitionWhiteRoom3GrowShrinkIntro, "_TransitionWhiteRoom3GrowShrinkIntro" },
			{ Levels.GrowShrink2, "_GrowShrink2" },
			{ Levels.Ascension0, "_Ascension0" },
			{ Levels.Ascension1, "_Ascension1" },
			{ Levels.EdgeOfAUniverse, "_EdgeOfAUniverse" }
		};
		public string activeSceneName;
		public Levels ActiveScene => activeSceneName.ToLevel();
		public List<Levels> loadedLevels;
		public List<Levels> currentlyLoadingLevels;
		public List<Levels> currentlyUnloadingLevels;
		QueuedSceneSwitch queuedActiveSceneSwitch;
		
		public bool isCurrentlySwitchingScenes;

		[Serializable]
		public class QueuedSceneSwitch {
			readonly Levels level;
			readonly bool playBanner;
			readonly bool saveDeactivatedScenesToDisk;
			readonly bool loadActivatedScenesFromDisk;
			readonly bool checkActiveSceneName;

			[NonSerialized]
			private readonly Action callback;

			public QueuedSceneSwitch(
				Levels level,
				bool playBanner = true,
				bool saveDeactivatedScenesToDisk = true,
				bool loadActivatedScenesFromDisk = true,
				bool checkActiveSceneName = true,
				Action callback = null
			) {
				this.level = level;
				this.playBanner = playBanner;
				this.saveDeactivatedScenesToDisk = saveDeactivatedScenesToDisk;
				this.loadActivatedScenesFromDisk = loadActivatedScenesFromDisk;
				this.checkActiveSceneName = checkActiveSceneName;
				this.callback = callback;
			}

			public void Invoke(DebugLogger debug) {
				debug.LogWarning($"Queued level change happening now for {level}");
				LevelManager.instance.SwitchActiveSceneNow(level, playBanner, saveDeactivatedScenesToDisk, loadActivatedScenesFromDisk, checkActiveSceneName);
			}
		}

		public bool IsCurrentlySwitchingScenes => isCurrentlySwitchingScenes || ScenesAreLoading;

		private bool ScenesAreLoading => currentlyLoadingLevels.Count > 0 || currentlyUnloadingLevels.Count > 0;

		/// <summary>
		/// Order of events:
		/// 1) BeforeActiveSceneChange
		/// 2) (if saving) BeforeSceneSerializeState - foreach scene being unloaded
		/// 3) BeforeSceneUnload - foreach scene being unloaded
		/// 4) BeforeSceneLoad - for the scene becoming active, if it's not already loaded
		/// 5) BeforeSceneLoad - foreach connected scene being loaded
		/// --- Awake(), possibly Start() happens here for newly loaded objects ---
		/// 5.5) OnActiveSceneChange - just before AfterSceneLoad, only for the active scene
		/// 6) AfterSceneLoad/AfterSceneUnload - in order that the scenes are loaded in from SceneManager
		/// 7) (if load from disk) BeforeSceneRestoreDynamicObjects - foreach scene loaded
		/// 8) (if load from disk) BeforeSceneRestoreState - foreach scene loaded
		/// 9) (if load from disk) AfterSceneRestoreState - foreach scene loaded
		/// </summary>
		public delegate void ActiveSceneChange();

		// Called at the very end of the scene change process, after active scene has already changed
		public event ActiveSceneChange OnActiveSceneChange;

		public delegate void ActiveSceneWillChange(Levels nextLevel);

		public event ActiveSceneWillChange BeforeActiveSceneChange;

		public delegate void SceneLoadUnload(Levels level);

		public event SceneLoadUnload BeforeSceneUnload;
		public event SceneLoadUnload BeforeSceneLoad;
		public event SceneLoadUnload AfterSceneUnload;
		public event SceneLoadUnload AfterSceneLoad;
		public event SceneLoadUnload BeforeSceneRestoreDynamicObjects;
		public event SceneLoadUnload BeforeSceneRestoreState;
		public event SceneLoadUnload AfterSceneRestoreState;
		public event SceneLoadUnload BeforeSceneSerializeState;

		public const string ManagerScene = "__ManagerScene";

		protected override void Awake() {
			base.Awake();
			hasLoadedDefaultPlayerPosition = false;

			loadedLevels = new List<Levels>();
			currentlyLoadingLevels = new List<Levels>();
			currentlyUnloadingLevels = new List<Levels>();

#if UNITY_EDITOR || TEST_BUILD
			levels = allLevels.ToDictionary(l => l.level, l => l);
#else
			levels = allLevels.Where(l => l.level.ToName().ToLower().Contains("test")).ToDictionary(l => l.level, l => l);
#endif
			
#if UNITY_EDITOR
			PopulateAlreadyLoadedScenes();
#endif
		}

		protected override void Start() {
			base.Start();

			StartCoroutine(nameof(StartCoro));
		}

		protected IEnumerator StartCoro() {
			SceneManager.sceneLoaded += (scene, mode) => FinishLoadingScene(scene);
			SceneManager.sceneLoaded += (scene, mode) => { ChangeLevelInEditor(); };
			SceneManager.sceneUnloaded += FinishUnloadingScene;

			yield return new WaitUntil(() => GameManager.instance.settingsHaveLoaded);

#if !UNITY_EDITOR
			if (!initialized) {
				if (GameManager.firstLaunch && Settings.Autoload.AutoloadEnabled.value) {
					SaveFileUtils.ReadAllSavedMetadataWithScreenshot((metadata) => {
						SaveMetadataWithScreenshot mostRecentlyLoadedSave = metadata.Find(m => m.metadata.lastLoadedTimestamp == metadata.Max(m => m.metadata.lastLoadedTimestamp));
			
						DateTime now = DateTime.Now;
						DateTime lastSaveDateTime = mostRecentlyLoadedSave == null ? now : new DateTime(mostRecentlyLoadedSave.metadata.saveTimestamp);
						int autoloadDaysThreshold = (int)Settings.Autoload.AutoloadThreshold.dropdownSelection.selection.Datum;
						if (autoloadDaysThreshold == -1) autoloadDaysThreshold = int.MaxValue; // -1 is a flag for "infinity"
						bool lastSaveWasRecent = (now - lastSaveDateTime).Days < autoloadDaysThreshold;

						if (mostRecentlyLoadedSave != null && lastSaveWasRecent) {
							// Don't loop this logic forever just do it once
							initialized = true;
							SaveManager.Load(mostRecentlyLoadedSave);
						}
						else {
							SwitchActiveScene(startingScene, true, false, false, false);
							initialized = true;
						}
					});
				}
				else {
					SwitchActiveScene(startingScene, true, false, false, false);
					initialized = true;
				}
			}
#else
			SwitchActiveScene(startingScene, true, false, false, false);
			initialized = true;
#endif
		}

		void Update() {
			if (!initialized) return;

			if (queuedActiveSceneSwitch != null && !IsCurrentlySwitchingScenes) {
				queuedActiveSceneSwitch.Invoke(debug);
				queuedActiveSceneSwitch = null;
			}
		}

		/// <summary>
		/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
		/// If we are currently loading already, queue up this scene change instead to be started once the previous one finishes.
		/// </summary>
		/// <param name="level">Enum value of the scene to become active</param>
		/// <param name="playBanner">Whether or not to play the LevelBanner. Defaults to true.</param>
		/// <param name="saveDeactivatedScenesToDisk">Whether or not to save any scenes that deactivated to disk. Defaults to true</param>
		/// <param name="loadActivatedScenesFromDisk">Whether or not to load any scenes from disk that become activated. Defaults to true</param>
		/// <param name="checkActiveSceneName">If true, will skip loading the scene if it's already the active scene. False will force it to load the scene. Defaults to true.</param>
		public void SwitchActiveScene(
			Levels level,
			bool playBanner = true,
			bool saveDeactivatedScenesToDisk = true,
			bool loadActivatedScenesFromDisk = true,
			bool checkActiveSceneName = true,
			Action onFinishCallback = null
		) {
			if (IsCurrentlySwitchingScenes) {
				queuedActiveSceneSwitch = new QueuedSceneSwitch(level, playBanner, saveDeactivatedScenesToDisk, loadActivatedScenesFromDisk, checkActiveSceneName, onFinishCallback);
				debug.LogWarning($"Queued scene switch to {level}");
			}
			else {
				SwitchActiveSceneNow(level, playBanner, saveDeactivatedScenesToDisk, loadActivatedScenesFromDisk, checkActiveSceneName, onFinishCallback);
			}
		}

		/// <summary>
		/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
		/// </summary>
		/// <param name="level">Level to become active</param>
		/// <param name="playBanner">Whether or not to play the LevelBanner. Defaults to true.</param>
		/// <param name="saveDeactivatedScenesToDisk">Whether or not to save any scenes that deactivated to disk. Defaults to true</param>
		/// <param name="loadActivatedScenesFromDisk">Whether or not to load any scenes from disk that become activated. Defaults to true</param>
		/// <param name="checkActiveSceneName">If true, will skip loading the scene if it's already the active scene. False will force it to load the scene. Defaults to true.</param>
		/// <param name="onFinishCallback">Callback to be invoked once the scene switch is complete.</param>
		async void SwitchActiveSceneNow(
			Levels level,
			bool playBanner = true,
			bool saveDeactivatedScenesToDisk = true,
			bool loadActivatedScenesFromDisk = true,
			bool checkActiveSceneName = true,
			Action onFinishCallback = null
		) {
			if (!levels.ContainsKey(level)) {
				debug.LogError("No level name found in world graph for " + level);
				return;
			}

			if (checkActiveSceneName && ActiveScene == level) {
				debug.LogWarning("Level " + level + " already the active scene.");
				return;
			}

			isCurrentlySwitchingScenes = true;
			// Immediately turn on the loading icon instead of waiting for a frame into the loading
			LoadingIcon.instance.ShowLoadingIcon();
			
			debug.Log($"Switching to level {level}");

			try {
				BeforeActiveSceneChange?.Invoke(level);
			}
			catch (Exception e) {
				Debug.LogError($"Error during BeforeActiveSceneChange: {e.Message}: {e.StackTrace}");
			}

			activeSceneName = level.ToName();

			if (playBanner) {
				LevelChangeBanner.instance.PlayBanner(enumToSceneName[activeSceneName]);
			}

			// First unload any scene no longer needed
			DeactivateUnrelatedScenes(level, saveDeactivatedScenesToDisk);

			List<Levels> levelsToBeLoadedFromDisk = new List<Levels>();

			// Then load the level if it's not already loaded
			if (!(loadedLevels.Contains(level) || currentlyLoadingLevels.Contains(level))) {
				if (ShouldLoadLevel(level)) {
					currentlyLoadingLevels.Add(level);

					try {
						BeforeSceneLoad?.Invoke(level);
					}
					catch (Exception e) {
						Debug.LogError($"Error during BeforeSceneLoad({level}): {e.Message}: {e.StackTrace}");
					}

					levelsToBeLoadedFromDisk.Add(level);
					SceneManager.LoadSceneAsync(level.ToName(), LoadSceneMode.Additive);
				}
			}
			else {
				if (!hasLoadedDefaultPlayerPosition) {
					ChangeLevelInEditor();
				}
			}

			// Then load the adjacent scenes if they're not already loaded
			List<Levels> connectedLevels = levels[level].connectedLevels;
			foreach (var connectedLevel in connectedLevels) {
				if (!(loadedLevels.Contains(connectedLevel) ||
				      currentlyLoadingLevels.Contains(connectedLevel))) {
					if (ShouldLoadLevel(connectedLevel)) {
						currentlyLoadingLevels.Add(connectedLevel);

						try {
							BeforeSceneLoad?.Invoke(connectedLevel);
						}
						catch (Exception e) {
							Debug.LogError($"Error during BeforeSceneLoad({connectedLevel}): {e.Message}: {e.StackTrace}");
						}
						levelsToBeLoadedFromDisk.Add(connectedLevel);
						SceneManager.LoadSceneAsync(connectedLevel.ToName(), LoadSceneMode.Additive);
					}
				}
			}

			debug.Log("Waiting for scenes to be loaded...");
			await TaskEx.WaitUntil(() => !LevelManager.instance.ScenesAreLoading);
			debug.Log("All scenes loaded into memory" + (loadActivatedScenesFromDisk ? ", loading save..." : "."));

			SceneManager.SetActiveScene(SceneManager.GetSceneByName(activeSceneName));

			// Restore the state for the loaded scenes
			if (loadActivatedScenesFromDisk && levelsToBeLoadedFromDisk.Count > 0) {
				// BeforeSceneRestoreDynamicObjects event
				foreach (var levelToBeLoaded in levelsToBeLoadedFromDisk) {
					try {
						BeforeSceneRestoreDynamicObjects?.Invoke(levelToBeLoaded);
					}
					catch (Exception e) {
						Debug.LogError($"Error during BeforeSceneRestoreDynamicObjects({levelToBeLoaded}): {e.Message}: {e.StackTrace}");
					}
				}

				// Restore DynamicObjects state for each loaded scene
				foreach (var levelToBeLoaded in levelsToBeLoadedFromDisk) {
					SaveManager.SaveManagerForLevel saveManagerForLevel = SaveManager.GetOrCreateSaveManagerForLevel(levelToBeLoaded);
					saveManagerForLevel.RestoreDynamicObjectStateForScene();
				}

				// BeforeSceneRestoreState event
				foreach (var levelToBeLoaded in levelsToBeLoadedFromDisk) {
					try {
						BeforeSceneRestoreState?.Invoke(levelToBeLoaded);
					}
					catch (Exception e) {
						Debug.LogError($"Error during BeforeSceneRestoreState({levelToBeLoaded}): {e.Message}: {e.StackTrace}");
					}
				}

				// Restore SuperspectiveObject state for each loaded scene
				foreach (var levelToBeLoaded in levelsToBeLoadedFromDisk) {
					SaveManager.SaveManagerForLevel saveManagerForLevel = SaveManager.GetOrCreateSaveManagerForLevel(levelToBeLoaded);
					saveManagerForLevel.RestoreSuperspectiveObjectStateForLevel();
				}

				// AfterSceneRestoreState event
				foreach (var levelToBeLoaded in levelsToBeLoadedFromDisk) {
					try {
						AfterSceneRestoreState?.Invoke(levelToBeLoaded);
					}
					catch (Exception e) {
						Debug.LogError($"Error during AfterSceneRestoreState({levelToBeLoaded}): {e.Message}: {e.StackTrace}");
					}
				}
			}
			
			try {
				OnActiveSceneChange?.Invoke();
			}
			catch (Exception e) {
				Debug.LogError($"Error during OnActiveSceneChange: {e.Message}: {e.StackTrace}");
			}
			try {
				onFinishCallback?.Invoke();
			}
			catch (Exception e) {
				Debug.LogError($"Error during onFinishCallback: {e.Message}: {e.StackTrace}");
			}
			
			isCurrentlySwitchingScenes = false;
		}

		/// <summary>
		/// Unloads any scene that is not the selected scene or connected to it as defined by the world graph.
		/// </summary>
		/// <param name="forLevel"></param>
		void DeactivateUnrelatedScenes(Levels forLevel, bool serializeDeactivatingScenes) {
			List<Levels> levelsToDeactivate = new List<Levels>();
			foreach (Levels currentlyActiveLevel in loadedLevels) {
				if (currentlyActiveLevel != forLevel &&
				    !levels[forLevel].connectedLevels.Exists(l => l == currentlyActiveLevel)) {
					levelsToDeactivate.Add(currentlyActiveLevel);
				}
			}

			if (serializeDeactivatingScenes) {
				foreach (var levelToDeactivate in levelsToDeactivate) {
					try {
						BeforeSceneSerializeState?.Invoke(levelToDeactivate);
					}
					catch (Exception e) {
						Debug.LogError($"Error during BeforeSceneSerializeState: {e.Message}: {e.StackTrace}");
					}
				}
			}

			if (serializeDeactivatingScenes) {
				foreach (Levels levelToDeactivate in levelsToDeactivate) {
					SaveManager.SaveManagerForLevel saveForLevel = SaveManager.GetOrCreateSaveManagerForLevel(levelToDeactivate);
					saveForLevel.SerializeSceneState();
				}
			}

			// Update internal state before starting any unload scene calls
			foreach (var levelToDeactivate in levelsToDeactivate) {
				loadedLevels.Remove(levelToDeactivate);
				currentlyUnloadingLevels.Add(levelToDeactivate);
			}

			foreach (var levelToDeactivate in levelsToDeactivate) {
				try {
					BeforeSceneUnload?.Invoke(levelToDeactivate);
				}
				catch (Exception e) {
					Debug.LogError($"Error during BeforeSceneUnload: {e.Message}: {e.StackTrace}");
				}
			}

			foreach (var levelToDeactivate in levelsToDeactivate) {
				SceneManager.UnloadSceneAsync(levelToDeactivate.ToName());
			}
		}

		/// <summary>
		/// Callback for a finished async level load.
		/// Marks scene as active if it's name matches activeSceneName.
		/// Removes scene name from currentlyLoadingSceneNames and adds it to loadedSceneNames.
		/// </summary>
		/// <param name="loadedScene">Scene that finished loading</param>
		void FinishLoadingScene(Scene loadedScene) {
			if (loadedScene.name == ManagerScene) {
				return;
			}

			Levels loadedLevel = loadedScene.name.ToLevel();

			try {
				if (loadedLevel is Levels.InvalidLevel) {
					throw new Exception($"Invalid scene loaded: {loadedScene.name}");
				}
				AfterSceneLoad?.Invoke(loadedScene.name.ToLevel());
			}
			catch (Exception e) {
				Debug.LogError($"Error during AfterSceneLoad({loadedScene.name}): {e.Message}: {e.StackTrace}");
			}

			if (currentlyLoadingLevels.Contains(loadedLevel)) {
				currentlyLoadingLevels.Remove(loadedLevel);
			}

			if (!loadedLevels.Contains(loadedLevel)) {
				loadedLevels.Add(loadedLevel);
			}
		}

		/// <summary>
		/// Callback for a finished async level unload.
		/// Removes the scene from currentlyUnloadingSceneNames.
		/// </summary>
		/// <param name="unloadedScene">Scene that finished unloading</param>
		void FinishUnloadingScene(Scene unloadedScene) {
			if (unloadedScene.name == activeSceneName) {
				debug.LogError("Just unloaded the active scene!");
			}

			Levels unloadedLevel = unloadedScene.name.ToLevel();
			
			try {
				if (unloadedLevel is Levels.InvalidLevel) {
					throw new Exception($"Invalid scene unloaded: {unloadedScene.name}");
				}
				AfterSceneUnload?.Invoke(unloadedLevel);
			}
			catch (Exception e) {
				Debug.LogError($"Error during AfterSceneUnload({unloadedScene.name}): {e.Message}: {e.StackTrace}");
			}

			if (currentlyUnloadingLevels.Contains(unloadedLevel)) {
				currentlyUnloadingLevels.Remove(unloadedLevel);
			}
		}

		bool ShouldLoadLevel(Levels levelToMaybeLoad) {
			return levelToMaybeLoad != Levels.ManagerScene && !SceneManager.GetSceneByName(levelToMaybeLoad.ToName()).isLoaded;
		}

#if UNITY_EDITOR
		/// <summary>
		/// When ran from the Editor, checks every scene in the build settings to see which are loaded.
		/// Any already loaded levels are added to the loadedSceneNames list.
		/// Manager scene is left out of scene management.
		/// </summary>
		void PopulateAlreadyLoadedScenes() {
			foreach (var scene in EditorBuildSettings.scenes) {
				Scene alreadyLoadedScene = SceneManager.GetSceneByPath(scene.path);
				if (alreadyLoadedScene.IsValid() && alreadyLoadedScene.name != ManagerScene) {
					loadedLevels.Add(alreadyLoadedScene.name.ToLevel());
				}
			}
		}
#endif

#region Saving
		public override void LoadSave(LevelManagerSave save) {
			initialized = save.initialized;
			queuedActiveSceneSwitch = save.queuedActiveSceneSwitch;
			SwitchActiveScene(save.activeScene.ToLevel(), false, false, false, false);
		}
		
		[Serializable]
		public class LevelManagerSave : SaveObject<LevelManager> {
			public bool initialized;
			public string activeScene;
			public QueuedSceneSwitch queuedActiveSceneSwitch;

			public LevelManagerSave(LevelManager levelManager) : base(levelManager) {
				this.initialized = levelManager.initialized;
				this.activeScene = levelManager.activeSceneName;
				this.queuedActiveSceneSwitch = levelManager.queuedActiveSceneSwitch;
			}
		}
#endregion
	}
}