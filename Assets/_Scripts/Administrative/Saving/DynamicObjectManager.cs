using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SerializableClasses;
using System;
using static Saving.DynamicObject;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Experimental.AI;

namespace Saving {
	public static class DynamicObjectManager {
		public static Dictionary<string, DynamicObject> allDynamicObjects = new Dictionary<string, DynamicObject>();
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
			DynamicObjectType type = (DynamicObjectType)dynamicObjSave.Type;
			if (type == DynamicObjectType.NotSet) {
				Debug.LogError("Can't instantiate dynamic object without a type set");
				return null;
			}
			string prefabPath = prefabPathPrefix + GetPrefabPathSuffix(type);
			DynamicObject prefab = Resources.Load<DynamicObject>(prefabPath);
			return GameObject.Instantiate(prefab);
		}

		private static void InitializeDynamicObjectsDict() {
			allDynamicObjects = Resources.FindObjectsOfTypeAll<DynamicObject>()
				.Where(d =>d.ID != null && d.ID != "")
				.ToDictionary(d => d.ID);
		}

		public static void SaveDynamicObjects(string saveFileName) {
			string directoryPath = SavePath(saveFileName);
			string saveFile = $"{directoryPath}/{filename}";

			// TODO: Load the existing save and perform a union on the serialized dynamic object info
			InitializeDynamicObjectsDict();
			DynamicObjectsSaveFile unionSave = GetDynamicObjectsSaveFile(saveFileName) ?? new DynamicObjectsSaveFile(new Dictionary<string, DynamicObject>());
			DynamicObjectsSaveFile currentDynamicObjects = new DynamicObjectsSaveFile(allDynamicObjects);
			unionSave.Merge(currentDynamicObjects);

			BinaryFormatter bf = new BinaryFormatter();
			Directory.CreateDirectory(directoryPath);
			FileStream file = File.Create(saveFile);
			bf.Serialize(file, unionSave);
			file.Close();
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

		public static void LoadFromSaveFile(DynamicObjectsSaveFile saveFile) {
			if (saveFile != null) {
				InitializeDynamicObjectsDict();
				foreach (var id in saveFile.serializedDynamicObjects.Keys) {
					// If this ID was not found in the game, and it should exist (its scene is loaded), create it
					if (!allDynamicObjects.ContainsKey(id) && LevelManager.instance.loadedSceneNames.Contains(saveFile.serializedDynamicObjects[id].scene)) {
						DynamicObject newDynamicObject = CreateInstanceFromSavedInfo(saveFile.serializedDynamicObjects[id]);
						newDynamicObject.id.uniqueId = id;
						Debug.Log($"Instantiating object of id: {id}, type: {newDynamicObject.Type}");
						allDynamicObjects.Add(id, newDynamicObject);
					}
				}

				foreach (var id in allDynamicObjects.Keys) {
					if (saveFile.serializedDynamicObjects.ContainsKey(id)) {
						saveFile.serializedDynamicObjects[id].LoadSave(allDynamicObjects[id]);
					}
				}
			}
			else {
				return;
			}
		}

		// Step 1: Load all dynamic objects or create instances of them if they do not exist
		public static void LoadOrCreateDynamicObjects(string saveFileName) {
			DynamicObjectsSaveFile save = GetDynamicObjectsSaveFile(saveFileName);

			LoadFromSaveFile(save);
		}

		[Serializable]
		public class DynamicObjectsSaveFile {
			public Dictionary<string, DynamicObjectSave> serializedDynamicObjects = new Dictionary<string, DynamicObjectSave>();

			public DynamicObjectsSaveFile() {
				this.serializedDynamicObjects = new Dictionary<string, DynamicObjectSave>();
				foreach (var kv in allDynamicObjects) {
					serializedDynamicObjects.Add(kv.Key, kv.Value.GetSaveObject() as DynamicObjectSave);
				}
			}

			//public void LoadSave() {
			//	InitializeDynamicObjectsDict();

			//	foreach (var id in serializedDynamicObjects.Keys) {
			//		// If this ID was not found in the game
			//		if (!allDynamicObjects.ContainsKey(id)) {
			//			DynamicObject newDynamicObject = CreateInstanceFromSavedInfo(serializedDynamicObjects[id]);
			//			newDynamicObject.id.uniqueId = id;
			//			Debug.Log($"Instantiating object of id: {id}, type: {newDynamicObject.Type}");
			//			allDynamicObjects.Add(id, newDynamicObject);
			//		}
			//	}

			//	foreach (var id in allDynamicObjects.Keys) {
			//		serializedDynamicObjects[id].LoadSave(allDynamicObjects[id]);
			//	}
			//}

			public DynamicObjectsSaveFile(Dictionary<string, DynamicObject> objs) {
				this.serializedDynamicObjects = new Dictionary<string, DynamicObjectSave>();
				foreach (var id in objs.Keys) {
					serializedDynamicObjects.Add(id, objs[id].GetSaveObject() as DynamicObjectSave);
				}
			}

			public void Merge(DynamicObjectsSaveFile other) {
				other.serializedDynamicObjects.ToList().ForEach(x => this.serializedDynamicObjects[x.Key] = x.Value);
			}
		}
	}
}