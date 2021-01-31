using EpitaphUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Saving.DynamicObject;

namespace Saving {
    public class SaveManagerForScene : MonoBehaviour {
        public bool DEBUG = false;
        public DebugLogger debug;
        string sceneName => gameObject.scene.name;

        public Dictionary<string, SaveableObject> saveableObjects = new Dictionary<string, SaveableObject>();

        public void InitializeSaveableObjectsDict() {
            //var x = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            //var y = x.Where(s => IsObjectInThisScene(s));
            //var z = y.OfType<SaveableObject>().ToList();
            //var w = z.Where(s => !(s is DynamicObject) && s.ID != null && s.ID != "").ToList();
            //var r = w.Select(s => s.ID).ToList();
            saveableObjects = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .Where(s => IsObjectInThisScene(s))
                .OfType<SaveableObject>()
                // Dynamic objects are loaded separately
                .Where(s => !(s is DynamicObject) && s.ID != null && s.ID != "")
                .Where(s => !s.SkipSave)
                .ToDictionary(s => s.ID);
		}

        bool IsObjectInThisScene(MonoBehaviour obj) {
            return obj != null && obj.gameObject != null && obj.gameObject.scene != null && obj.gameObject.scene == gameObject.scene;
        }

        [Serializable]
        public class SaveFileForScene {
            public string saveFileName;
            public string sceneName;
            public Dictionary<string, object> serializedSaveObjects;

            string savePath => $"{Application.persistentDataPath}/Saves/{saveFileName}";
            string saveFile => $"{savePath}/{sceneName}.save";

            SaveFileForScene(string saveFileName, string sceneName, Dictionary<string, SaveableObject> objs) {
                this.saveFileName = saveFileName;
                this.sceneName = sceneName;
                serializedSaveObjects = new Dictionary<string, object>();
                foreach (var id in objs.Keys) {
                    try {
                        serializedSaveObjects.Add(id, objs[id].GetSaveObject());
                    }
                    catch (Exception e) {
                        Debug.LogError($"Could not get serialized save object for: {id}, {objs[id]}. Details:\n{e.ToString()}");
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

            public static SaveFileForScene CreateSave(string saveFileName, string sceneName, Dictionary<string, SaveableObject> objs) {
                return new SaveFileForScene(saveFileName, sceneName, objs);
			}
        }

        void Awake() {
            debug = new DebugLogger(gameObject, () => DEBUG);
            InitializeSaveableObjectsDict();
        }

        void OnDisable() {
            SaveManager.RemoveSaveManagerForScene(sceneName);
        }

        public void SaveScene(string saveFileName) {
            InitializeSaveableObjectsDict();
            SaveFileForScene currentSaveFile = SaveFileForScene.CreateSave(saveFileName, sceneName, saveableObjects);

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
            else {
                return null;
            }
        }

        public void LoadSceneFromSaveFile(SaveFileForScene currentSaveFile) {
            InitializeSaveableObjectsDict();

            if (currentSaveFile != null) {
                foreach (var id in currentSaveFile.serializedSaveObjects.Keys) {
                    if (!saveableObjects.ContainsKey(id)) {
                        Debug.LogWarning($"{id} not found in scene {sceneName}");
                        continue;
					}
                    SaveableObject saveableObject = saveableObjects[id];
                    saveableObject.LoadFromSavedObject(currentSaveFile.serializedSaveObjects[id]);
                }

                debug.Log($"Loaded {sceneName} from {currentSaveFile.saveFileName}");
            }
        }
    }
}