using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using static Saving.DynamicObject;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using SuperspectiveUtils;
using LevelManagement;
using Library.Functional;
using Object = UnityEngine.Object;

namespace Saving {
	public enum RegistrationStatus {
		ExistingRecordInAnotherScene,
		ExistingRecordIsDestroyed,
		RegisteredSuccessfully
	}
	public static class RegistrationStatusExtensions {
		public static bool IsSuccess(this RegistrationStatus status) {
			return status == RegistrationStatus.RegisteredSuccessfully;
		}

		public static bool IsAlreadyDestroyed(this RegistrationStatus status) {
			return status == RegistrationStatus.ExistingRecordIsDestroyed;
		}
	}
	public static class DynamicObjectManager {
		// A DynamicObjectRecord keeps track of all DynamicObjects that have ever existed in this save file
		// This includes DynamicObjects that have ceased to exist, so they are not recreated when their original scene is loaded
		[Serializable]
		struct DynamicObjectRecord {
			public string id;
			public string sceneName;
			public bool isDestroyed;

			public DynamicObjectRecord(DynamicObject dynamicObject) {
				this.id = dynamicObject.ID;
				this.sceneName = dynamicObject.gameObject.scene.name;
				this.isDestroyed = false;
			}
		}
		// ID => Record
		static Dictionary<string, DynamicObjectRecord> allDynamicObjectRecords = new Dictionary<string, DynamicObjectRecord>();

		public delegate void DynamicObjectCreated(string id);
		public static event DynamicObjectCreated OnDynamicObjectCreated;

		static string SavePath(string saveFileName) {
			return $"{Application.persistentDataPath}/Saves/{saveFileName}";
		}

		const string filename = "DynamicObjects.save";

		public static DynamicObject CreateInstanceFromSavedInfo(string id, DynamicObjectSave dynamicObjSave) {
			// Use a root GameObject to instantiate the object directly into the appropriate scene
			DynamicObject InstantiateInScene(DynamicObject dynamicObjectPrefab, string sceneName) {
				List<GameObject> gameObjects = new List<GameObject>();
				SceneManager.GetSceneByName(dynamicObjSave.scene).GetRootGameObjects(gameObjects);
				gameObjects = gameObjects.Where(go => go.activeSelf).ToList();
				DynamicObject newDynamicObject = Object.Instantiate(dynamicObjectPrefab, gameObjects[0].transform);
				//newDynamicObject.transform.SetParent(null);

				return newDynamicObject;
			}
			
			string prefabPath = dynamicObjSave.prefabPath;
			DynamicObject prefab = Resources.Load<DynamicObject>(prefabPath);
			if (prefab == null) {
				Debug.LogError($"Can't instantiate dynamic object from prefabPath: {prefabPath}");
				return null;
			}
			DynamicObject newObject = InstantiateInScene(prefab, dynamicObjSave.scene);
			newObject.prefabPath = dynamicObjSave.prefabPath;

			newObject.id.uniqueId = id;
			dynamicObjSave.LoadSave(newObject);
			newObject.RegisterDynamicObjectUponCreation(id);
			OnDynamicObjectCreated?.Invoke(id);
			// Any SaveableObjects on the newly created DynamicObject need to register themselves before Start(), do it now
			foreach (var saveable in newObject.transform.GetComponentsInChildrenRecursively<SaveableObject>()) {
				var saveableObject = (ISaveableObject)saveable;
				saveableObject.Register();
			}
			return newObject;
		}
		
		// Will be called from a DynamicObject's Awake call, but not during DynamicObject scene change
		public static RegistrationStatus RegisterDynamicObject(DynamicObject dynamicObject, string sceneName) {
			string id = dynamicObject.ID;
			if (allDynamicObjectRecords.ContainsKey(id)) {
				// Disallow DynamicObject registration if it already exists in another scene (use ChangeDynamicObjectScene to update the scene)
				if (allDynamicObjectRecords[id].sceneName != sceneName) {
					Debug.LogError($"Trying to register DynamicObject {dynamicObject}, scene {sceneName}, but existing record for id {id} is in a different scene: {allDynamicObjectRecords[id].sceneName}");
					return RegistrationStatus.ExistingRecordInAnotherScene;
				}
				// Disallow DynamicObject registration if we have a record of the object having been destroyed
				if (allDynamicObjectRecords[id].isDestroyed) {
					Debug.LogError($"Trying to register DynamicObject {dynamicObject}, scene {sceneName}, but existing record for id {id} has been destroyed");
					return RegistrationStatus.ExistingRecordIsDestroyed;
				}
				
				// Existing record is (likely) for the object being registered, nothing to update
				return RegistrationStatus.RegisteredSuccessfully;
			}

			allDynamicObjectRecords.Add(id, new DynamicObjectRecord(dynamicObject));
			return RegistrationStatus.RegisteredSuccessfully;
		}

		public static bool MarkDynamicObjectAsDestroyed(Either<DynamicObject, DynamicObjectSave> dynamicObject, string sceneName) {
			string id = dynamicObject.Match(
				dynamicObj => dynamicObj.ID,
				dynamicObjSave => dynamicObjSave.ID
			);
			if (!allDynamicObjectRecords.ContainsKey(id)) {
				Debug.LogError($"Trying to mark DynamicObject {dynamicObject} as destroyed, but allDynamicObjectRecords contains no entry for {id}");
				return false;
			}

			if (allDynamicObjectRecords[id].sceneName != sceneName) {
				Debug.LogError($"Trying to mark DynamicObject {dynamicObject} in scene {sceneName} as destroyed, but existing entry for id {id} is in a different scene: {allDynamicObjectRecords[id].sceneName}");
				return false;
			}

			DynamicObjectRecord record = allDynamicObjectRecords[id];
			record.isDestroyed = true;
			allDynamicObjectRecords[id] = record;
			return true;
		}

		public static bool ChangeDynamicObjectScene(DynamicObject dynamicObject, string oldScene, string newScene) {
			string id = dynamicObject.ID;
			if (!allDynamicObjectRecords.ContainsKey(id)) {
				Debug.LogError($"Trying to change scene for DynamicObject {dynamicObject}, but there is no entry for id {id}");
				return false;
			}

			if (allDynamicObjectRecords[id].sceneName == newScene) {
				Debug.LogWarning($"Trying to change scene for DynamicObject {dynamicObject}, but existing entry for id {id} is already in {newScene}. Nothing to do.");
				return true;
			}
			
			// Update all associated SaveableObject IDs' scenes
			SaveManagerForScene oldSceneSaveManager = SaveManager.GetOrCreateSaveManagerForScene(oldScene);
			SaveManagerForScene newSceneSaveManager = SaveManager.GetOrCreateSaveManagerForScene(newScene);
			oldSceneSaveManager.UnregisterDynamicObject(id);
			newSceneSaveManager.RegisterDynamicObject(dynamicObject);
			SaveManager.GetAllAssociatedSaveableObjects(dynamicObject.ID).ForEach(associatedSO => {
				string associatedId = associatedSO.ID;
				oldSceneSaveManager.UnregisterSaveableObject(associatedId);
				SaveManager.sceneLookupForId[associatedId] = newScene;
				newSceneSaveManager.RegisterSaveableObject(associatedSO);
			});

			DynamicObjectRecord record = allDynamicObjectRecords[id];
			record.sceneName = newScene;
			allDynamicObjectRecords[id] = record;
			return true;
		}

		public static DynamicObjectsSaveFile GetDynamicObjectRecordsSave() {
			return DynamicObjectsSaveFile.CreateSaveFileFromCurrentState();
		}

		public static void DeleteAllExistingDynamicObjectsAndClearState() {
			foreach (var scene in LevelManager.instance.loadedSceneNames) {
				SaveManager.GetOrCreateSaveManagerForScene(scene)?.DeleteAllDynamicObjectsInScene();
			}
			allDynamicObjectRecords.Clear();
		}

		public static void LoadAllDynamicObjectRecords(string saveFileName) {
			GetDynamicObjectsSaveFile(saveFileName)?.LoadSaveFile();
		}

		static DynamicObjectsSaveFile GetDynamicObjectsSaveFile(string saveFileName) {
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
			Dictionary<string, DynamicObjectRecord> dynamicObjectRecords;

			public static DynamicObjectsSaveFile CreateSaveFileFromCurrentState() {
				DynamicObjectsSaveFile save = new DynamicObjectsSaveFile {
					dynamicObjectRecords = DynamicObjectManager.allDynamicObjectRecords
				};

				return save;
			}

			public void LoadSaveFile() {
				DynamicObjectManager.allDynamicObjectRecords = dynamicObjectRecords;
			}
		}
	}
}