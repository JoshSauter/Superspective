﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
	transition2_3,
	transition3_4
}

public class LevelManager : Singleton<LevelManager> {
	public Level startingScene;

	Dictionary<Level, string> enumToSceneName;
	Dictionary<string, List<string>> worldGraph;
	string activeSceneName;
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

	private const string tutorialHallway = "_TutorialHallway";
	private const string transition2_3 = "_Transition2_3";
	private const string transition3_4 = "_Transition3_4";

#endregion

	public void Start() {
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

		SceneManager.sceneLoaded += FinishLoadingScene;
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

	public string GetSceneName(Level level) {
		return enumToSceneName[level];
	}

	private void PopulateSceneNames() {
#if UNITY_EDITOR
		enumToSceneName.Add(Level.testScene, testScene);
#endif
		enumToSceneName.Add(Level.managerScene, managerScene);
		enumToSceneName.Add(Level.emptyRoom, emptyRoom);
		enumToSceneName.Add(Level.hexPillarRoom, hexPillarRoom);
		enumToSceneName.Add(Level.library, library);
		enumToSceneName.Add(Level.level3, level3);
		enumToSceneName.Add(Level.level4, level4);
		enumToSceneName.Add(Level.tutorialHallway, tutorialHallway);
		enumToSceneName.Add(Level.transition2_3, transition2_3);
		enumToSceneName.Add(Level.transition3_4, transition3_4);
	}

	/// <summary>
	/// Defines the world graph which determines which scenes are adjacent to one another.
	/// </summary>
	private void PopulateWorldGraph() {
#if UNITY_EDITOR
		worldGraph.Add(testScene, new List<string>());
#endif

		worldGraph.Add(emptyRoom, new List<string>() { tutorialHallway });
		worldGraph.Add(hexPillarRoom, new List<string>() { tutorialHallway, library });
		worldGraph.Add(library, new List<string>() { hexPillarRoom, tutorialHallway });
		worldGraph.Add(level3, new List<string>() { transition2_3, transition3_4 });
		worldGraph.Add(level4, new List<string>() { transition3_4 });

		worldGraph.Add(tutorialHallway, new List<string>() { emptyRoom, hexPillarRoom });
		worldGraph.Add(transition2_3, new List<string>() { hexPillarRoom, level3 });
		worldGraph.Add(transition3_4, new List<string>() { level3, level4 });
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