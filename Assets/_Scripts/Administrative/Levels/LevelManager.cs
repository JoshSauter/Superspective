// Remove this when making release versions to not include the test scene in the build
#define TEST_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using EpitaphUtils;
using NaughtyAttributes;
using Saving;
using static Saving.SaveManagerForScene;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelManagement {
	// When adding a new Level to this enum, make sure you also add it to the LevelManager inspector,
	// and add the scene to Build Settings as well
	// ALSO NOTE: You MUST append any new additions to the END of the enum, else it fucks with serialization
	// TODO: I can fix ^ by explicitly assigning values if I need to
	[Serializable]
	public enum Levels {
		ManagerScene,
		TestScene,
		EmptyRoom,
		HexPillarRoom,
		Library,
		Level3,
		Level4,
		TutorialHallway,
		TutorialRoom,
		Transition23,
		Transition34,
		Axis,
		Fork,
		ForkWhiteRoom,
		ForkBlackRoom,
		InvisFloor,
		MetaEdgeDetection,
		PortalTestScene,
		ForkWhiteRoom2,
		ForkWhiteRoomBlackHallway,
		ForkWhiteRoom3,
		TransitionWhiteRoomFork,
		ForkOctagon,
		ForkBlackRoom2,
		WhiteRoom1BackRoom,
		BehindForkTransition
	}

	public class LevelManager : SingletonSaveableObject<LevelManager, LevelManager.LevelManagerSave> {
		[OnValueChanged("LoadDefaultPlayerPosition")]
		public Levels startingScene;

		bool initialized = false;

#region PlayerDefaultLocations
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

			if (HasVector3(positionKey)) {
				RemoveVector3(positionKey);
			}

			if (HasVector3(rotationKey)) {
				RemoveVector3(rotationKey);
			}
		}
#endif

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
				if (enumToSceneName == null) {
					PopulateScenes();
				}

				sceneName = enumToSceneName[startingScene];
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

		if (HasVector3(positionKey) && HasVector3(rotationKey)) {
			Vector3 pos = GetVector3(positionKey);
			Vector3 eulerRot = GetVector3(rotationKey);

			Player.instance.transform.position = pos;
			Player.instance.transform.rotation = Quaternion.Euler(eulerRot);
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
		LevelChangeBanner.instance.PlayBanner(sceneNameToEnum[sceneName]);
#if UNITY_EDITOR
		if (EditorApplication.isPlaying)
#endif
			hasLoadedDefaultPlayerPosition = true;
		}
#endregion
		[SerializeField]
		List<Level> allLevels;
		// levels is allLevels, keyed by levelName, but with test scenes removed in build
		Dictionary<string, Level> levels;
		Dictionary<Levels, string> enumToSceneName;
		public Dictionary<string, Levels> sceneNameToEnum;
		public string activeSceneName;
		public Levels ActiveScene => GetLevel(activeSceneName);
		public List<string> loadedSceneNames;
		public List<string> currentlyLoadingSceneNames;
		public List<string> currentlyUnloadingSceneNames;
		QueuedSceneSwitch queuedActiveSceneSwitch;
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

		public const string ManagerScene = "_ManagerScene";

		protected override void Awake() {
			base.Awake();
			hasLoadedDefaultPlayerPosition = false;

			loadedSceneNames = new List<string>();
			currentlyLoadingSceneNames = new List<string>();
			currentlyUnloadingSceneNames = new List<string>();

			PopulateScenes();

#if UNITY_EDITOR
			PopulateAlreadyLoadedScenes();
#endif
		}

		void Start() {
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
			
			Debug.Log($"Switching to level {levelName}");

			BeforeActiveSceneChange?.Invoke(levelName);

			activeSceneName = levelName;

			if (playBanner) {
				LevelChangeBanner.instance.PlayBanner(sceneNameToEnum[activeSceneName]);
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
		}

		public string GetSceneName(Levels level) {
			return enumToSceneName[level];
		}

		public Levels GetLevel(string sceneName) {
			return sceneNameToEnum[sceneName];
		}

		void PopulateScenes() {
#if UNITY_EDITOR || TEST_BUILD
			levels = allLevels.ToDictionary(l => l.levelName, l => l);
#else
			levels = allLevels.Where(l => l.levelName.ToLower().Contains("test")).ToDictionary(l => l.level, l => l);
#endif
			enumToSceneName = new Dictionary<Levels, string>();
			sceneNameToEnum = new Dictionary<string, Levels>();

			foreach (var level in levels) {
				sceneNameToEnum[level.Key] = level.Value.level;
			}

			foreach (var kv in sceneNameToEnum) {
				enumToSceneName[kv.Value] = kv.Key;
			}
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