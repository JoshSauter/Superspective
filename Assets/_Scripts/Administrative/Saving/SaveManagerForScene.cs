﻿using SuperspectiveUtils;
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
    public class SaveManagerForScene {
        private SaveManagerForScene() { }

        public SaveManagerForScene(string sceneName) {
            this.sceneName = sceneName;
        }
        
        public readonly string sceneName;

        public bool sceneIsLoaded => sceneName == LevelManager.ManagerScene ||
                                     LevelManager.instance.loadedSceneNames.Contains(sceneName);
        
        Dictionary<string, SaveableObject> saveableObjects = new Dictionary<string, SaveableObject>();
        readonly Dictionary<string, DynamicObject> dynamicObjects = new Dictionary<string, DynamicObject>();
        
        Dictionary<string, SerializableSaveObject> serializedSaveObjects = new Dictionary<string, SerializableSaveObject>();
        Dictionary<string, DynamicObjectSave> serializedDynamicObjects = new Dictionary<string, DynamicObjectSave>();

        private static bool DEBUG = false;
        private static DebugLogger _debug;
        private static DebugLogger debug => _debug ??= new DebugLogger(LevelManager.instance, () => DEBUG);
        
        /// <summary>
        /// Register a SaveableObject, if it's not already registered.
        /// This should happen on Awake for all SaveableObjects when its scene is loaded.
        /// </summary>
        /// <param name="saveableObject">SaveableObject to be registered</param>
        /// <returns>True if the object was registered, false otherwise</returns>
        public bool RegisterSaveableObject(SaveableObject saveableObject) {
            string id = saveableObject.ID;
            if (saveableObjects.ContainsKey(id)) {
                if (saveableObjects[id] == null) {
                    saveableObjects[id] = saveableObject;
                    return true;
                }
                debug.LogWarning($"saveableObjects already contains key {id}: {saveableObjects[id]}");
                return false;
            }
            
            SaveManager.sceneLookupForId[id] = saveableObject.gameObject.scene.name;
            saveableObjects.Add(id, saveableObject);
            return true;
        }

        /// <summary>
        /// Unregister a SaveableObject, if it's already registered.
        /// This should only happen if a SaveableObject is explicitly destroyed (e.g. when its associated DynamicObject is explicitly destroyed)
        /// </summary>
        /// <param name="id">ID of the SaveableObject to deregister</param>
        /// <returns>True if the SaveableObject was successfully unregistered, false otherwise</returns>
        public bool UnregisterSaveableObject(string id) {
            if (!saveableObjects.ContainsKey(id)) {
                debug.LogWarning($"Attempting to remove SaveableObject with id: {id}, but no entry for that id exists.");
                return false;
            }

            SaveManager.sceneLookupForId.Remove(id);
            saveableObjects.Remove(id);
            return true;
        }

        /// <summary>
        /// Will attempt to unregister any SaveableObject or DynamicObject whose ID matches the given association ID.
        /// Used when an object is explicitly Destroyed (not by a scene change or application quit) to unregister any
        /// saved state associated with that object
        /// </summary>
        /// <param name="associationId">AssociationId used to find related SaveableObjects & DynamicObjects</param>
        /// <returns>True if all associated saved state is unregistered, false otherwise</returns>
        public bool UnregisterAllAssociatedObjects(string associationId) {
            bool allUnregistrationSuccessful = true;
            if (sceneIsLoaded) {
                // Delete associated SaveableObjects
                List<string> associatedIds = new List<string>();
                foreach (string id in saveableObjects.Keys) {
                    string lastPart = id.Split('_').Last();
                    string curAssociationId = lastPart.IsGuid() ? lastPart : id;

                    if (curAssociationId == associationId) {
                        associatedIds.Add(id);
                    }
                }

                debug.Log($"About to unregister {associatedIds.Count} SaveableObjects: {string.Join("\n", associatedIds)}");
                foreach (string idToDelete in associatedIds) {
                    allUnregistrationSuccessful = allUnregistrationSuccessful && UnregisterSaveableObject(idToDelete);
                }
                
                // Delete associated DynamicObjects
                associatedIds.Clear();
                foreach (string id in dynamicObjects.Keys) {
                    string lastPart = id.Split('_').Last();
                    string curAssociationId = lastPart.IsGuid() ? lastPart : id;

                    if (curAssociationId == associationId) {
                        associatedIds.Add(id);
                    }
                }

                debug.Log($"About to unregister {associatedIds.Count} DynamicObjects: {string.Join("\n", associatedIds)}");
                foreach (string idToDelete in associatedIds) {
                    allUnregistrationSuccessful = allUnregistrationSuccessful && UnregisterDynamicObject(idToDelete);
                }
            }
            // Scene not loaded
            else {
                // Delete associated SerializeableSaveObjects
                List<string> associatedIds = new List<string>();
                foreach (SerializableSaveObject serializedSaveObj in serializedSaveObjects.Values) {
                    if (serializedSaveObj.associationID == associationId) {
                        associatedIds.Add(serializedSaveObj.ID);
                    }
                }

                debug.Log($"About to unregister {associatedIds.Count} SaveableObjects: {string.Join("\n", associatedIds)}");
                foreach (string idToDelete in associatedIds) {
                    allUnregistrationSuccessful = allUnregistrationSuccessful && UnregisterSaveableObject(idToDelete);
                }
                
                // Delete associated SerializeableSaveObjects
                associatedIds.Clear();
                foreach (DynamicObjectSave serializedDynamicObj in serializedDynamicObjects.Values) {
                    if (serializedDynamicObj.associationID == associationId) {
                        associatedIds.Add(serializedDynamicObj.ID);
                    }
                }

                debug.Log($"About to unregister {associatedIds.Count} DynamicObjects: {string.Join("\n", associatedIds)}");
                foreach (string idToDelete in associatedIds) {
                    allUnregistrationSuccessful = allUnregistrationSuccessful && UnregisterSaveableObject(idToDelete);
                }
            }

            return allUnregistrationSuccessful;
        }
        
        /// <summary>
        /// Will return a SaveableObject reference if this scene is active, or a SerializableSaveObject if the scene is inactive
        /// Will return null if the id is not found in its respective dictionary
        /// </summary>
        /// <param name="id">ID of the SaveableObject or SerializableSaveObject to be retrieved</param>
        /// <returns></returns>
        public Either<SaveableObject, SerializableSaveObject> GetSaveableObject(string id) {
            if (sceneIsLoaded) {
                if (saveableObjects.ContainsKey(id)) {
                    if (saveableObjects[id] == null) {
                        SaveableObject foundSaveableObject = LookForMissingSaveableObject<SaveableObject>(id);
                        if (foundSaveableObject != null) {
                            return foundSaveableObject;
                        }
                        saveableObjects.Remove(id);
                        return null;

                    }

                    return saveableObjects[id];
                }

                SaveableObject missingSaveableObject = LookForMissingSaveableObject<SaveableObject>(id);
                if (missingSaveableObject != null) {
                    missingSaveableObject.Register();
                    return missingSaveableObject;
                }

                debug.LogWarning($"No saveableObject found with id {id} in scene {sceneName}");
                return null;
            }
            else {
                if (serializedSaveObjects.ContainsKey(id)) {
                    return serializedSaveObjects[id];
                }
                
                debug.LogWarning($"No serializedSaveObject found with id {id} in scene {sceneName}");
                return null;
            }
        }
        
        public Either<DynamicObject, DynamicObjectSave> GetDynamicObject(string id) {
            if (sceneIsLoaded) {
                if (dynamicObjects.ContainsKey(id)) {
                    if (dynamicObjects[id] == null) {
                        dynamicObjects.Remove(id);
                        return null;
                    }

                    return dynamicObjects[id];
                }

                Debug.LogError($"No dynamicObject found with id {id} in scene {sceneName}");
                return null;
            }
            else {
                if (serializedDynamicObjects.ContainsKey(id)) {
                    return serializedDynamicObjects[id];
                }
                
                Debug.LogError($"No serializedDynamicObject found with id {id} in scene {sceneName}");
                return null;
            }
        }
        
        /// <summary>
        /// Register a DynamicObject, if it's not already registered.
        /// This should happen on Awake for each DynamicObject when its scene is loaded, or when it is instantiated.
        /// </summary>
        /// <param name="dynamicObject">DynamicObject to be registered</param>
        /// <returns>True if the object was registered, false otherwise</returns>
        public bool RegisterDynamicObject(DynamicObject dynamicObject) {
            string id = dynamicObject.ID;
            if (dynamicObjects.ContainsKey(id)) {
                debug.LogWarning($"dynamicObjects already contains key {id}: {dynamicObjects[id]}");
                return false;
            }

            SaveManager.sceneLookupForId[id] = dynamicObject.gameObject.scene.name;
            dynamicObjects.Add(id, dynamicObject);
            return true;
        }
        
        /// <summary>
        /// Unregister a DynamicObject, if it is currently registered.
        /// This should happen when a DynamicObject is explicitly destroyed (not just from a scene change or application quit).
        /// </summary>
        /// <param name="id">ID of the DynamicObject to unregister</param>
        /// <returns>True if the object was unregistered, false otherwise</returns>
        public bool UnregisterDynamicObject(string id) {
            if (!dynamicObjects.ContainsKey(id)) {
                debug.LogWarning($"Attempting to remove DynamicObject with id: {id}, but no entry for that id exists.");
                return false;
            }

            SaveManager.sceneLookupForId.Remove(id);
            dynamicObjects.Remove(id);
            return true;
        }
        
        static SerializableSaveObject GetSaveObject(ISaveableObject saveableObject) {
            try {
                return saveableObject.GetSaveObject();
            }
            catch (Exception e) {
                string ID = "<Unknown ID>";
                try {
                    ID = saveableObject.ID;
                }
                catch (Exception) { } // Sometimes this fails for some reason, we don't really care as it's just for logging

                Debug.LogError($"Could not get serialized save object for: {ID}, {saveableObject}. Details:\n{e}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a SaveFile for this scene and returns it
        /// </summary>
        /// <returns>SaveFileForScene for this scene</returns>
        public SaveDataForScene GetSaveFileForScene() {
            Dictionary<string, SerializableSaveObject> objectsToSave;
            Dictionary<string, DynamicObjectSave> dynamicObjectsToSave;
            if (sceneIsLoaded) {
                objectsToSave = saveableObjects
                    .Where(kv => !kv.Value.SkipSave)
                    .ToDictionary(kv => kv.Key, kv => GetSaveObject(kv.Value))
                    .Where(kv => kv.Value != null)
                    .ToDictionary();
                dynamicObjectsToSave = dynamicObjects
                    .Where(kv => !kv.Value.SkipSave)
                    .ToDictionary(kv => kv.Key, kv => kv.Value.GetSaveObject() as DynamicObjectSave)
                    .Where(kv => kv.Value != null)
                    .ToDictionary();
            }
            else {
                objectsToSave = serializedSaveObjects;
                dynamicObjectsToSave = serializedDynamicObjects;
            }

            return new SaveDataForScene(sceneName, objectsToSave, dynamicObjectsToSave);
        }
        
        /// <summary>
        /// Gets a SaveFile, if it exists, for this scene from the given save file
        /// </summary>
        /// <param name="saveFileName">Name of the save file to read from</param>
        /// <returns>A SaveFileForScene read from disk for this scene if one exists, null otherwise</returns>
        public SaveDataForScene GetSaveFromDisk(string saveFileName) {
            string directoryPath = $"{Application.persistentDataPath}/Saves/{saveFileName}";
            string saveFile = $"{directoryPath}/{sceneName}.save";

            if (Directory.Exists(directoryPath) && File.Exists(saveFile)) {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(saveFile, FileMode.Open);
                SaveDataForScene save = (SaveDataForScene)bf.Deserialize(file);
                file.Close();

                return save;
            }

            return null;
        }

        /// <summary>
        /// Given a SaveFile for this scene, will either get (or create) all DynamicObjects associated with the
        /// currentSaveFile.serializedDynamicObjects and then load their state from the save file for a loaded scene,
        /// or, will copy the serializedDynamicObjects dictionary to this SaveManager for an unloaded scene.
        /// </summary>
        /// <param name="currentSaveData">Save file to load state from</param>
        public void LoadDynamicObjectsStateFromSaveFile(SaveDataForScene currentSaveData) {
            if (currentSaveData?.serializedDynamicObjects != null) {
                if (sceneIsLoaded) {
                    foreach (var id in currentSaveData.serializedDynamicObjects.Keys) {
                        DynamicObjectSave dynamicObjectSave = currentSaveData.serializedDynamicObjects[id];
                        DynamicObject dynamicObject = GetOrCreateDynamicObject(id, dynamicObjectSave);

                        dynamicObjectSave.LoadSave(dynamicObject);
                    }
                }
                else {
                    serializedDynamicObjects = currentSaveData.serializedDynamicObjects;
                }
            }
        }

        /// <summary>
        /// Given a SaveFile for this scene, will get all SaveableObjects associated with the
        /// currentSaveFile.serializedSaveObjects and then load their state from the save file for a loaded scene,
        /// or, will copy the serializedSaveObjects dictionary to this SaveManager for an unloaded scene.
        /// </summary>
        /// <param name="currentSaveData">Save file to load state from</param>
        public void LoadSaveableObjectsStateFromSaveFile(SaveDataForScene currentSaveData) {
            if (currentSaveData?.serializedSaveObjects != null) {
                if (sceneIsLoaded) {
                    foreach (var id in currentSaveData.serializedSaveObjects.Keys) {
                        SaveableObject saveableObject = GetSaveableObjectOrNull(id);
                        if (saveableObject == null) {
                            debug.LogWarning($"{id} not found in scene {sceneName}");
                            continue;
                        }

                        try {
                            saveableObject.RestoreStateFromSave(currentSaveData.serializedSaveObjects[id]);
                        }
                        catch (Exception e) {
                            Debug.LogError($"Error loading ID {id}, cause: " + e);
                        }
                    }
                }
                else {
                    serializedSaveObjects = currentSaveData.serializedSaveObjects;
                }

                debug.Log($"Loaded {sceneName} from save");
            }
        }

        /// <summary>
        /// Called before a scene is unloaded.
        /// Transfers all state from saveableObjects && dynamicObjects to serializedSaveableObjects &&
        /// serializedDynamicObjects, respectively, then clears saveableObjects && dynamicObjects dictionaries.
        /// </summary>
        public void SerializeStateForScene() {
            if (!sceneIsLoaded) {
                Debug.LogError($"Trying to save state for already unloaded scene {sceneName}");
                return;
            }
            
            serializedSaveObjects.Clear();
            foreach (var saveableObjectKV in saveableObjects) {
                string id = saveableObjectKV.Key;
                SaveableObject saveableObject = saveableObjectKV.Value;
                if (saveableObject.SkipSave) continue;
                
                try {
                    serializedSaveObjects[id] = saveableObject.GetSaveObject();
                }
                catch (Exception e) {
                    Debug.LogError($"Error while trying to get save object for {saveableObject}, ID {id} in scene {sceneName}:\n{e}");
                }
            }
            saveableObjects.Clear();
            
            serializedDynamicObjects.Clear();
            foreach (DynamicObject dynamicObject in dynamicObjects.Values) {
                try {
                    serializedDynamicObjects[dynamicObject.ID] = dynamicObject.GetSaveObject() as DynamicObjectSave;
                }
                catch (Exception e) {
                    Debug.LogError($"Error serializing {dynamicObject.ID}, cause: {e.StackTrace}");
                }
            }
            dynamicObjects.Clear();
        }

        /// <summary>
        /// Called after a scene is loaded, after RestoreDynamicObjectStateForScene has ran for all scenes.
        /// Transfers all state from serializableSaveObjects to saveableObjects, loads the state for each object,
        /// then clears serializableSaveObjects dictionary
        /// </summary>
        public void RestoreSaveableObjectStateForScene() {
            if (!sceneIsLoaded) {
                Debug.LogError($"Trying to restore SaveableObject state for unloaded scene {sceneName}");
                return;
            }
            
            foreach (string id in serializedSaveObjects.Keys) {
                try {
                    SaveableObject saveableObject = GetSaveableObjectOrNull(id);
                    if (saveableObject == null) {
                        Debug.LogError($"{id} not found in scene {sceneName}");
                        continue;
                    }

                    saveableObject.RestoreStateFromSave(serializedSaveObjects[id]);
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
            if (!sceneIsLoaded) {
                Debug.LogError($"Trying to restore DynamicObject state for unloaded scene {sceneName}");
                return;
            }
            
            foreach (string id in serializedDynamicObjects.Keys) {
                try {
                    DynamicObject dynamicObject = GetOrCreateDynamicObject(id, serializedDynamicObjects[id]);
                    dynamicObject.RestoreStateFromSave(serializedDynamicObjects[id]);
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
            if (sceneIsLoaded) {
                saveableObjects = Resources.FindObjectsOfTypeAll<SaveableObject>()
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
        public void ClearAllState() {
            if (sceneIsLoaded) {
                saveableObjects.Clear();
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
            foreach (var dynamicObj in dynamicObjects.Values) {
                if (dynamicObj != null && dynamicObj.gameObject != null) {
                    // Don't destroy objects that are just part of the scene
                    if (dynamicObj.instantiatedAtRuntime) {
                        Object.Destroy(dynamicObj.gameObject);
                    }
                }
            }
            dynamicObjects.Clear();
        }
        
        // Sometimes SaveableObjects don't register themselves by the time they are referenced by something else
        // (for example, if they start deactivated), and we need to find them by ID.
        // This is inefficient and should be avoided when possible.
        T LookForMissingSaveableObject<T>(string id) where T : MonoBehaviour,  ISaveableObject {
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

            List<T> matches = Resources.FindObjectsOfTypeAll<T>()
                .Where(s => HasValidId(s) && s.ID == id && IsObjectInThisScene(s))
                .ToList();

            if (matches.Count == 0) {
                debug.LogWarning($"No saveableObject with id {id} found anywhere in scene {sceneName}", true);
                return null;
            }

            if (matches.Count > 1) {
                Debug.LogError($"Multiple matches for id {id} found:\n{string.Join("\n", matches)}");
                return null;
            }
            if (matches[0].gameObject.scene.name != sceneName) {
                Debug.LogError($"Match found for {id}, but it's in scene {matches[0].gameObject.scene.name} rather than expected {sceneName}");
                return null;
            }
            
            return matches[0];
        }
        
        bool IsObjectInThisScene(MonoBehaviour obj) {
            return obj != null && obj.gameObject != null && obj.gameObject.scene != null && obj.gameObject.scene.name == sceneName;
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
        SaveableObject GetSaveableObjectOrNull(string id) {
            return GetSaveableObject(id)?.Match(
                result => result,
                other => null
            );
        }
    }
}