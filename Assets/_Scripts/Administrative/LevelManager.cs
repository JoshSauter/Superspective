using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : Singleton<LevelManager> {
	public string startingScene;

	Dictionary<string, List<string>> worldGraph;
	string activeSceneName;
	List<string> loadedSceneNames;
	List<string> currentlyLoadingSceneNames;
	List<string> currentlyUnloadingSceneNames;

#region level names
	private const string managerScene = "_ManagerScene";
	private const string testScene = "_TestScene";

	private const string level1 = "_Level1";
	private const string level2 = "_Level2";
	private const string level3 = "_Level3";

	private const string transition1_2 = "_Transition1_2";
	private const string transition2_3 = "_Transition2_3";
#endregion

	public void Start() {
		loadedSceneNames = new List<string>();
		currentlyLoadingSceneNames = new List<string>();
		currentlyUnloadingSceneNames = new List<string>();

		worldGraph = new Dictionary<string, List<string>>();
		PopulateWorldGraph();

#if UNITY_EDITOR
		PopulateAlreadyLoadedScenes();
#endif


		SceneManager.sceneLoaded += FinishLoadingScene;
		SceneManager.sceneUnloaded += FinishUnloadingScene;

		if (startingScene != null) {
			SwitchActiveScene(startingScene);
		}
	}

	/// <summary>
	/// Switches the active scene, loads the connected scenes as defined by worldGraph, and unloads all other currently loaded scenes.
	/// </summary>
	/// <param name="levelName">Name of the scene to become active</param>
	public void SwitchActiveScene(string levelName) {
		if (!worldGraph.ContainsKey(levelName)) {
			Debug.LogError("No level name found in world graph with name " + levelName);
			return;
		}

		if (activeSceneName == levelName) {
			Debug.LogWarning("Level " + levelName + " already the active scene.");
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

	/// <summary>
	/// Defines the world graph which determines which scenes are adjacent to one another.
	/// </summary>
	private void PopulateWorldGraph() {
#if UNITY_EDITOR
		worldGraph.Add(testScene, new List<string>());
#endif

		worldGraph.Add(level1, new List<string>() { transition1_2 });
		worldGraph.Add(level2, new List<string>() { transition1_2, transition2_3 });
		worldGraph.Add(level3, new List<string>() { transition2_3 });

		worldGraph.Add(transition1_2, new List<string>() { level1, level2 });
		worldGraph.Add(transition2_3, new List<string>() { level2, level3 });
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
	/// <param name="mode">(Unused) Additive or Single</param>
	private void FinishLoadingScene(Scene loadedScene, LoadSceneMode mode) {
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
			Debug.LogError("Just unloaded the active scene!");
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
