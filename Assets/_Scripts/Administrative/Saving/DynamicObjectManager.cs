using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using static Saving.DynamicObject;
using UnityEngine.SceneManagement;
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
			public Levels level;
			public bool isDestroyed;

			public DynamicObjectRecord(DynamicObject dynamicObject) {
				this.id = dynamicObject.ID;
				this.level = dynamicObject.Level;
				this.isDestroyed = false;
			}
		}
		// ID => Record
		static Dictionary<string, DynamicObjectRecord> allDynamicObjectRecords = new Dictionary<string, DynamicObjectRecord>();

		public delegate void DynamicObjectCreated(string id);
		public static event DynamicObjectCreated OnDynamicObjectCreated;

		public static DynamicObject CreateInstanceFromSavedInfo(string id, DynamicObjectSave dynamicObjSave) {
			// Use a root GameObject to instantiate the object directly into the appropriate scene
			DynamicObject InstantiateInScene(DynamicObject dynamicObjectPrefab, string sceneName) {
				List<GameObject> gameObjects = new List<GameObject>();
				SceneManager.GetSceneByName(sceneName).GetRootGameObjects(gameObjects);
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
			DynamicObject newObject = InstantiateInScene(prefab, dynamicObjSave.level.ToName());
			newObject.prefabPath = dynamicObjSave.prefabPath;

			newObject.id.uniqueId = id;
			newObject.LoadSave(dynamicObjSave);
			newObject.Register();
			OnDynamicObjectCreated?.Invoke(id);
			// Any SuperspectiveObjects on the newly created DynamicObject need to register themselves before Start(), do it now
			foreach (var superspectiveObject in newObject.transform.GetComponentsInChildrenRecursively<SuperspectiveObject>()) {
				superspectiveObject.Register();
			}
			return newObject;
		}
		
		// Will be called from a DynamicObject's Awake call, but not during DynamicObject scene change
		public static RegistrationStatus RegisterDynamicObject(DynamicObject dynamicObject) {
			string id = dynamicObject.ID;
			Levels level = dynamicObject.Level;

			if (!level.IsValid()) {
				throw new ArgumentException($"DynamicObject {dynamicObject.id} has an invalid level: {level}");
			}
			
			Debug.Log($"Registering DynamicObject {id} in level {level}");
			if (allDynamicObjectRecords.ContainsKey(id)) {
				// Disallow DynamicObject registration if it already exists in another scene (use ChangeDynamicObjectScene to update the scene)
				if (allDynamicObjectRecords[id].level != level) {
					Debug.LogError($"Trying to register DynamicObject {dynamicObject}, level {level}, but existing record for id {id} is in a different level: {allDynamicObjectRecords[id].level}");
					return RegistrationStatus.ExistingRecordInAnotherScene;
				}
				// Disallow DynamicObject registration if we have a record of the object having been destroyed
				if (allDynamicObjectRecords[id].isDestroyed) {
					Debug.LogError($"Trying to register DynamicObject {dynamicObject}, level {level}, but existing record for id {id} has been destroyed");
					return RegistrationStatus.ExistingRecordIsDestroyed;
				}
				
				// Existing record is (likely) for the object being registered, nothing to update
				return RegistrationStatus.RegisteredSuccessfully;
			}

			allDynamicObjectRecords.Add(id, new DynamicObjectRecord(dynamicObject));
			return RegistrationStatus.RegisteredSuccessfully;
		}

		public static bool MarkDynamicObjectAsDestroyed(Either<DynamicObject, DynamicObjectSave> dynamicObject) {
			string id = dynamicObject.Match(
				dynamicObj => dynamicObj.ID,
				dynamicObjSave => dynamicObjSave.ID
			);
			Levels level = dynamicObject.Match(
				dynamicObj => dynamicObj.Level,
				dynamicObjSave => dynamicObjSave.level
			);
			if (!allDynamicObjectRecords.ContainsKey(id)) {
				Debug.LogError($"Trying to mark DynamicObject {dynamicObject} as destroyed, but allDynamicObjectRecords contains no entry for {id}");
				return false;
			}

			if (allDynamicObjectRecords[id].level != level) {
				Debug.LogError($"Trying to mark DynamicObject {dynamicObject} in level {level} as destroyed, but existing entry for id {id} is in a different level: {allDynamicObjectRecords[id].level}");
				return false;
			}

			DynamicObjectRecord record = allDynamicObjectRecords[id];
			record.isDestroyed = true;
			allDynamicObjectRecords[id] = record;
			return true;
		}

		public static bool ChangeDynamicObjectLevel(DynamicObject dynamicObject, Levels oldLevel, Levels newLevel) {
			string id = dynamicObject.ID;
			if (!allDynamicObjectRecords.ContainsKey(id)) {
				Debug.LogError($"Trying to change scene for DynamicObject {dynamicObject}, but there is no entry for id {id}");
				return false;
			}

			if (allDynamicObjectRecords[id].level == newLevel) {
				Debug.LogWarning($"Trying to change level for DynamicObject {dynamicObject}, but existing entry for id {id} is already in {newLevel}. Nothing to do.");
				return true;
			}
			
			// Update all associated SaveableObject IDs' scenes
			SaveManager.ChangeDynamicObjectLevel(dynamicObject, oldLevel, newLevel);

			DynamicObjectRecord record = allDynamicObjectRecords[id];
			record.level = newLevel;
			allDynamicObjectRecords[id] = record;
			return true;
		}

		public static DynamicObjectsSaveFile GetDynamicObjectRecordsSave() {
			return DynamicObjectsSaveFile.CreateSaveFileFromCurrentState();
		}

		public static void DeleteAllExistingDynamicObjectsAndClearState() {
			Debug.Log("Deleting all existing dynamic objects and clearing state");
			foreach (var level in LevelManager.instance.loadedLevels) {
				SaveManager.GetOrCreateSaveManagerForLevel(level)?.DeleteAllDynamicObjectsInScene();
			}
			allDynamicObjectRecords.Clear();
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