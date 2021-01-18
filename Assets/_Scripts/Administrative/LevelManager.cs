#define TEST_BUILD

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EpitaphUtils;
using NaughtyAttributes;
using Saving;
using UnityEngine.Serialization;
using static Saving.SaveManagerForScene;
#if UNITY_EDITOR
using UnityEditor;
#endif

// When adding a new Level to this enum, make sure you also add it under level names region,
// PopulateSceneNames function, PopulateWorldGraph function, and add the scene to Build Settings as well
// ALSO NOTE: You MUST append any new additions to the END of the enum, else it fucks with serialization
[Serializable]
public enum Level {
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
	ForkBlackRoom2
}

public class LevelManager : Singleton<LevelManager>, SaveableObject {
	public bool debugMode = false;
	DebugLogger debug;
	[OnValueChanged("LoadDefaultPlayerPosition")]
	public Level startingScene;
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

		if (debugMode) {
			Debug.Log($"Starting position for player set to {Player.instance.transform.position} for scene {sceneName}");
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
				PopulateSceneNames();
			}
			sceneName = enumToSceneName[startingScene];
		}
		return sceneName;
	}

	[Button("Load default player position")]
	void LoadDefaultPlayerPosition() {
		if (!defaultPlayerPosition || hasLoadedDefaultPlayerPosition
#if !UNITY_EDITOR
			|| true
#endif
		) return;

		string sceneName = GetSceneName();
		string positionKey = $"{PositionKeyPrefix}.{sceneName}";
		string rotationKey = $"{RotationKeyPrefix}.{sceneName}";

		if (HasVector3(positionKey) && HasVector3(rotationKey)) {
			Vector3 pos = GetVector3(positionKey);
			Vector3 eulerRot = GetVector3(rotationKey);

			Player.instance.transform.position = pos;
			Player.instance.transform.rotation = Quaternion.Euler(eulerRot);
		}
		if (debugMode) {
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

	Dictionary<Level, string> enumToSceneName;
	Dictionary<string, Level> sceneNameToEnum;
	Dictionary<string, List<string>> worldGraph;
	public string activeSceneName;
	public Level ActiveScene => GetLevel(activeSceneName);
	public List<string> loadedSceneNames;
	List<string> currentlyLoadingSceneNames;
	List<string> currentlyUnloadingSceneNames;
	public bool IsCurrentlyLoadingScenes => currentlyLoadingSceneNames.Count > 0 || currentlyUnloadingSceneNames.Count > 0;

	public delegate void ActiveSceneChange();
	public event ActiveSceneChange OnActiveSceneChange;
	public delegate void ActiveSceneWillChange(string nextSceneName);
	public event ActiveSceneWillChange BeforeActiveSceneChange;

	public delegate void SceneLoadUnload(string sceneName);
	public event SceneLoadUnload BeforeSceneUnload;
	public event SceneLoadUnload BeforeSceneLoad;
	public event SceneLoadUnload AfterSceneUnload;
	public event SceneLoadUnload AfterSceneLoad;

#region level names
	public const string ManagerScene = "_ManagerScene";
	const string TestScene = "_TestScene";
	const string PortalTestScene = "PortalTestScene";

	// Main Scenes
	const string EmptyRoom = "_EmptyRoom";
	const string HexPillarRoom = "_HexPillarRoom";
	const string Library = "_Library";
	const string Level3 = "_Level3";
	const string Level4 = "_Level4";
	const string Axis = "_Axis";
	const string Fork = "_Fork";
	const string ForkOctagon = "_ForkOctagon";
	const string ForkWhiteRoom = "_Fork_WhiteRoom";
	const string ForkWhiteRoom2 = "_Fork_WhiteRoom2";
	const string ForkWhiteRoomBlackHallway = "_WhiteRoom_BlackHallway";
	const string ForkWhiteRoom3 = "_Fork_WhiteRoom3";
	const string ForkBlackRoom = "_Fork_BlackRoom";
	const string ForkBlackRoom2 = "_Fork_BlackRoom2";
	const string InvisFloor = "_InvisFloor";

	// Transition Scenes
	const string TutorialHallway = "_TutorialHallway";
	const string TutorialRoom = "_TutorialRoom";
	const string Transition23 = "_Transition2_3";
	const string Transition34 = "_Transition3_4";
	const string TransitionWhiteRoomFork = "_TransitionWhiteRoom_Fork";

	const string MetaEdgeDetection = "_Meta_EdgeDetection";

#endregion

	public void Awake() {
		hasLoadedDefaultPlayerPosition = false;
        debug = new DebugLogger(this, () => debugMode);

		loadedSceneNames = new List<string>();
		currentlyLoadingSceneNames = new List<string>();
		currentlyUnloadingSceneNames = new List<string>();

		PopulateSceneNames();
		worldGraph = new Dictionary<string, List<string>>();
		PopulateWorldGraph();

#if UNITY_EDITOR
		PopulateAlreadyLoadedScenes();
#endif

		activeSceneName = enumToSceneName[startingScene];
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

	/// <summary>
	/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
	/// </summary>
	/// <param name="level">Enum value of the scene to become active</param>
	/// <param name="playBanner">Whether or not to play the LevelBanner. Defaults to true.</param>
	/// <param name="saveDeactivatedScenesToDisk">Whether or not to save any scenes that deactivated to disk. Defaults to true</param>
	/// <param name="loadActivatedScenesFromDisk">Whether or not to load any scenes from disk that become activated. Defaults to true</param>
	/// <param name="checkActiveSceneName">If true, will skip loading the scene if it's already the active scene. False will force it to load the scene. Defaults to true.</param>
	public void SwitchActiveScene(Level level, bool playBanner = true, bool saveDeactivatedScenesToDisk = true, bool loadActivatedScenesFromDisk = true, bool checkActiveSceneName = true) {
		SwitchActiveScene(enumToSceneName[level], playBanner, saveDeactivatedScenesToDisk, loadActivatedScenesFromDisk, checkActiveSceneName);
	}

	/// <summary>
	/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
	/// </summary>
	/// <param name="levelName">Name of the scene to become active</param>
	/// <param name="playBanner">Whether or not to play the LevelBanner. Defaults to true.</param>
	/// <param name="saveDeactivatedScenesToDisk">Whether or not to save any scenes that deactivated to disk. Defaults to true</param>
	/// <param name="loadActivatedScenesFromDisk">Whether or not to load any scenes from disk that become activated. Defaults to true</param>
	/// <param name="checkActiveSceneName">If true, will skip loading the scene if it's already the active scene. False will force it to load the scene. Defaults to true.</param>
	public async void SwitchActiveScene(string levelName, bool playBanner = true, bool saveDeactivatedScenesToDisk = true, bool loadActivatedScenesFromDisk = true, bool checkActiveSceneName = true) {
		if (!worldGraph.ContainsKey(levelName)) {
			debug.LogError("No level name found in world graph with name " + levelName);
			return;
		}

		if (checkActiveSceneName && activeSceneName == levelName) {
			debug.LogWarning("Level " + levelName + " already the active scene.");
			return;
		}

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
			SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
		}
		else {
			if (!hasLoadedDefaultPlayerPosition) {
				LoadDefaultPlayerPosition();
			}
		}

		// Then load the adjacent scenes if they're not already loaded
		foreach (string connectedSceneName in worldGraph[levelName]) {
			if (!(loadedSceneNames.Contains(connectedSceneName) || currentlyLoadingSceneNames.Contains(connectedSceneName))) {
				currentlyLoadingSceneNames.Add(connectedSceneName);

				BeforeSceneLoad?.Invoke(connectedSceneName);

				scenesToBeLoadedFromDisk.Add(connectedSceneName);
				SceneManager.LoadSceneAsync(connectedSceneName, LoadSceneMode.Additive);
			}
		}

		debug.Log("Waiting for scenes to be loaded...");
		await TaskEx.WaitUntil(() => !LevelManager.instance.IsCurrentlyLoadingScenes);
		debug.Log("All scenes loaded into memory" + (loadActivatedScenesFromDisk ? ", loading save..." : "."));

		if (loadActivatedScenesFromDisk && scenesToBeLoadedFromDisk.Count > 0) {
			foreach (string sceneToBeLoaded in scenesToBeLoadedFromDisk) {
				DynamicObjectManager.LoadDynamicObjectsForScene(sceneToBeLoaded);
			}

			foreach (string loadedScene in loadedSceneNames) {
				SaveManager.GetSaveManagerForScene(loadedScene).InitializeSaveableObjectsDict();
			}

			foreach (string sceneToBeLoaded in scenesToBeLoadedFromDisk) {
				SaveManagerForScene saveManagerForScene = SaveManager.GetSaveManagerForScene(sceneToBeLoaded);
				SaveFileForScene saveFileForScene = saveManagerForScene.GetSaveFromDisk(SaveManager.temp);
				saveManagerForScene?.LoadSceneFromSaveFile(saveFileForScene);
			}
		}
	}

	public string GetSceneName(Level level) {
		return enumToSceneName[level];
	}

	public Level GetLevel(string sceneName) {
		return sceneNameToEnum[sceneName];
	}

	void PopulateSceneNames() {
		enumToSceneName = new Dictionary<Level, string>();
		sceneNameToEnum = new Dictionary<string, Level>();

#if UNITY_EDITOR || TEST_BUILD
		enumToSceneName.Add(Level.TestScene, TestScene);
#endif
		enumToSceneName.Add(Level.ManagerScene, ManagerScene);
		enumToSceneName.Add(Level.EmptyRoom, EmptyRoom);
		enumToSceneName.Add(Level.HexPillarRoom, HexPillarRoom);
		enumToSceneName.Add(Level.Library, Library);
		enumToSceneName.Add(Level.Level3, Level3);
		enumToSceneName.Add(Level.Level4, Level4);
		enumToSceneName.Add(Level.Axis, Axis);
		enumToSceneName.Add(Level.Fork, Fork);
		enumToSceneName.Add(Level.ForkWhiteRoom, ForkWhiteRoom);
		enumToSceneName.Add(Level.ForkWhiteRoom2, ForkWhiteRoom2);
		enumToSceneName.Add(Level.ForkWhiteRoomBlackHallway, ForkWhiteRoomBlackHallway);
		enumToSceneName.Add(Level.ForkWhiteRoom3, ForkWhiteRoom3);
		enumToSceneName.Add(Level.ForkBlackRoom, ForkBlackRoom);
		enumToSceneName.Add(Level.TutorialHallway, TutorialHallway);
		enumToSceneName.Add(Level.TutorialRoom, TutorialRoom);
		enumToSceneName.Add(Level.Transition23, Transition23);
		enumToSceneName.Add(Level.Transition34, Transition34);
        enumToSceneName.Add(Level.InvisFloor, InvisFloor);
		enumToSceneName.Add(Level.MetaEdgeDetection, MetaEdgeDetection);
		enumToSceneName.Add(Level.PortalTestScene, PortalTestScene);
		enumToSceneName.Add(Level.TransitionWhiteRoomFork, TransitionWhiteRoomFork);
		enumToSceneName.Add(Level.ForkOctagon, ForkOctagon);
		enumToSceneName.Add(Level.ForkBlackRoom2, ForkBlackRoom2);

		foreach (var kv in enumToSceneName) {
			sceneNameToEnum[kv.Value] = kv.Key;
		}
	}

	/// <summary>
	/// Defines the world graph which determines which scenes are adjacent to one another.
	/// </summary>
	void PopulateWorldGraph() {
#if UNITY_EDITOR || TEST_BUILD
		worldGraph.Add(TestScene, new List<string>());
#endif

		worldGraph.Add(EmptyRoom, new List<string>() { TutorialHallway });
		worldGraph.Add(HexPillarRoom, new List<string>() { TutorialHallway, Library });
		worldGraph.Add(Library, new List<string>() { HexPillarRoom, TutorialHallway });
		worldGraph.Add(Level3, new List<string>() { Transition23, Transition34 });
		worldGraph.Add(Level4, new List<string>() { Transition34 });
		worldGraph.Add(Axis, new List<string>() { TutorialHallway, TutorialRoom });
		worldGraph.Add(Fork, new List<string>() { TransitionWhiteRoomFork, ForkWhiteRoom, ForkBlackRoom, ForkOctagon });
		worldGraph.Add(ForkOctagon, new List<string>() { TransitionWhiteRoomFork, Fork });
		worldGraph.Add(ForkWhiteRoom, new List<string>() { Fork, MetaEdgeDetection, TransitionWhiteRoomFork });
		worldGraph.Add(ForkWhiteRoom2, new List<string>() { ForkWhiteRoom3, ForkWhiteRoomBlackHallway });
		worldGraph.Add(ForkWhiteRoomBlackHallway, new List<string>() { ForkWhiteRoom2 });
		worldGraph.Add(ForkWhiteRoom3, new List<string>() { ForkWhiteRoom2 });
		worldGraph.Add(ForkBlackRoom, new List<string>() { Fork });
		worldGraph.Add(ForkBlackRoom2, new List<string>() { });
        worldGraph.Add(InvisFloor, new List<string>());

		worldGraph.Add(TutorialHallway, new List<string>() { EmptyRoom, TutorialRoom });
		worldGraph.Add(TutorialRoom, new List<string>() { TutorialHallway, Axis });
		worldGraph.Add(Transition23, new List<string>() { HexPillarRoom, Level3 });
		worldGraph.Add(Transition34, new List<string>() { Level3, Level4 });
		worldGraph.Add(TransitionWhiteRoomFork, new List<string>() { ForkWhiteRoom, ForkWhiteRoom3, ForkOctagon });

		worldGraph.Add(MetaEdgeDetection, new List<string>() { ForkWhiteRoom });
		worldGraph.Add(PortalTestScene, new List<string>() { });
	}

	/// <summary>
	/// Unloads any scene that is not the selected scene or connected to it as defined by the world graph.
	/// </summary>
	/// <param name="selectedScene"></param>
	void DeactivateUnrelatedScenes(string selectedScene, bool saveDeactivatingScenesToDisk) {
		List<string> scenesToDeactivate = new List<string>();
		foreach (string currentlyActiveScene in loadedSceneNames) {
			if (currentlyActiveScene != selectedScene && !worldGraph[selectedScene].Contains(currentlyActiveScene)) {
				scenesToDeactivate.Add(currentlyActiveScene);
			}
		}

		foreach (string sceneToDeactivate in scenesToDeactivate) {
			BeforeSceneUnload?.Invoke(sceneToDeactivate);

			if (saveDeactivatingScenesToDisk) {
				DynamicObjectManager.SaveDynamicObjectsForScene(sceneToDeactivate);
				SaveManagerForScene saveForScene = SaveManager.GetSaveManagerForScene(sceneToDeactivate);
				saveForScene?.SaveScene(SaveManager.temp);
			}

			SceneManager.UnloadSceneAsync(sceneToDeactivate);
			loadedSceneNames.Remove(sceneToDeactivate);
			currentlyUnloadingSceneNames.Add(sceneToDeactivate);
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
	public bool SkipSave { get; set; }
	// There's only one LevelManager so we don't need a UniqueId here
	public string ID => "LevelManager";

	[Serializable]
	class LevelManagerSave {
		bool initialized;
		string activeScene;

		public LevelManagerSave(LevelManager levelManager) {
			this.initialized = levelManager.initialized;
			this.activeScene = levelManager.activeSceneName;
		}

		public void LoadSave(LevelManager levelManager) {
			levelManager.initialized = this.initialized;
			levelManager.SwitchActiveScene(activeScene, false, false, false, false);
		}
	}

	public object GetSaveObject() {
		return new LevelManagerSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		LevelManagerSave save = savedObject as LevelManagerSave;

		save?.LoadSave(this);
	}
#endregion
}
