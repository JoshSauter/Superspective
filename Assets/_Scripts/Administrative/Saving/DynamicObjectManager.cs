using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using static Saving.DynamicObject;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Saving {
	public static class DynamicObjectManager {
		// Scene: ID => DynamicObject
		public static Dictionary<string, Dictionary<string, DynamicObject>> dynamicObjectsInActiveScenes = new Dictionary<string, Dictionary<string, DynamicObject>>();
		public static Dictionary<string, Dictionary<string, DynamicObjectSave>> dynamicObjectsInInactiveScenes = new Dictionary<string, Dictionary<string, DynamicObjectSave>>();
		public static Dictionary<string, string> allDynamicObjectsSceneById = new Dictionary<string, string>();

		public delegate void DynamicObjectCreated(string id);
		public static event DynamicObjectCreated OnDynamicObjectCreated;

		private static string SavePath(string saveFileName) {
			return $"{Application.persistentDataPath}/Saves/{saveFileName}";
		}
		private const string filename = "DynamicObjects.save";

		public enum DynamicObjectType {
			NotSet,
			PickupCube,
			PickupCubeRed,
			PickupCubeGreen,
			PickupCubeBlue,
			MultiDimensionCube
		}
		private const string prefabPathPrefix = "Prefabs/";
		private const string pickupCubePrefabPath = "PickUpCubes/PickUpCube";
		private const string pickupCubeRedPrefabPath = "PickUpCubes/PickUpCubeRed";
		private const string pickupCubeGreenPrefabPath = "PickUpCubes/PickUpCubeGreen";
		private const string pickupCubeBluePrefabPath = "PickUpCubes/PickUpCubeBlue";
		private const string multiDimensionCubePrefabPath = "PickUpCubes/MultiDimensionCube";

		private static string GetPrefabPathSuffix(DynamicObjectType type) {
			switch (type) {
				case DynamicObjectType.PickupCube:
					return pickupCubePrefabPath;
				case DynamicObjectType.PickupCubeRed:
					return pickupCubeRedPrefabPath;
				case DynamicObjectType.PickupCubeGreen:
					return pickupCubeGreenPrefabPath;
				case DynamicObjectType.PickupCubeBlue:
					return pickupCubeBluePrefabPath;
				case DynamicObjectType.MultiDimensionCube:
					return multiDimensionCubePrefabPath;
				default:
					return "";
			}
		}

		public static DynamicObject CreateInstanceFromSavedInfo(DynamicObjectSave dynamicObjSave) {
			string prefabPath = dynamicObjSave.prefabPath;
			DynamicObject prefab = Resources.Load<DynamicObject>(prefabPath);
			if (prefab == null) {
				Debug.LogError($"Can't instantiate dynamic object from prefabPath: {prefabPath}");
				return null;
			}
			DynamicObject newObject = GameObject.Instantiate(prefab);
			newObject.prefabPath = dynamicObjSave.prefabPath;
			if (newObject.gameObject.scene.name != dynamicObjSave.scene) {
				SceneManager.MoveGameObjectToScene(newObject.gameObject, SceneManager.GetSceneByName(dynamicObjSave.scene));
			}
			return newObject;
		}

		private static void InitializeDynamicObjectsDict() {
			var allDynamicObjects = Resources.FindObjectsOfTypeAll<DynamicObject>()
				.Where(d => d.ID != null && d.ID != "" && !d.SkipSave && d.gameObject.scene.name != null);
			dynamicObjectsInActiveScenes = allDynamicObjects
				.GroupBy(d => d.gameObject.scene.name)
				.ToDictionary(d => d.Key, d => d.ToList())
				.ToDictionary(kv => kv.Key, kv => kv.Value.ToDictionary(d => d.ID));
			allDynamicObjectsSceneById = allDynamicObjects
				.ToDictionary(d => d.ID, d => d.gameObject.scene.name);
		}

		public static void DeleteAllExistingDynamicObjects() {
			InitializeDynamicObjectsDict();

			List<string> scenes = dynamicObjectsInActiveScenes.Keys.ToList();
			foreach (var sceneName in scenes) {
				DeleteExistingDynamicObjects(sceneName);
			}
		}

		public static void DeleteExistingDynamicObjects(string sceneName) {
			InitializeDynamicObjectsDict();

			Dictionary<string, DynamicObject> dynamicObjectsInScene = dynamicObjectsInActiveScenes[sceneName];

			List<string> ids = dynamicObjectsInScene.Keys.ToList();
			foreach (var id in ids) {
				GameObject objToBeDeleted = dynamicObjectsInScene[id].gameObject;
				dynamicObjectsInScene.Remove(id);
				allDynamicObjectsSceneById.Remove(id);
				GameObject.Destroy(objToBeDeleted);
			}

			if (dynamicObjectsInScene.Count == 0) {
				dynamicObjectsInActiveScenes.Remove(sceneName);
			}
		}

		public static void SaveAllDynamicObjectsToDisk(string saveFileName) {
			string directoryPath = SavePath(saveFileName);
			string saveFile = $"{directoryPath}/{filename}";

			InitializeDynamicObjectsDict();
			DynamicObjectsSaveFile save = DynamicObjectsSaveFile.CreateSaveFileFromCurrentState();

			BinaryFormatter bf = new BinaryFormatter();
			Directory.CreateDirectory(directoryPath);
			FileStream file = File.Create(saveFile);
			bf.Serialize(file, save);
			file.Close();
		}

		public static void SaveDynamicObjectsForScene(string sceneName) {
			if (dynamicObjectsInActiveScenes.ContainsKey(sceneName)) {
				InitializeDynamicObjectsDict();
				Dictionary<string, DynamicObjectSave> dynamicObjectSavesForScene = dynamicObjectsInActiveScenes[sceneName]
					.ToDictionary(kv => kv.Key, kv => kv.Value.GetSaveObject() as DynamicObjectSave);

				dynamicObjectsInActiveScenes.Remove(sceneName);
				dynamicObjectsInInactiveScenes[sceneName] = dynamicObjectSavesForScene;
			}
		}

		public static void LoadDynamicObjectsFromDisk(string saveFileName) {
			DynamicObjectsSaveFile save = GetDynamicObjectsSaveFile(saveFileName);

			LoadFromSaveFile(save);
		}

		public static void LoadFromSaveFile(DynamicObjectsSaveFile save) {
			DeleteAllExistingDynamicObjects();
			dynamicObjectsInInactiveScenes = save.serializedDynamicObjects;

			foreach (var activeScene in LevelManager.instance.loadedSceneNames) {
				LoadDynamicObjectsForScene(activeScene);
			}
		}

		public static void LoadDynamicObjectsForScene(string sceneName) {
			if (dynamicObjectsInInactiveScenes.ContainsKey(sceneName)) {
				InitializeDynamicObjectsDict();

				Dictionary<string, DynamicObjectSave> savedSceneInfo = dynamicObjectsInInactiveScenes[sceneName];
				Dictionary<string, DynamicObject> loadedSceneInfo = new Dictionary<string, DynamicObject>();
				foreach (var id in savedSceneInfo.Keys) {
					// Skip object creation if an object with that ID already exists
					if (dynamicObjectsInActiveScenes.ContainsKey(sceneName) && dynamicObjectsInActiveScenes[sceneName].ContainsKey(id)) {
						continue;
					}
					DynamicObjectSave savedObject = savedSceneInfo[id];

					DynamicObject newDynamicObject = CreateInstanceFromSavedInfo(savedObject);
					newDynamicObject.id.uniqueId = id;
					savedObject.LoadSave(newDynamicObject);
					OnDynamicObjectCreated?.Invoke(id);
					loadedSceneInfo.Add(id, newDynamicObject);
				}

				dynamicObjectsInActiveScenes[sceneName] = loadedSceneInfo;
				dynamicObjectsInInactiveScenes.Remove(sceneName);
			}
		}

		private static DynamicObjectsSaveFile GetDynamicObjectsSaveFile(string saveFileName) {
			string directoryPath = SavePath(saveFileName);
			string saveFile = $"{directoryPath}/{filename}";

			if (Directory.Exists(directoryPath) && File.Exists(saveFile)) {
				BinaryFormatter bf = new BinaryFormatter();
				FileStream file = File.Open(saveFile, FileMode.Open);
				DynamicObjectsSaveFile save = (DynamicObjectsSaveFile)bf.Deserialize(file);
				file.Close();

				return save;
			}
			else {
				return null;
			}
		}

		[Serializable]
		public class DynamicObjectsSaveFile {
			public Dictionary<string, Dictionary<string, DynamicObjectSave>> serializedDynamicObjects = new Dictionary<string, Dictionary<string, DynamicObjectSave>>();

			public static DynamicObjectsSaveFile CreateSaveFileFromCurrentState() {
				DynamicObjectsSaveFile save = new DynamicObjectsSaveFile();
				save.serializedDynamicObjects = new Dictionary<string, Dictionary<string, DynamicObjectSave>>();
				foreach (var kv in dynamicObjectsInActiveScenes) {
					string scene = kv.Key;
					Dictionary<string, DynamicObject> dynamicObjectsInScene = kv.Value;

					Dictionary<string, DynamicObjectSave> idToSaveObj = new Dictionary<string, DynamicObjectSave>();
					foreach (var idToObject in dynamicObjectsInScene) {
						idToSaveObj.Add(idToObject.Key, idToObject.Value.GetSaveObject() as DynamicObjectSave);
					}
					save.serializedDynamicObjects.Add(kv.Key, kv.Value.ToDictionary(idToObj => idToObj.Key, idToObj => idToObj.Value.GetSaveObject() as DynamicObjectSave));
				}
				foreach (var kv in dynamicObjectsInInactiveScenes) {
					save.serializedDynamicObjects.Add(kv.Key, kv.Value);
				}

				return save;
			}
		}
	}
}