// Remove this when making release versions to not include the test scene in the build
#define TEST_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using SuperspectiveUtils;
using NaughtyAttributes;
using ObjectSerializationUtils;
using Saving;
using SerializableClasses;
using static Saving.SaveManagerForScene;
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
	}
	
	// When adding a new Level to this enum, make sure you also add it to the LevelManager inspector,
	// and add the scene to Build Settings as well
	// ALSO NOTE: Be careful not to fuck up the serialization
	// Next level: 27
	[Serializable]
	public enum Levels {
		ManagerScene = 0,
		TestScene = 1,
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
		PortalTestScene = 17,
		ForkCathedral = 18,
		ForkWhiteRoomBlackHallway = 19,
		ForkWhiteRoom3 = 20,
		TransitionWhiteRoomFork = 21,
		ForkOctagon = 22,
		ForkBlackRoom2 = 23,
		WhiteRoom1BackRoom = 24,
		BehindForkTransition = 25,
		ForkBlackRoom3 = 26,
		ForkCathedralTutorial = 6
	}

	public class LevelManager : SingletonSaveableObject<LevelManager, LevelManager.LevelManagerSave> {
		[OnValueChanged("LoadDefaultPlayerPosition")]
		public Levels startingScene;

		bool initialized = false;

#region PlayerDefaultLocations
		const string EdgeDetectionSettingsKeyPrefix = "edgeDetectionSettings";
		[Serializable]
		class EDSettings {
			BladeEdgeDetection.EdgeColorMode edgeColorMode;
			SerializableColor edgeColor;
			SerializableGradient edgeColorGradient;
			// Can't save textures easily

			public EDSettings(BladeEdgeDetection edgeDetection) {
				this.edgeColorMode = edgeDetection.edgeColorMode;
				this.edgeColor = edgeDetection.edgeColor;

				this.edgeColorGradient = new Gradient {
					alphaKeys = edgeDetection.edgeColorGradient.alphaKeys,
					colorKeys = edgeDetection.edgeColorGradient.colorKeys,
					mode = edgeDetection.edgeColorGradient.mode
				};
			}

			public void ApplyTo(BladeEdgeDetection edgeDetection) {
				edgeDetection.edgeColorMode = this.edgeColorMode;
				edgeDetection.edgeColor = this.edgeColor;
				edgeDetection.edgeColorGradient = this.edgeColorGradient;
			}
		}
		const string PositionKeyPrefix = "playerStartingPositions";
		const string RotationKeyPrefix = "playerStartingRotations";
		public bool defaultPlayerPosition = false;
		bool hasLoadedDefaultPlayerPosition = false;

#if UNITY_EDITOR
		[ShowNativeProperty]
		public Vector3 StartingPositionForScene {
			get {
				string sceneName = GetSceneName();
				string key = $"{PositionKeyPrefix}.{sceneName}";
				return HasVector3(key) ? GetVector3(key) : Vector3.zero;
			}
		}

		[Button("Set default player position")]
		void SetDefaultPlayerPositionForScene() {
			string sceneName = GetSceneName();
			SetVector3($"{PositionKeyPrefix}.{sceneName}", Player.instance.transform.position);
			SetVector3($"{RotationKeyPrefix}.{sceneName}", Player.instance.transform.rotation.eulerAngles);
			Camera mainCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
			BladeEdgeDetection edgeDetection = (mainCam == null) ? null : mainCam.GetComponent<BladeEdgeDetection>();
			if (edgeDetection != null) {
				string edgeDetectionKey = $"{EdgeDetectionSettingsKeyPrefix}.{sceneName}";
				SetEDSettings(edgeDetectionKey, new EDSettings(edgeDetection));
			}

			if (DEBUG) {
				Debug.Log(
					$"Starting position for player set to {Player.instance.transform.position} for scene {sceneName}"
				);
			}
		}

		[Button("Remove default player position for this scene")]
		void UnsetDefaultPlayerPositionForScene() {
			string sceneName = GetSceneName();
			string positionKey = $"{PositionKeyPrefix}.{sceneName}";
			string rotationKey = $"{RotationKeyPrefix}.{sceneName}";
			string edgeDetectionKey = $"{EdgeDetectionSettingsKeyPrefix}.{sceneName}";

			if (HasVector3(positionKey)) {
				RemoveVector3(positionKey);
			}

			if (HasVector3(rotationKey)) {
				RemoveVector3(rotationKey);
			}

			if (PlayerPrefs.HasKey(edgeDetectionKey)) {
				PlayerPrefs.DeleteKey(edgeDetectionKey);
			}
		}
#endif
		void SetEDSettings(string key, EDSettings settings) {
			string serializedSettings = Convert.ToBase64String(settings.SerializeToByteArray());
			PlayerPrefs.SetString(key, serializedSettings);
		}
		
		EDSettings GetEDSettings(string key) {
			if (PlayerPrefs.HasKey(key)) {
				string serializedSettings = PlayerPrefs.GetString(key);
				return Convert.FromBase64String(serializedSettings).Deserialize<EDSettings>();
			}
			else {
				throw new ArgumentException($"No PlayerPrefs key for {key}");
			}
		}

		bool HasVector3(string key) {
			string xKey = $"{key}.x";
			string yKey = $"{key}.y";
			string zKey = $"{key}.z";

			return (PlayerPrefs.HasKey(xKey) && PlayerPrefs.HasKey(yKey) && PlayerPrefs.HasKey(zKey));
		}

		void RemoveVector3(string key) {
			string xKey = $"{key}.x";
			string yKey = $"{key}.y";
			string zKey = $"{key}.z";

			PlayerPrefs.DeleteKey(xKey);
			PlayerPrefs.DeleteKey(yKey);
			PlayerPrefs.DeleteKey(zKey);
		}

		void SetVector3(string key, Vector3 value) {
			PlayerPrefs.SetFloat($"{key}.x", value.x);
			PlayerPrefs.SetFloat($"{key}.y", value.y);
			PlayerPrefs.SetFloat($"{key}.z", value.z);
		}

		Vector3 GetVector3(string key) {
			Vector3 returnVector = Vector3.zero;
			string xKey = $"{key}.x";
			string yKey = $"{key}.y";
			string zKey = $"{key}.z";

			// X
			if (PlayerPrefs.HasKey(xKey)) {
				returnVector.x = PlayerPrefs.GetFloat(xKey);
			}
			else {
				throw new ArgumentException($"No PlayerPrefs key for {key}");
			}

			// Y
			if (PlayerPrefs.HasKey(yKey)) {
				returnVector.y = PlayerPrefs.GetFloat(yKey);
			}
			else {
				throw new ArgumentException($"No PlayerPrefs key for {key}");
			}

			// Z
			if (PlayerPrefs.HasKey(zKey)) {
				returnVector.z = PlayerPrefs.GetFloat(zKey);
			}
			else {
				throw new ArgumentException($"No PlayerPrefs key for {key}");
			}

			return returnVector;
		}

		string GetSceneName() {
			string sceneName = activeSceneName;
			if (!Application.isPlaying) {
				sceneName = startingScene.ToName();
			}

			return sceneName;
		}

		[Button("Load default player position")]
		void LoadDefaultPlayerPosition() {
#if !UNITY_EDITOR
		return;
#endif

		if (!defaultPlayerPosition || hasLoadedDefaultPlayerPosition) return;

		string sceneName = GetSceneName();
		string positionKey = $"{PositionKeyPrefix}.{sceneName}";
		string rotationKey = $"{RotationKeyPrefix}.{sceneName}";
		string edgeDetectionKey = $"{EdgeDetectionSettingsKeyPrefix}.{sceneName}";

		if (HasVector3(positionKey) && HasVector3(rotationKey)) {
			Vector3 pos = GetVector3(positionKey);
			Vector3 eulerRot = GetVector3(rotationKey);

			Player.instance.transform.position = pos;
			Player.instance.transform.rotation = Quaternion.Euler(eulerRot);
		}

		if (PlayerPrefs.HasKey(edgeDetectionKey)) {
			Camera mainCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
			BladeEdgeDetection edgeDetection = (mainCam == null) ? null : mainCam.GetComponent<BladeEdgeDetection>();
			if (edgeDetection != null) {
				GetEDSettings(edgeDetectionKey).ApplyTo(edgeDetection);
			}
		}

		if (DEBUG) {
			if (!HasVector3(positionKey)) {
				Debug.LogError($"No position key found for {positionKey}");
			}

			if (!HasVector3(rotationKey)) {
				Debug.LogError($"No rotation key found for {rotationKey}");
			}
		}

		// Hijacking this to display level banner on load, even when it's already the active scene
		LevelChangeBanner.instance.PlayBanner(enumToSceneName[sceneName]);
#if UNITY_EDITOR
			try {
				Scene scene = EditorSceneManager.GetSceneByName(sceneName);
				if (!EditorSceneManager.GetSceneByName(sceneName).IsValid()) {
					string path = $"Assets/{(sceneName != enumToSceneName[Levels.TestScene] ? "__Scenes" : "PrototypeAndTesting")}/{sceneName}.unity";
					scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
				}
				EditorSceneManager.SetActiveScene(scene);
			}
			catch (Exception e) {
				Debug.LogError(e);
			}

		if (EditorApplication.isPlaying)
#endif
			hasLoadedDefaultPlayerPosition = true;
		}
#endregion
		[SerializeField]
		public List<Level> allLevels;
		// levels is allLevels, keyed by levelName, but with test scenes removed in build
		Dictionary<string, Level> levels;
		internal static TwoWayDictionary<Levels, string> enumToSceneName = new TwoWayDictionary<Levels, string>() {
			{ Levels.ManagerScene, ManagerScene },
			{ Levels.TestScene, "_TestScene" },
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
			{ Levels.ForkWhiteRoom3, "_Fork_WhiteRoom3" },
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
			{ Levels.PortalTestScene, "PortalTestScene" },
			{ Levels.ForkBlackRoom3, "_Fork_BlackRoom3" },
			{ Levels.ForkCathedralTutorial, "_Fork_Cathedral_Tutorial" }
		};
		public string activeSceneName;
		public Levels ActiveScene => activeSceneName.ToLevel();
		public List<string> loadedSceneNames;
		public List<string> currentlyLoadingSceneNames;
		public List<string> currentlyUnloadingSceneNames;
		QueuedSceneSwitch queuedActiveSceneSwitch;
		
		public bool isCurrentlySwitchingScenes;

		[Serializable]
		class QueuedSceneSwitch {
			readonly string levelName;
			readonly bool playBanner;
			readonly bool saveDeactivatedScenesToDisk;
			readonly bool loadActivatedScenesFromDisk;
			readonly bool checkActiveSceneName;

			public QueuedSceneSwitch(
				string levelName,
				bool playBanner = true,
				bool saveDeactivatedScenesToDisk = true,
				bool loadActivatedScenesFromDisk = true,
				bool checkActiveSceneName = true
			) {
				this.levelName = levelName;
				this.playBanner = playBanner;
				this.saveDeactivatedScenesToDisk = saveDeactivatedScenesToDisk;
				this.loadActivatedScenesFromDisk = loadActivatedScenesFromDisk;
				this.checkActiveSceneName = checkActiveSceneName;
			}

			public void Invoke() {
				Debug.LogWarning($"Queued level change happening now for {levelName}");
				LevelManager.instance.SwitchActiveSceneNow(levelName, playBanner, saveDeactivatedScenesToDisk, loadActivatedScenesFromDisk, checkActiveSceneName);
			}
		}

		public bool IsCurrentlyLoadingScenes =>
			currentlyLoadingSceneNames.Count > 0 || currentlyUnloadingSceneNames.Count > 0;

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

		public event ActiveSceneChange OnActiveSceneChange;

		public delegate void ActiveSceneWillChange(string nextSceneName);

		public event ActiveSceneWillChange BeforeActiveSceneChange;

		public delegate void SceneLoadUnload(string sceneName);

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

			loadedSceneNames = new List<string>();
			currentlyLoadingSceneNames = new List<string>();
			currentlyUnloadingSceneNames = new List<string>();

#if UNITY_EDITOR || TEST_BUILD
			levels = allLevels.ToDictionary(l => l.level.ToName(), l => l);
#else
			levels = allLevels.Where(l => l.level.ToName().ToLower().Contains("test")).ToDictionary(l => l.level, l => l);
#endif
			
#if UNITY_EDITOR
			PopulateAlreadyLoadedScenes();
#endif
		}

		protected override void Start() {
			base.Start();
			SceneManager.sceneLoaded += (scene, mode) => FinishLoadingScene(scene);
			SceneManager.sceneLoaded += (scene, mode) => { LoadDefaultPlayerPosition(); };
			SceneManager.sceneUnloaded += FinishUnloadingScene;

			if (!initialized) {
				SwitchActiveScene(startingScene, true, false, false, false);
				initialized = true;
			}
		}

		void Update() {
			if (!initialized) return;

			if (queuedActiveSceneSwitch != null && !IsCurrentlyLoadingScenes) {
				queuedActiveSceneSwitch.Invoke();
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
			bool checkActiveSceneName = true
		) {
			SwitchActiveScene(
				enumToSceneName[level],
				playBanner,
				saveDeactivatedScenesToDisk,
				loadActivatedScenesFromDisk,
				checkActiveSceneName
			);
		}

		/// <summary>
		/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
		/// If we are currently loading already, queue up this scene change instead to be started once the previous one finishes.
		/// </summary>
		/// <param name="levelName">Name of the scene to become active</param>
		/// <param name="playBanner">Whether or not to play the LevelBanner. Defaults to true.</param>
		/// <param name="saveDeactivatedScenesToDisk">Whether or not to save any scenes that deactivated to disk. Defaults to true</param>
		/// <param name="loadActivatedScenesFromDisk">Whether or not to load any scenes from disk that become activated. Defaults to true</param>
		/// <param name="checkActiveSceneName">If true, will skip loading the scene if it's already the active scene. False will force it to load the scene. Defaults to true.</param>
		public void SwitchActiveScene(
			string levelName,
			bool playBanner = true,
			bool saveDeactivatedScenesToDisk = true,
			bool loadActivatedScenesFromDisk = true,
			bool checkActiveSceneName = true
		) {
			if (IsCurrentlyLoadingScenes) {
				queuedActiveSceneSwitch = new QueuedSceneSwitch(levelName, playBanner, saveDeactivatedScenesToDisk, loadActivatedScenesFromDisk, checkActiveSceneName);
			}
			else {
				SwitchActiveSceneNow(levelName, playBanner, saveDeactivatedScenesToDisk, loadActivatedScenesFromDisk, checkActiveSceneName);
			}
		}

		/// <summary>
		/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
		/// </summary>
		/// <param name="levelName">Name of the scene to become active</param>
		/// <param name="playBanner">Whether or not to play the LevelBanner. Defaults to true.</param>
		/// <param name="saveDeactivatedScenesToDisk">Whether or not to save any scenes that deactivated to disk. Defaults to true</param>
		/// <param name="loadActivatedScenesFromDisk">Whether or not to load any scenes from disk that become activated. Defaults to true</param>
		/// <param name="checkActiveSceneName">If true, will skip loading the scene if it's already the active scene. False will force it to load the scene. Defaults to true.</param>
		async void SwitchActiveSceneNow(
			string levelName,
			bool playBanner = true,
			bool saveDeactivatedScenesToDisk = true,
			bool loadActivatedScenesFromDisk = true,
			bool checkActiveSceneName = true
		) {
			if (!levels.ContainsKey(levelName)) {
				debug.LogError("No level name found in world graph with name " + levelName);
				return;
			}

			if (checkActiveSceneName && activeSceneName == levelName) {
				debug.LogWarning("Level " + levelName + " already the active scene.");
				return;
			}

			isCurrentlySwitchingScenes = true;
			// Immediately turn on the loading icon instead of waiting for a frame into the loading
			LoadingIcon.instance.state = LoadingIcon.State.Loading;
			
			Debug.Log($"Switching to level {levelName}");

			BeforeActiveSceneChange?.Invoke(levelName);

			activeSceneName = levelName;

			if (playBanner) {
				LevelChangeBanner.instance.PlayBanner(enumToSceneName[activeSceneName]);
			}

			// First unload any scene no longer needed
			DeactivateUnrelatedScenes(levelName, saveDeactivatedScenesToDisk);

			List<string> scenesToBeLoadedFromDisk = new List<string>();

			// Then load the level if it's not already loaded
			if (!(loadedSceneNames.Contains(levelName) || currentlyLoadingSceneNames.Contains(levelName))) {
				currentlyLoadingSceneNames.Add(levelName);

				BeforeSceneLoad?.Invoke(levelName);

				scenesToBeLoadedFromDisk.Add(levelName);
				if (ShouldLoadScene(levelName)) {
					SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
				}
			}
			else {
				if (!hasLoadedDefaultPlayerPosition) {
					LoadDefaultPlayerPosition();
				}
			}

			// Then load the adjacent scenes if they're not already loaded
			var connectedSceneNames = levels[levelName].connectedLevels.Select(l => enumToSceneName[l]);
			foreach (string connectedSceneName in connectedSceneNames) {
				if (!(loadedSceneNames.Contains(connectedSceneName) ||
				      currentlyLoadingSceneNames.Contains(connectedSceneName))) {
					currentlyLoadingSceneNames.Add(connectedSceneName);

					BeforeSceneLoad?.Invoke(connectedSceneName);

					scenesToBeLoadedFromDisk.Add(connectedSceneName);
					if (ShouldLoadScene(connectedSceneName)) {
						SceneManager.LoadSceneAsync(connectedSceneName, LoadSceneMode.Additive);
					}
				}
			}

			debug.Log("Waiting for scenes to be loaded...");
			await TaskEx.WaitUntil(() => !LevelManager.instance.IsCurrentlyLoadingScenes);
			debug.Log("All scenes loaded into memory" + (loadActivatedScenesFromDisk ? ", loading save..." : "."));

			if (loadActivatedScenesFromDisk && scenesToBeLoadedFromDisk.Count > 0) {
				foreach (string sceneToBeLoaded in scenesToBeLoadedFromDisk) {
					BeforeSceneRestoreDynamicObjects?.Invoke(sceneToBeLoaded);
				}

				foreach (string sceneToBeLoaded in scenesToBeLoadedFromDisk) {
					SaveManagerForScene saveManagerForScene = SaveManager.GetOrCreateSaveManagerForScene(sceneToBeLoaded);
					saveManagerForScene.RestoreDynamicObjectStateForScene();
				}

				foreach (string sceneToBeLoaded in scenesToBeLoadedFromDisk) {
					BeforeSceneRestoreState?.Invoke(sceneToBeLoaded);
				}

				foreach (string sceneToBeLoaded in scenesToBeLoadedFromDisk) {
					SaveManagerForScene saveManagerForScene = SaveManager.GetOrCreateSaveManagerForScene(sceneToBeLoaded);
					saveManagerForScene.RestoreSaveableObjectStateForScene();
				}

				foreach (string sceneToBeLoaded in scenesToBeLoadedFromDisk) {
					AfterSceneRestoreState?.Invoke(sceneToBeLoaded);
				}
			}
			
			isCurrentlySwitchingScenes = false;
		}

		/// <summary>
		/// Unloads any scene that is not the selected scene or connected to it as defined by the world graph.
		/// </summary>
		/// <param name="selectedScene"></param>
		void DeactivateUnrelatedScenes(string selectedScene, bool serializeDeactivatingScenes) {
			List<string> scenesToDeactivate = new List<string>();
			foreach (string currentlyActiveScene in loadedSceneNames) {
				if (currentlyActiveScene != selectedScene &&
				    !levels[selectedScene].connectedLevels.Exists(l => enumToSceneName[l] == currentlyActiveScene)) {
					scenesToDeactivate.Add(currentlyActiveScene);
				}
			}

			if (serializeDeactivatingScenes) {
				foreach (var sceneToDeactivate in scenesToDeactivate) {
					BeforeSceneSerializeState?.Invoke(sceneToDeactivate);
				}
			}

			if (serializeDeactivatingScenes) {
				foreach (string sceneToDeactivate in scenesToDeactivate) {
					SaveManagerForScene saveForScene = SaveManager.GetOrCreateSaveManagerForScene(sceneToDeactivate);
					saveForScene.SerializeStateForScene();
				}
			}

			// Update internal state before starting any unload scene calls
			foreach (var sceneToDeactivate in scenesToDeactivate) {
				loadedSceneNames.Remove(sceneToDeactivate);
				currentlyUnloadingSceneNames.Add(sceneToDeactivate);
			}

			foreach (var sceneToDeactivate in scenesToDeactivate) {
				BeforeSceneUnload?.Invoke(sceneToDeactivate);
			}

			foreach (var sceneToDeactivate in scenesToDeactivate) {
				SceneManager.UnloadSceneAsync(sceneToDeactivate);
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

			if (loadedScene.name == activeSceneName) {
				SceneManager.SetActiveScene(loadedScene);
				OnActiveSceneChange?.Invoke();
			}

			AfterSceneLoad?.Invoke(loadedScene.name);

			if (currentlyLoadingSceneNames.Contains(loadedScene.name)) {
				currentlyLoadingSceneNames.Remove(loadedScene.name);
			}

			if (!loadedSceneNames.Contains(loadedScene.name)) {
				loadedSceneNames.Add(loadedScene.name);
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

			AfterSceneUnload?.Invoke(unloadedScene.name);

			if (currentlyUnloadingSceneNames.Contains(unloadedScene.name)) {
				currentlyUnloadingSceneNames.Remove(unloadedScene.name);
			}
		}

		bool ShouldLoadScene(string sceneToLoad) {
			return sceneToLoad != ManagerScene && !SceneManager.GetSceneByName(sceneToLoad).isLoaded;
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
					loadedSceneNames.Add(alreadyLoadedScene.name);
				}
			}
		}
#endif

#region Saving
		// There's only one LevelManager so we don't need a UniqueId here
		public override string ID => "LevelManager";

		[Serializable]
		public class LevelManagerSave : SerializableSaveObject<LevelManager> {
			bool initialized;
			string activeScene;
			QueuedSceneSwitch queuedActiveSceneSwitch;

			public LevelManagerSave(LevelManager levelManager) : base(levelManager) {
				this.initialized = levelManager.initialized;
				this.activeScene = levelManager.activeSceneName;
				this.queuedActiveSceneSwitch = levelManager.queuedActiveSceneSwitch;
			}

			public override void LoadSave(LevelManager levelManager) {
				levelManager.initialized = this.initialized;
				levelManager.queuedActiveSceneSwitch = this.queuedActiveSceneSwitch;
				levelManager.SwitchActiveScene(activeScene, false, false, false, false);
			}
		}
#endregion
	}
}