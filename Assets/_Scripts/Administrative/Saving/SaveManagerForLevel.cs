using SuperspectiveUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using LevelManagement;
using Library.Functional;
using UnityEngine;
using static Saving.DynamicObject;
using Object = UnityEngine.Object;

namespace Saving {
    public static partial class SaveManager {
        public class SaveManagerForLevel {
            private SaveManagerForLevel() { }

            public SaveManagerForLevel(Levels level) {
                this.level = level;
            }

            public readonly Levels level;

            public bool SceneIsLoaded => level == Levels.ManagerScene ||
                                         LevelManager.instance.loadedLevels.Contains(level);

            private Dictionary<string, SuperspectiveObject> superspectiveObjects = new Dictionary<string, SuperspectiveObject>();
            private Dictionary<string, DynamicObject> dynamicObjects = new Dictionary<string, DynamicObject>();

            private Dictionary<string, SaveObject> serializedSaveObjects = new Dictionary<string, SaveObject>();
            private Dictionary<string, DynamicObjectSave> serializedDynamicObjects = new Dictionary<string, DynamicObjectSave>();

            private static bool DEBUG = false;
            private static DebugLogger _debug;
            private static DebugLogger debug => _debug ??= new DebugLogger(LevelManager.instance, () => DEBUG);

            /// <summary>
            /// Register a SuperspectiveObject, if it's not already registered.
            /// This should happen on Awake for all SuperspectiveObjects when its scene is loaded.
            /// </summary>
            /// <param name="superspectiveObj">SuperspectiveObject to be registered</param>
            /// <returns>True if the object was registered, false otherwise</returns>
            internal bool RegisterSuperspectiveObjectInScene(SuperspectiveObject superspectiveObj) {
                debug.LogWithContext($"Registering SuperspectiveObject {superspectiveObj.ID} in {level}", superspectiveObj);
                
                string id = superspectiveObj.ID;
                if (superspectiveObjects.ContainsKey(id)) {
                    if (superspectiveObjects[id] == null) {
                        superspectiveObjects[id] = superspectiveObj;
                        return true;
                    }

                    debug.LogWarning($"superspectiveObjects already contains key {id}: {superspectiveObjects[id]}");
                    return false;
                }

                superspectiveObjects.Add(id, superspectiveObj);
                return true;
            }

            /// <summary>
            /// Unregister a SuperspectiveObject, if it's already registered.
            /// This should only happen if a SuperspectiveObject is explicitly destroyed (e.g. when its associated DynamicObject is explicitly destroyed)
            /// </summary>
            /// <param name="id">ID of the SuperspectiveObject to deregister</param>
            /// <returns>True if the SuperspectiveObject was successfully unregistered, false otherwise</returns>
            internal bool UnregisterSuperspectiveObjectInScene(string id) {
                if (SceneIsLoaded) {
                    if (!superspectiveObjects.ContainsKey(id)) {
                        debug.LogWarning($"Attempting to remove SuperspectiveObject with id: {id}, but no entry for that id exists.");
                        return false;
                    }
                    
                    debug.LogWithContext($"Unregistering SuperspectiveObject {id} in {level}", superspectiveObjects[id]);

                    superspectiveObjects.Remove(id);
                    return true;
                }
                else {
                    if (!serializedSaveObjects.ContainsKey(id)) {
                        debug.LogWarning($"Attempting to remove SaveObject with id: {id}, but no entry for that id exists.");
                        return false;
                    }

                    serializedSaveObjects.Remove(id);
                    return true;
                }
            }

            /// <summary>
            /// Will return a SuperspectiveObject reference if this scene is active, or a SaveObject if the scene is inactive
            /// Will return null if the id is not found in its respective dictionary
            /// </summary>
            /// <param name="id">ID of the SuperspectiveObject or SaveObject to be retrieved</param>
            /// <returns></returns>
            public Either<SuperspectiveObject, SaveObject> GetSuperspectiveObjectInScene(string id) {
                if (SceneIsLoaded) {
                    if (superspectiveObjects.ContainsKey(id)) {
                        if (superspectiveObjects[id] == null) {
                            SuperspectiveObject foundSuperspectiveObject = LookForMissingSaveableObject<SuperspectiveObject>(id);
                            if (foundSuperspectiveObject != null) {
                                return foundSuperspectiveObject;
                            }

                            superspectiveObjects.Remove(id);
                            return null;

                        }

                        return superspectiveObjects[id];
                    }

                    SuperspectiveObject missingSuperspectiveObject = LookForMissingSaveableObject<SuperspectiveObject>(id);
                    if (missingSuperspectiveObject != null) {
                        missingSuperspectiveObject.Register();
                        return missingSuperspectiveObject;
                    }

                    debug.LogWarning($"No SuperspectiveObject found with id {id} in level {level}");
                    return null;
                }
                else {
                    if (serializedSaveObjects.ContainsKey(id)) {
                        return serializedSaveObjects[id];
                    }

                    debug.LogWarning($"No SaveObject found with id {id} in level {level}");
                    return null;
                }
            }

            public Either<DynamicObject, DynamicObjectSave> GetDynamicObject(string id) {
                if (SceneIsLoaded) {
                    if (dynamicObjects.ContainsKey(id)) {
                        if (dynamicObjects[id] == null) {
                            dynamicObjects.Remove(id);
                            return null;
                        }

                        return dynamicObjects[id];
                    }

                    Debug.LogError($"No DynamicObject found with id {id} in level {level}");
                    return null;
                }
                else {
                    if (serializedDynamicObjects.ContainsKey(id)) {
                        return serializedDynamicObjects[id];
                    }

                    Debug.LogError($"No serializedDynamicObject found with id {id} in level {level}");
                    return null;
                }
            }

            /// <summary>
            /// Register a DynamicObject, if it's not already registered.
            /// This should happen on Awake for each DynamicObject when its scene is loaded, or when it is instantiated.
            /// </summary>
            /// <param name="dynamicObject">DynamicObject to be registered</param>
            /// <returns>True if the object was registered, false otherwise</returns>
            internal bool RegisterDynamicObjectInScene(DynamicObject dynamicObject) {
                debug.LogWithContext($"Registering dynamic object {dynamicObject.ID} in {level}", dynamicObject);
                
                string id = dynamicObject.ID;
                if (dynamicObjects.ContainsKey(id)) {
                    debug.LogWarning($"dynamicObjects already contains key {id}: {dynamicObjects[id]}");
                    return false;
                }

                dynamicObjects.Add(id, dynamicObject);
                
                debug.Log($"{dynamicObjects.Count} dynamic objects in {level}");
                
                return true;
            }

            /// <summary>
            /// Unregister a DynamicObject, if it is currently registered.
            /// This should happen when a DynamicObject is explicitly destroyed (not just from a scene change or application quit).
            /// </summary>
            /// <param name="id">ID of the DynamicObject to unregister</param>
            /// <returns>True if the object was unregistered, false otherwise</returns>
            public bool UnregisterDynamicObjectInScene(string id) {
                if (!dynamicObjects.ContainsKey(id)) {
                    debug.LogWarning($"Attempting to remove DynamicObject with id: {id}, but no entry for that id exists.");
                    return false;
                }
                
                debug.LogWithContext($"Unregistering dynamic object {id} in {level}", dynamicObjects[id]);

                dynamicObjects.Remove(id);
                
                debug.Log($"{dynamicObjects.Count} dynamic objects in {level}");
                return true;
            }

            static SaveObject GetSaveObject(ISaveableObject saveableObject) {
                try {
                    return saveableObject.CreateSave();
                }
                catch (Exception e) {
                    string ID = "<Unknown ID>";
                    try {
                        ID = saveableObject.ID;
                    }
                    catch (Exception) { } // Sometimes this fails for some reason, we don't really care as it's just for getting the ID for logging the error

                    Debug.LogError($"Could not get serialized save object for: {ID}, {saveableObject}. Details:\n{e}");
                    return null;
                }
            }

            /// <summary>
            /// Creates a SaveDataForLevel for this scene and returns it
            /// </summary>
            /// <returns>SaveDataForLevel for this scene</returns>
            public SaveDataForLevel GetSaveFileForLevel() {
                debug.Log($"Creating save file for {level}");
                
                Dictionary<string, SaveObject> objectsToSave;
                Dictionary<string, DynamicObjectSave> dynamicObjectsToSave;
                if (SceneIsLoaded) {
                    objectsToSave = superspectiveObjects
                        .Where(kv => !kv.Value.SkipSave)
                        .ToDictionary(kv => kv.Key, kv => GetSaveObject(kv.Value))
                        .Where(kv => kv.Value != null)
                        .ToDictionary();
                    dynamicObjectsToSave = dynamicObjects
                        .Where(kv => !kv.Value.SkipSave)
                        .ToDictionary(kv => kv.Key, kv => kv.Value.CreateSave() as DynamicObjectSave)
                        .Where(kv => kv.Value != null)
                        .ToDictionary();
                }
                else {
                    objectsToSave = serializedSaveObjects;
                    dynamicObjectsToSave = serializedDynamicObjects;
                }

                return new SaveDataForLevel(level, objectsToSave, dynamicObjectsToSave);
            }

            /// <summary>
            /// Gets a SaveDataForLevel, if it exists, for this level from the given save file
            /// </summary>
            /// <param name="saveFileName">Name of the save file to read from</param>
            /// <returns>A SaveDataForLevel read from disk for this level if one exists, null otherwise</returns>
            public SaveDataForLevel GetSaveFromDisk(string saveFileName) {
                string directoryPath = $"{Application.persistentDataPath}/Saves/{saveFileName}";
                string saveFile = $"{directoryPath}/{level.ToName()}.save";

                if (Directory.Exists(directoryPath) && File.Exists(saveFile)) {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(saveFile, FileMode.Open);
                    SaveDataForLevel save = (SaveDataForLevel)bf.Deserialize(file);
                    file.Close();

                    return save;
                }

                return null;
            }

            /// <summary>
            /// Given a SaveDataForLevel for this level, will either get (or create) all DynamicObjects associated with the
            /// currentSaveFile.serializedDynamicObjects and then load their state from the save file for a loaded scene,
            /// or, will copy the serializedDynamicObjects dictionary to this SaveManager for an unloaded scene.
            /// </summary>
            /// <param name="currentSaveData">Save file to load state from</param>
            public void LoadDynamicObjectsStateFromSaveFile(SaveDataForLevel currentSaveData) {
                if (currentSaveData?.serializedDynamicObjects != null) {
                    if (SceneIsLoaded) {
                        foreach (var id in currentSaveData.serializedDynamicObjects.Keys) {
                            DynamicObjectSave dynamicObjectSave = currentSaveData.serializedDynamicObjects[id];
                            DynamicObject dynamicObject = GetOrCreateDynamicObject(id, dynamicObjectSave);

                            dynamicObject.LoadFromSave(dynamicObjectSave);
                        }
                    }
                    else {
                        serializedDynamicObjects = currentSaveData.serializedDynamicObjects;
                    }
                }
            }

            /// <summary>
            /// Given a SaveDataForLevel for this level, will get all SuperspectiveObjects associated with the
            /// currentSaveFile.saveObjects and then load their state from the save file for a loaded scene,
            /// or, will copy the saveObjects dictionary to this SaveManager for an unloaded scene.
            /// </summary>
            /// <param name="currentSaveData">Save file to load state from</param>
            public void LoadSuperspectiveObjectsStateFromSaveFile(SaveDataForLevel currentSaveData) {
                if (currentSaveData?.serializedSaveObjects != null) {
                    if (SceneIsLoaded) {
                        foreach (var id in currentSaveData.serializedSaveObjects.Keys) {
                            SuperspectiveObject superspectiveObject = GetSuperspectiveObjectOrNull(id);
                            if (superspectiveObject == null) {
                                debug.LogWarning($"{id} not found in level {level}");
                                continue;
                            }

                            try {
                                superspectiveObject.LoadFromSave(currentSaveData.serializedSaveObjects[id]);
                            }
                            catch (Exception e) {
                                Debug.LogError($"Error loading ID {id}, cause: " + e);
                            }
                        }
                    }
                    else {
                        serializedSaveObjects = currentSaveData.serializedSaveObjects;
                    }

                    debug.Log($"Loaded {level} from save");
                }
            }

            /// <summary>
            /// Called before a scene is unloaded.
            /// Transfers all state from:
            /// saveableObjects --> serializedSaveableObjects and
            /// dynamicObjects  --> serializedDynamicObjects
            /// 
            /// Then clears saveableObjects && dynamicObjects dictionaries.
            /// </summary>
            public void SerializeSceneState() {
                if (!SceneIsLoaded) {
                    Debug.LogError($"Trying to save state for already unloaded scene {level}");
                    return;
                }

                serializedSaveObjects.Clear();
                foreach (var saveableObjectKV in superspectiveObjects) {
                    string id = saveableObjectKV.Key;
                    SuperspectiveObject superspectiveObject = saveableObjectKV.Value;
                    if (superspectiveObject.SkipSave) continue;

                    try {
                        serializedSaveObjects[id] = superspectiveObject.CreateSave();
                    }
                    catch (Exception e) {
                        Debug.LogError($"Error while trying to get save object for {superspectiveObject}, ID {id} in level {level}:\n{e}");
                    }
                }

                superspectiveObjects.Clear();

                serializedDynamicObjects.Clear();
                foreach (DynamicObject dynamicObject in dynamicObjects.Values) {
                    try {
                        serializedDynamicObjects[dynamicObject.ID] = dynamicObject.CreateSave() as DynamicObjectSave;
                    }
                    catch (Exception e) {
                        Debug.LogError($"Error serializing {dynamicObject.ID}, cause: {e.StackTrace}");
                    }
                }

                dynamicObjects.Clear();
            }

            /// <summary>
            /// Called after a scene is loaded, after RestoreDynamicObjectStateForScene has run for all scenes.
            /// Transfers all state from serializableSaveObjects to saveableObjects, loads the state for each object,
            /// then clears serializableSaveObjects dictionary
            /// </summary>
            public void RestoreSuperspectiveObjectStateForLevel() {
                if (!SceneIsLoaded) {
                    Debug.LogError($"Trying to restore SaveableObject state for unloaded level {level}");
                    return;
                }

                foreach (string id in serializedSaveObjects.Keys) {
                    try {
                        SuperspectiveObject superspectiveObject = GetSuperspectiveObjectOrNull(id);
                        if (superspectiveObject == null) {
                            Debug.LogError($"{id} not found in level {level}");
                            continue;
                        }

                        superspectiveObject.LoadFromSave(serializedSaveObjects[id]);
                    }
                    catch (Exception e) {
                        Debug.LogError($"{id} failed to restore state, cause: {e}");
                    }
                }

                serializedSaveObjects.Clear();
            }

            /// <summary>
            /// Called after a scene is loaded, before any RestoreSaveableObjectsStateForScene.
            /// Transfers all state from serializableDynamicObjects to dynamicObjects, loads the state for each object,
            /// then clears serializableDynamicObjects dictionary
            /// </summary>
            public void RestoreDynamicObjectStateForScene() {
                if (!SceneIsLoaded) {
                    Debug.LogError($"Trying to restore DynamicObject state for unloaded level {level}");
                    return;
                }

                foreach (string id in serializedDynamicObjects.Keys) {
                    try {
                        DynamicObject dynamicObject = GetOrCreateDynamicObject(id, serializedDynamicObjects[id]);
                        dynamicObject.LoadFromSave(serializedDynamicObjects[id]);
                    }
                    catch (Exception e) {
                        Debug.LogError($"{id} DynamicObject failed to restore state, cause: {e}");
                    }
                }

                serializedDynamicObjects.Clear();
            }

            /// <summary>
            /// Finds all unregistered SaveableObjects in the scene and adds them to saveableObjects.
            /// This is necessary due to some objects (such as BladeEdgeDetection) being ExecuteInEditMode,
            /// which causes it to miss the registration step.
            /// Currently, this is explicitly called only for the ManagerScene before saving.
            /// </summary>
            public void ForceGetAllSaveableObjectsInScene() {
                if (SceneIsLoaded) {
                    superspectiveObjects = Resources.FindObjectsOfTypeAll<SuperspectiveObject>()
                        .Where(IsObjectInThisScene)
                        // Dynamic objects are loaded separately
                        .Where(s => !string.IsNullOrEmpty(s.ID))
                        .Where(s => !s.SkipSave)
                        .ToDictionary(s => s.ID);
                }
            }

            /// <summary>
            /// Clears saveableObjects and dynamicObjects if the scene is loaded,
            /// or serializedSaveObjects and serializedDynamicObjects if the scene is unloaded.
            /// Used before loading a save file if this level was never loaded in that save file (missing in the save file)
            /// </summary>
            public void ClearAllStateForScene() {
                debug.Log($"Clearing all state for {level}");
                
                if (SceneIsLoaded) {
                    superspectiveObjects.Clear();
                    dynamicObjects.Clear(); // Should already be clear from calling DeleteAllDynamicObjectsInScene earlier
                }
                else {
                    serializedSaveObjects.Clear();
                    serializedDynamicObjects.Clear();
                }
            }

            /// <summary>
            /// Used before loading a different save file to clean state
            /// </summary>
            public void DeleteAllDynamicObjectsInScene() {
                debug.Log($"Deleting all dynamic objects in {level}");

                List<DynamicObject> dynamicObjectsToDelete = dynamicObjects
                    .Values
                    .Where(d => d != null && d.gameObject != null && d.instantiatedAtRuntime)
                    .ToList();
                foreach (var dynamicObj in dynamicObjectsToDelete) {
                    if (dynamicObj != null && dynamicObj.gameObject != null) {
                        // Don't destroy objects that are just part of the scene
                        if (dynamicObj.instantiatedAtRuntime) {
                            dynamicObj.Destroy();
                        }
                    }
                }
            }

            // Sometimes SaveableObjects don't register themselves by the time they are referenced by something else
            // (for example, if they start deactivated), and we need to find them by ID.
            // This is inefficient and should be avoided when possible.
            T LookForMissingSaveableObject<T>(string id) where T : MonoBehaviour, ISaveableObject {
                // debug.LogWarning($"Having to search for a missing id {id} in scene {sceneName}");
                bool HasValidId(ISaveableObject obj) {
                    try {
                        string s = obj.ID;

                        return !string.IsNullOrEmpty(s);
                    }
                    catch {
                        return false;
                    }
                }

                List<T> allMatches = Resources.FindObjectsOfTypeAll<T>().ToList();

                List<T> matches = Resources.FindObjectsOfTypeAll<T>()
                    .Where(s => HasValidId(s) && s.ID == id && IsObjectInThisScene(s))
                    .ToList();

                if (matches.Count == 0) {
                    debug.LogWarning($"No saveableObject with id {id} found anywhere in level {level}", true);
                    return null;
                }

                if (matches.Count > 1) {
                    Debug.LogError($"Multiple matches for id {id} found:\n{string.Join("\n", matches)}");
                    return null;
                }

                if (!IsObjectInThisScene(matches[0])) {
                    Debug.LogError($"Match found for {id}, but it's in scene {matches[0].gameObject.scene.name} rather than expected {level.ToName()}");
                    return null;
                }

                return matches[0];
            }

            bool IsObjectInThisScene(MonoBehaviour obj) {
                return obj != null && obj.gameObject != null && obj.gameObject.scene.name == level.ToName();
            }

            // Helper method to find an existent DynamicObject (e.g. when its default location is placed in the scene),
            // or, if it doesn't exist, create one from the saved info.
            DynamicObject GetOrCreateDynamicObject(string id, DynamicObjectSave dynamicObjectSave) {
                if (dynamicObjects.ContainsKey(id)) {
                    return dynamicObjects[id];
                }

                // Try finding the object, maybe it already exists in the scene ;)
                var foundDynamicObj = LookForMissingSaveableObject<DynamicObject>(id);
                if (foundDynamicObj != null) {
                    dynamicObjects.Add(id, foundDynamicObj);
                    return foundDynamicObj;
                }

                DynamicObjectManager.CreateInstanceFromSavedInfo(id, dynamicObjectSave);
                if (!dynamicObjects.ContainsKey(id)) {
                    Debug.LogError($"Newly created dynamic object {dynamicObjectSave}, id: {id} not present in dynamicObjects Dictionary");
                    return null;
                }

                return dynamicObjects[id];
            }

            // Helper method for when we know the scene is already in loaded state
            SuperspectiveObject GetSuperspectiveObjectOrNull(string id) {
                return GetSuperspectiveObjectInScene(id)?.Match(
                    result => result,
                    other => null
                );
            }
        }
    }
}
