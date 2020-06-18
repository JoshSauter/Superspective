#define TEST_BUILD

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EpitaphUtils;
using Boo.Lang.Runtime;
#if UNITY_EDITOR
using UnityEditor;
using NaughtyAttributes;
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
	forkWhiteRoom2
}

public class LevelManager : Singleton<LevelManager> {
	public bool DEBUG = false;
	public DebugLogger debug;
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
			throw new RuntimeException($"No PlayerPrefs key for {key}");
		}

		// Y
		if (PlayerPrefs.HasKey(yKey)) {
			returnVector.y = PlayerPrefs.GetFloat(yKey);
		}
		else {
			throw new RuntimeException($"No PlayerPrefs key for {key}");
		}

		// Z
		if (PlayerPrefs.HasKey(zKey)) {
			returnVector.z = PlayerPrefs.GetFloat(zKey);
		}
		else {
			throw new RuntimeException($"No PlayerPrefs key for {key}");
		}

		return returnVector;
	}

	private string GetSceneName() {
		string sceneName = activeSceneName;
		if (!Application.isPlaying) {
			if (enumToSceneName == null) {
				enumToSceneName = new Dictionary<Level, string>();
				PopulateSceneNames();
			}
			sceneName = enumToSceneName[startingScene];
		}
		return sceneName;
	}

	[Button("Load default player position")]
	private void LoadDefaultPlayerPosition() {
		string sceneName = GetSceneName();
		string positionKey = $"{positionKeyPrefix}.{sceneName}";
		string rotationKey = $"{rotationKeyPrefix}.{sceneName}";

		if (HasVector3(positionKey) && HasVector3(rotationKey)) {
			Vector3 pos = GetVector3(positionKey);
			Vector3 eulerRot = GetVector3(rotationKey);

			Player.instance.transform.position = pos;
			Player.instance.transform.rotation = Quaternion.Euler(eulerRot);
		}

		hasLoadedDefaultPlayerPosition = true;
	}
#endregion

	Dictionary<Level, string> enumToSceneName;
	Dictionary<string, List<string>> worldGraph;
	public string activeSceneName;
	List<string> loadedSceneNames;
	List<string> currentlyLoadingSceneNames;
	List<string> currentlyUnloadingSceneNames;

#region level names
	private const string managerScene = "_ManagerScene";
	private const string testScene = "_TestScene";

	private const string emptyRoom = "_EmptyRoom";
	private const string hexPillarRoom = "_HexPillarRoom";
	private const string library = "_Library";
	private const string level3 = "_Level3";
	private const string level4 = "_Level4";
	private const string axis = "_Axis";
	private const string fork = "_Fork";
	private const string forkWhiteRoom = "_Fork_WhiteRoom";
	private const string forkWhiteRoom2 = "_Fork_WhiteRoom2";
	private const string forkBlackRoom = "_Fork_BlackRoom";
    private const string invisFloor = "_InvisFloor";

	private const string tutorialHallway = "_TutorialHallway";
	private const string tutorialRoom = "_TutorialRoom";
	private const string transition2_3 = "_Transition2_3";
	private const string transition3_4 = "_Transition3_4";

	private const string metaEdgeDetection = "_Meta_EdgeDetection";
	private const string portalTestScene = "PortalTestScene";

#endregion

	public void Start() {
        debug = new DebugLogger(this, () => DEBUG);

		loadedSceneNames = new List<string>();
		currentlyLoadingSceneNames = new List<string>();
		currentlyUnloadingSceneNames = new List<string>();

		enumToSceneName = new Dictionary<Level, string>();
		PopulateSceneNames();
		worldGraph = new Dictionary<string, List<string>>();
		PopulateWorldGraph();

#if UNITY_EDITOR
		PopulateAlreadyLoadedScenes();
#endif

		SceneManager.sceneLoaded += (scene, mode) => FinishLoadingScene(scene);
		SceneManager.sceneLoaded += (scene, mode) => { if (defaultPlayerPosition && !hasLoadedDefaultPlayerPosition) LoadDefaultPlayerPosition(); };
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
	public void SwitchActiveScene(string levelName) {
		if (!worldGraph.ContainsKey(levelName)) {
			debug.LogError("No level name found in world graph with name " + levelName);
			return;
		}

		if (activeSceneName == levelName) {
			debug.LogWarning("Level " + levelName + " already the active scene.");
			return;
		}

		activeSceneName = levelName;

		// First unload any scene no longer needed
		DeactivateUnrelatedScenes(levelName);

		// Then load the level if it's not already loaded
		if (!(loadedSceneNames.Contains(levelName) || currentlyLoadingSceneNames.Contains(levelName))) {
			currentlyLoadingSceneNames.Add(levelName);
			SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
		}

		// Then load the adjacent scenes if they're not already loaded
		foreach (string connectedSceneName in worldGraph[levelName]) {
			if (!(loadedSceneNames.Contains(connectedSceneName) || currentlyLoadingSceneNames.Contains(connectedSceneName))) {
				currentlyLoadingSceneNames.Add(connectedSceneName);
				SceneManager.LoadSceneAsync(connectedSceneName, LoadSceneMode.Additive);
			}
		}
	}

	public string GetSceneName(Level level) {
		return enumToSceneName[level];
	}

	private void PopulateSceneNames() {
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
		enumToSceneName.Add(Level.forkBlackRoom, forkBlackRoom);
		enumToSceneName.Add(Level.tutorialHallway, tutorialHallway);
		enumToSceneName.Add(Level.tutorialRoom, tutorialRoom);
		enumToSceneName.Add(Level.transition2_3, transition2_3);
		enumToSceneName.Add(Level.transition3_4, transition3_4);
        enumToSceneName.Add(Level.invisFloor, invisFloor);
		enumToSceneName.Add(Level.metaEdgeDetection, metaEdgeDetection);
		enumToSceneName.Add(Level.portalTestScene, portalTestScene);
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
		worldGraph.Add(fork, new List<string>() { forkWhiteRoom, forkBlackRoom });
		worldGraph.Add(forkWhiteRoom, new List<string>() { fork, metaEdgeDetection, forkWhiteRoom2 });
		worldGraph.Add(forkWhiteRoom2, new List<string>() { forkWhiteRoom });
		worldGraph.Add(forkBlackRoom, new List<string>() { fork });
        worldGraph.Add(invisFloor, new List<string>());

		worldGraph.Add(tutorialHallway, new List<string>() { emptyRoom, tutorialRoom });
		worldGraph.Add(tutorialRoom, new List<string>() { tutorialHallway, axis });
		worldGraph.Add(transition2_3, new List<string>() { hexPillarRoom, level3 });
		worldGraph.Add(transition3_4, new List<string>() { level3, level4 });

		worldGraph.Add(metaEdgeDetection, new List<string>() { forkWhiteRoom });
		worldGraph.Add(portalTestScene, new List<string>() { });
	}

	/// <summary>
	/// Unloads any scene that is not the selected scene or connected to it as defined by the world graph.
	/// </summary>
	/// <param name="selectedScene"></param>
	private void DeactivateUnrelatedScenes(string selectedScene) {
		List<string> scenesToDeactivate = new List<string>();
		foreach (string currentlyActiveScene in loadedSceneNames) {
			if (currentlyActiveScene != selectedScene && !worldGraph[selectedScene].Contains(currentlyActiveScene)) {
				scenesToDeactivate.Add(currentlyActiveScene);
			}
		}
		foreach (string sceneToDeactivate in scenesToDeactivate) {
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
		}

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
}
