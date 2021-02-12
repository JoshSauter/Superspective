using EpitaphUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Saving.DynamicObject;

namespace Saving {
    public class SaveManagerForScene : MonoBehaviour {
        public bool DEBUG = true;
        public DebugLogger debug;
        string sceneName => gameObject.scene.name;

        readonly Dictionary<string, ISaveableObject> saveableObjects = new Dictionary<string, ISaveableObject>();
        readonly Dictionary<string, DynamicObject> dynamicObjects = new Dictionary<string, DynamicObject>();

        [ShowNativeProperty]
        public int numSaveableObjects => saveableObjects.Count;

        [ShowNativeProperty]
        public int numDynamicObjects => dynamicObjects.Count;
        
#if UNITY_EDITOR
        public List<ISaveableObject> saveables = new List<ISaveableObject>();

        public List<DynamicObject> dynamics = new List<DynamicObject>();
#endif
        
        public bool RegisterSaveableObject(ISaveableObject saveableObject) {
            string id = saveableObject.ID;
            if (saveableObjects.ContainsKey(id)) {
                debug.LogWarning($"saveableObjects already contains key {id}: {saveableObjects[id]}");
                return false;
            }
            
            saveableObjects.Add(id, saveableObject);
#if UNITY_EDITOR
            saveables.Add(saveableObject);
#endif
            return true;
        }

        public bool UnregisterSaveableObject(ISaveableObject saveableObject) {
            string id = saveableObject.ID;
            if (!saveableObjects.ContainsKey(id)) {
                debug.LogWarning($"Attempting to remove saveableObject with id: {id}, but no entry for that id exists.");
                return false;
            }
            
            saveableObjects.Remove(id);
#if UNITY_EDITOR
            saveables.Remove(saveableObject);
#endif
            return true;
        }

        public ISaveableObject GetSaveableObject(string id) {
            if (saveableObjects.ContainsKey(id)) {
                if (saveableObjects[id] == null) {
                    saveableObjects.Remove(id);
                    return null;
                }
                return saveableObjects[id];
            }

            ISaveableObject missingSaveableObject = LookForMissingSaveableObject(id);
            if (missingSaveableObject != null) {
                missingSaveableObject.Register();
                return missingSaveableObject;
            }

            debug.LogError($"No saveableObject found with id {id}");
            return null;
        }

        ISaveableObject LookForMissingSaveableObject(string id) {
            debug.LogWarning($"Having to search for a missing id {id}");
            bool HasValidId(ISaveableObject obj) {
                try {
                    string s = obj.ID;

                    return !string.IsNullOrEmpty(s);
                }
                catch {
                    return false;
                }
            }
            
            List<MonoBehaviour> matches = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .OfType<ISaveableObject>()
                .Where(s => HasValidId(s) && s.ID == id)
                .OfType<MonoBehaviour>()
                .ToList();

            if (matches.Count == 0) {
                debug.LogError($"No saveableObject found anywhere with id {id}");
                return null;
            }

            if (matches.Count > 1) {
                debug.LogError($"Multiple matches for id {id} found:\n{string.Join("\n", matches)}");
                return null;
            }
            if (matches[0].gameObject.scene != gameObject.scene) {
                debug.LogError($"Match found for {id}, but it's in scene {matches[0].gameObject.scene.name} rather than expected {gameObject.scene.name}");
                return null;
            }
            
            return matches[0] as ISaveableObject;
        }

        public bool RegisterDynamicObject(DynamicObject dynamicObject) {
            string id = dynamicObject.ID;
            if (dynamicObjects.ContainsKey(id)) {
                debug.LogWarning($"dynamicObjects already contains key {id}: {dynamicObjects[id]}");
                return false;
            }

            dynamicObjects.Add(id, dynamicObject);
#if UNITY_EDITOR
            dynamics.Add(dynamicObject);
#endif
            return true;
        }
        
        public bool UnregisterDynamicObject(DynamicObject dynamicObject) {
            string id = dynamicObject.ID;
            if (!dynamicObjects.ContainsKey(id)) {
                debug.LogWarning($"Attempting to remove DynamicObject with id: {id}, but no entry for that id exists.");
                return false;
            }
            
            dynamicObjects.Remove(id);
#if UNITY_EDITOR
            dynamics.Remove(dynamicObject);
#endif
            return true;
        }

        DynamicObject GetOrCreateDynamicObject(string id, DynamicObjectSave dynamicObjectSave) {
            if (dynamicObjects.ContainsKey(id)) {
                return dynamicObjects[id];
            }

            DynamicObjectManager.CreateInstanceFromSavedInfo(id, dynamicObjectSave);
            if (!dynamicObjects.ContainsKey(id)) {
                Debug.LogError($"Newly created dynamic object {dynamicObjectSave}, id: {id} not present in dynamicObjects Dictionary");
                return null;
            }

            return dynamicObjects[id];
        }
        
        // Used before loading a different save file to clean state
        public void DeleteAllDynamicObjectsInScene() {
            foreach (var dynamicObj in dynamicObjects.Values) {
                Destroy(dynamicObj.gameObject);
            }
            dynamicObjects.Clear();
        }

        [Serializable]
        public class SaveFileForScene {
            public string saveFileName;
            public string sceneName;
            public Dictionary<string, object> serializedSaveObjects;
            public Dictionary<string, object> serializedDynamicObjects;

            string savePath => $"{Application.persistentDataPath}/Saves/{saveFileName}";
            string saveFile => $"{savePath}/{sceneName}.save";

            SaveFileForScene(string saveFileName, string sceneName, Dictionary<string, ISaveableObject> saveableObjects, Dictionary<string, DynamicObject> dynamicObjects) {
                this.saveFileName = saveFileName;
                this.sceneName = sceneName;
                serializedSaveObjects = new Dictionary<string, object>();
                foreach (var id in saveableObjects.Keys) {
                    try {
                        serializedSaveObjects.Add(id, saveableObjects[id].GetSaveObject());
                    }
                    catch (Exception e) {
                        Debug.LogError($"Could not get serialized save object for: {id}, {saveableObjects[id]}. Details:\n{e.ToString()}");
					}
				}

                serializedDynamicObjects = new Dictionary<string, object>();
                foreach (var id in dynamicObjects.Keys) {
                    try {
                        serializedDynamicObjects.Add(id, dynamicObjects[id].GetSaveObject());
                    }
                    catch (Exception e) {
                        Debug.LogError($"Could not get serialized dynamic object for: {id}, {dynamicObjects[id]}. Details:\n{e.ToString()}");
                    }
                }
            }

            public void SaveToDisk() {
                BinaryFormatter bf = new BinaryFormatter();
                Directory.CreateDirectory(savePath);
                FileStream file = File.Create(saveFile);
                bf.Serialize(file, this);
                file.Close();
            }

            public static SaveFileForScene CreateSave(string saveFileName, string sceneName, Dictionary<string, ISaveableObject> saveableObjects, Dictionary<string, DynamicObject> dynamicObjects) {
                return new SaveFileForScene(saveFileName, sceneName, saveableObjects, dynamicObjects);
			}
        }

        void Awake() {
            debug = new DebugLogger(gameObject, () => DEBUG);
        }

        void OnDisable() {
            SaveManager.RemoveSaveManagerForScene(sceneName);
        }

        public void SaveScene(string saveFileName) {
            Dictionary<string, ISaveableObject> objectsToSave = saveableObjects
                .Where(kv => !kv.Value.SkipSave)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            Dictionary<string, DynamicObject> dynamicObjectsToSave = dynamicObjects
                .Where(kv => !kv.Value.SkipSave)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            
            SaveFileForScene currentSaveFile = SaveFileForScene.CreateSave(saveFileName, sceneName, objectsToSave, dynamicObjectsToSave);

            currentSaveFile.SaveToDisk();

            debug.Log($"Saved {sceneName} to {saveFileName}");
        }

        public SaveFileForScene GetSaveFromDisk(string saveFileName) {
            string directoryPath = $"{Application.persistentDataPath}/Saves/{saveFileName}";
            string saveFile = $"{directoryPath}/{sceneName}.save";

            if (Directory.Exists(directoryPath) && File.Exists(saveFile)) {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(saveFile, FileMode.Open);
                SaveFileForScene save = (SaveFileForScene)bf.Deserialize(file);
                file.Close();

                return save;
            }

            return null;
        }

        public void LoadDynamicObjectsFromSaveFile(SaveFileForScene currentSaveFile) {
            if (currentSaveFile?.serializedDynamicObjects != null) {
                foreach (var id in currentSaveFile.serializedDynamicObjects.Keys) {
                    DynamicObjectSave dynamicObjectSave = currentSaveFile.serializedDynamicObjects[id] as DynamicObjectSave;
                    DynamicObject dynamicObject = GetOrCreateDynamicObject(id, dynamicObjectSave);

                    dynamicObjectSave.LoadSave(dynamicObject);
                }
            }
        }

        public void RestoreStateFromSaveFile(SaveFileForScene currentSaveFile) {
            if (currentSaveFile?.serializedSaveObjects != null) {
                foreach (var id in currentSaveFile.serializedSaveObjects.Keys) {
                    ISaveableObject saveableObject = GetSaveableObject(id);
                    if (saveableObject == null) {
                        Debug.LogWarning($"{id} not found in scene {sceneName}");
                        continue;
					}
                    saveableObject.RestoreStateFromSave(currentSaveFile.serializedSaveObjects[id]);
                }

                debug.Log($"Loaded {sceneName} from {currentSaveFile.saveFileName}");
            }
        }
    }
}