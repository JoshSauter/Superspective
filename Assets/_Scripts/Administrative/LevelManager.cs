#define TEST_BUILD

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EpitaphUtils;
using NaughtyAttributes;
using Saving;
using System.Collections;
using static Saving.SaveManagerForScene;
#if UNITY_EDITOR
using UnityEditor;
#endif

// When adding a new Level to this enum, make sure you also add it under level names region,
// PopulateSceneNames function, PopulateWorldGraph function, and add the scene to Build Settings as well
// ALSO NOTE: You MUST append any new additions to the END of the enum, else it fucks with serializataion
[Serializable]
public enum Level {
	managerScene,
	testScene,
	emptyRoom,
	hexPillarRoom,
	library,
	level3,
	level4,
	tutorialHallway,
	tutorialRoom,
	transition2_3,
	transition3_4,
	axis,
	fork,
	forkWhiteRoom,
	forkBlackRoom,
    invisFloor,
	metaEdgeDetection,
	portalTestScene,
	forkWhiteRoom2,
	forkWhiteRoomBlackHallway,
	forkWhiteRoom3,
	transitionWhiteRoom_Fork,
	forkOctagon,
	forkBlackRoom2
}

public class LevelManager : Singleton<LevelManager>, SaveableObject {
	public bool DEBUG = false;
	public DebugLogger debug;
	[OnValueChanged("LoadDefaultPlayerPosition")]
	public Level startingScene;

#region PlayerDefaultLocations
	private const string positionKeyPrefix = "playerStartingPositions";
	private const string rotationKeyPrefix = "playerStartingRotations";
	public bool defaultPlayerPosition = false;
	private bool hasLoadedDefaultPlayerPosition = false;

#if UNITY_EDITOR
	[ShowNativeProperty]
	public Vector3 startingPositionForScene {
		get {
			string sceneName = GetSceneName();
			string key = $"{positionKeyPrefix}.{sceneName}";
			if (HasVector3(key)) {
				return GetVector3(key);
			}
			else {
				return Vector3.zero;
			}
		}
	}

	[Button("Set default player position")]
	private void SetDefaultPlayerPositionForScene() {
		string sceneName = GetSceneName();
		SetVector3($"{positionKeyPrefix}.{sceneName}", Player.instance.transform.position);
		SetVector3($"{rotationKeyPrefix}.{sceneName}", Player.instance.transform.rotation.eulerAngles);

		if (DEBUG) {
			Debug.Log($"Starting position for player set to {Player.instance.transform.position} for scene {sceneName}");
		}
	}

	[Button("Remove default player position for this scene")]
	private void UnsetDefaultPlayerPositionForScene() {
		string sceneName = GetSceneName();
		string positionKey = $"{positionKeyPrefix}.{sceneName}";
		string rotationKey = $"{rotationKeyPrefix}.{sceneName}";

		if (HasVector3(positionKey)) {
			RemoveVector3(positionKey);
		}
		if (HasVector3(rotationKey)) {
			RemoveVector3(rotationKey);
		}
	}
#endif

	private bool HasVector3(string key) {
		string xKey = $"{key}.x";
		string yKey = $"{key}.y";
		string zKey = $"{key}.z";

		return (PlayerPrefs.HasKey(xKey) && PlayerPrefs.HasKey(yKey) && PlayerPrefs.HasKey(zKey));
	}

	private void RemoveVector3(string key) {
		string xKey = $"{key}.x";
		string yKey = $"{key}.y";
		string zKey = $"{key}.z";

		PlayerPrefs.DeleteKey(xKey);
		PlayerPrefs.DeleteKey(yKey);
		PlayerPrefs.DeleteKey(zKey);
	}

	private void SetVector3(string key, Vector3 value) {
		PlayerPrefs.SetFloat($"{key}.x", value.x);
		PlayerPrefs.SetFloat($"{key}.y", value.y);
		PlayerPrefs.SetFloat($"{key}.z", value.z);
	}

	private Vector3 GetVector3(string key) {
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

	private string GetSceneName() {
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
	private void LoadDefaultPlayerPosition() {
		if (!defaultPlayerPosition || hasLoadedDefaultPlayerPosition) return;

		string sceneName = GetSceneName();
		string positionKey = $"{positionKeyPrefix}.{sceneName}";
		string rotationKey = $"{rotationKeyPrefix}.{sceneName}";

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

	Dictionary<Level, string> enumToSceneName;
	Dictionary<string, Level> sceneNameToEnum;
	Dictionary<string, List<string>> worldGraph;
	public string activeSceneName;
	public Level activeScene => GetLevel(activeSceneName);
	public List<string> loadedSceneNames;
	List<string> currentlyLoadingSceneNames;
	List<string> currentlyUnloadingSceneNames;
	public bool isCurrentlyLoadingScenes => currentlyLoadingSceneNames.Count > 0 || currentlyUnloadingSceneNames.Count > 0;

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
	public const string managerScene = "_ManagerScene";
	private const string testScene = "_TestScene";
	private const string portalTestScene = "PortalTestScene";

	// Main Scenes
	private const string emptyRoom = "_EmptyRoom";
	private const string hexPillarRoom = "_HexPillarRoom";
	private const string library = "_Library";
	private const string level3 = "_Level3";
	private const string level4 = "_Level4";
	private const string axis = "_Axis";
	private const string fork = "_Fork";
	private const string forkOctagon = "_ForkOctagon";
	private const string forkWhiteRoom = "_Fork_WhiteRoom";
	private const string forkWhiteRoom2 = "_Fork_WhiteRoom2";
	private const string forkWhiteRoomBlackHallway = "_WhiteRoom_BlackHallway";
	private const string forkWhiteRoom3 = "_Fork_WhiteRoom3";
	private const string forkBlackRoom = "_Fork_BlackRoom";
	private const string forkBlackRoom2 = "_Fork_BlackRoom2";
    private const string invisFloor = "_InvisFloor";

	// Transition Scenes
	private const string tutorialHallway = "_TutorialHallway";
	private const string tutorialRoom = "_TutorialRoom";
	private const string transition2_3 = "_Transition2_3";
	private const string transition3_4 = "_Transition3_4";
	private const string transitionWhiteRoom_Fork = "_TransitionWhiteRoom_Fork";

	private const string metaEdgeDetection = "_Meta_EdgeDetection";

#endregion

	public void Start() {
		hasLoadedDefaultPlayerPosition = false;
        debug = new DebugLogger(this, () => DEBUG);

		loadedSceneNames = new List<string>();
		currentlyLoadingSceneNames = new List<string>();
		currentlyUnloadingSceneNames = new List<string>();

		PopulateSceneNames();
		worldGraph = new Dictionary<string, List<string>>();
		PopulateWorldGraph();

#if UNITY_EDITOR
		PopulateAlreadyLoadedScenes();
#endif

		SceneManager.sceneLoaded += (scene, mode) => FinishLoadingScene(scene);
		SceneManager.sceneLoaded += (scene, mode) => { LoadDefaultPlayerPosition(); };
		SceneManager.sceneUnloaded += FinishUnloadingScene;

		SwitchActiveScene(startingScene);
	}

	/// <summary>
	/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
	/// </summary>
	/// <param name="level">Enum value of the scene to become active</param>
	public void SwitchActiveScene(Level level) {
		SwitchActiveScene(enumToSceneName[level]);
	}

	/// <summary>
	/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
	/// </summary>
	/// <param name="levelName">Name of the scene to become active</param>
	public async void SwitchActiveScene(string levelName, bool playBanner = true, bool saveDeactivatedScenesToDisk = true, bool loadActivatedScenesFromDisk = true) {
		if (!worldGraph.ContainsKey(levelName)) {
			debug.LogError("No level name found in world graph with name " + levelName);
			return;
		}

		if (activeSceneName == levelName) {
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

		Debug.Log("Waiting for scenes to be loaded...");
		await TaskEx.WaitUntil(() => !LevelManager.instance.isCurrentlyLoadingScenes);
		Debug.Log("All scenes loaded into memory" + (loadActivatedScenesFromDisk ? ", loading save..." : "."));

		if (loadActivatedScenesFromDisk && scenesToBeLoadedFromDisk.Count > 0) {
			DynamicObjectManager.LoadOrCreateDynamicObjects(SaveManager.temp);

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

	private void PopulateSceneNames() {
		enumToSceneName = new Dictionary<Level, string>();
		sceneNameToEnum = new Dictionary<string, Level>();

#if UNITY_EDITOR || TEST_BUILD
		enumToSceneName.Add(Level.testScene, testScene);
#endif
		enumToSceneName.Add(Level.managerScene, managerScene);
		enumToSceneName.Add(Level.emptyRoom, emptyRoom);
		enumToSceneName.Add(Level.hexPillarRoom, hexPillarRoom);
		enumToSceneName.Add(Level.library, library);
		enumToSceneName.Add(Level.level3, level3);
		enumToSceneName.Add(Level.level4, level4);
		enumToSceneName.Add(Level.axis, axis);
		enumToSceneName.Add(Level.fork, fork);
		enumToSceneName.Add(Level.forkWhiteRoom, forkWhiteRoom);
		enumToSceneName.Add(Level.forkWhiteRoom2, forkWhiteRoom2);
		enumToSceneName.Add(Level.forkWhiteRoomBlackHallway, forkWhiteRoomBlackHallway);
		enumToSceneName.Add(Level.forkWhiteRoom3, forkWhiteRoom3);
		enumToSceneName.Add(Level.forkBlackRoom, forkBlackRoom);
		enumToSceneName.Add(Level.tutorialHallway, tutorialHallway);
		enumToSceneName.Add(Level.tutorialRoom, tutorialRoom);
		enumToSceneName.Add(Level.transition2_3, transition2_3);
		enumToSceneName.Add(Level.transition3_4, transition3_4);
        enumToSceneName.Add(Level.invisFloor, invisFloor);
		enumToSceneName.Add(Level.metaEdgeDetection, metaEdgeDetection);
		enumToSceneName.Add(Level.portalTestScene, portalTestScene);
		enumToSceneName.Add(Level.transitionWhiteRoom_Fork, transitionWhiteRoom_Fork);
		enumToSceneName.Add(Level.forkOctagon, forkOctagon);
		enumToSceneName.Add(Level.forkBlackRoom2, forkBlackRoom2);

		foreach (var kv in enumToSceneName) {
			sceneNameToEnum[kv.Value] = kv.Key;
		}
	}

	/// <summary>
	/// Defines the world graph which determines which scenes are adjacent to one another.
	/// </summary>
	private void PopulateWorldGraph() {
#if UNITY_EDITOR || TEST_BUILD
		worldGraph.Add(testScene, new List<string>());
#endif

		worldGraph.Add(emptyRoom, new List<string>() { tutorialHallway });
		worldGraph.Add(hexPillarRoom, new List<string>() { tutorialHallway, library });
		worldGraph.Add(library, new List<string>() { hexPillarRoom, tutorialHallway });
		worldGraph.Add(level3, new List<string>() { transition2_3, transition3_4 });
		worldGraph.Add(level4, new List<string>() { transition3_4 });
		worldGraph.Add(axis, new List<string>() { tutorialHallway, tutorialRoom });
		worldGraph.Add(fork, new List<string>() { transitionWhiteRoom_Fork, forkWhiteRoom, forkBlackRoom, forkOctagon });
		worldGraph.Add(forkOctagon, new List<string>() { transitionWhiteRoom_Fork, fork });
		worldGraph.Add(forkWhiteRoom, new List<string>() { fork, metaEdgeDetection, forkWhiteRoom2 });
		worldGraph.Add(forkWhiteRoom2, new List<string>() { forkWhiteRoom, forkWhiteRoomBlackHallway });
		worldGraph.Add(forkWhiteRoomBlackHallway, new List<string>() { forkWhiteRoom2, transitionWhiteRoom_Fork });
		worldGraph.Add(forkWhiteRoom3, new List<string>() { transitionWhiteRoom_Fork });
		worldGraph.Add(forkBlackRoom, new List<string>() { fork });
		worldGraph.Add(forkBlackRoom2, new List<string>() { });
        worldGraph.Add(invisFloor, new List<string>());

		worldGraph.Add(tutorialHallway, new List<string>() { emptyRoom, tutorialRoom });
		worldGraph.Add(tutorialRoom, new List<string>() { tutorialHallway, axis });
		worldGraph.Add(transition2_3, new List<string>() { hexPillarRoom, level3 });
		worldGraph.Add(transition3_4, new List<string>() { level3, level4 });
		worldGraph.Add(transitionWhiteRoom_Fork, new List<string>() { forkWhiteRoomBlackHallway, forkWhiteRoom3, forkOctagon });

		worldGraph.Add(metaEdgeDetection, new List<string>() { forkWhiteRoom });
		worldGraph.Add(portalTestScene, new List<string>() { });
	}

	/// <summary>
	/// Unloads any scene that is not the selected scene or connected to it as defined by the world graph.
	/// </summary>
	/// <param name="selectedScene"></param>
	private void DeactivateUnrelatedScenes(string selectedScene, bool saveDeactivatingScenesToDisk) {
		List<string> scenesToDeactivate = new List<string>();
		foreach (string currentlyActiveScene in loadedSceneNames) {
			if (currentlyActiveScene != selectedScene && !worldGraph[selectedScene].Contains(currentlyActiveScene)) {
				scenesToDeactivate.Add(currentlyActiveScene);
			}
		}

		if (saveDeactivatingScenesToDisk && scenesToDeactivate.Count > 0) {
			DynamicObjectManager.SaveDynamicObjects(SaveManager.temp);
		}
		foreach (string sceneToDeactivate in scenesToDeactivate) {
			BeforeSceneUnload?.Invoke(sceneToDeactivate);

			if (saveDeactivatingScenesToDisk) {
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
	private void FinishLoadingScene(Scene loadedScene) {
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
	private void FinishUnloadingScene(Scene unloadedScene) {
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
	private void PopulateAlreadyLoadedScenes() {
		foreach (var scene in EditorBuildSettings.scenes) {
			Scene alreadyLoadedScene = SceneManager.GetSceneByPath(scene.path);
			if (alreadyLoadedScene.IsValid() && alreadyLoadedScene.name != managerScene) {
				loadedSceneNames.Add(alreadyLoadedScene.name);
			}
		}
	}
#endif

	#region Saving
	// There's only one LevelManager so we don't need a UniqueId here
	public string ID => "LevelManager";

	[Serializable]
	class LevelManagerSave {
		string activeScene;

		public LevelManagerSave(LevelManager levelManager) {
			this.activeScene = levelManager.activeSceneName;
		}

		public void LoadSave(LevelManager levelManager) {
			levelManager.SwitchActiveScene(activeScene, false, false, false);
		}
	}

	public object GetSaveObject() {
		return new LevelManagerSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		LevelManagerSave save = savedObject as LevelManagerSave;

		save.LoadSave(this);
	}
	#endregion
}
