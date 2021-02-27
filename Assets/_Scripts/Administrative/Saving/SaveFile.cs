using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using LevelManagement;
using UnityEngine;

namespace Saving {
    [Serializable]
    public class SaveFile {
        public string saveFileName;
        public SaveFileForScene managerScene;
        public Dictionary<string, SaveFileForScene> scenes;
        public DynamicObjectManager.DynamicObjectsSaveFile dynamicObjects;

        static string SavePath => $"{Application.persistentDataPath}/Saves";

        public static SaveFile CreateSaveFileFromCurrentState(string saveFileName) {
            // We force the ManagerScene to find all SaveableObjects because of the ExecuteInEditMode scripts in this scene
            SaveManagerForScene managerSceneSaveManager = SaveManager.saveManagers[LevelManager.ManagerScene];
            managerSceneSaveManager.ForceGetAllSaveableObjectsInScene();
            return new SaveFile {
                saveFileName = saveFileName,
                managerScene = SaveManager.saveManagers[LevelManager.ManagerScene].GetSaveFileForScene(),
                scenes = SaveManager.saveManagers
                    .Where(kv => kv.Key != LevelManager.ManagerScene)
                    .ToDictionary(kv => kv.Key, kv => kv.Value.GetSaveFileForScene()),
                dynamicObjects = DynamicObjectManager.GetDynamicObjectRecordsSave()
            };
        }

        public static SaveFile RetrieveSaveFileFromDisk(string saveFileName) {
            string saveFile = $"{SavePath}/{saveFileName}.save";

            if (Directory.Exists(SavePath) && File.Exists(saveFile)) {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(saveFile, FileMode.Open);
                SaveFile save = (SaveFile)bf.Deserialize(file);
                file.Close();

                return save;
            }

            return null;
        }

        public void WriteToDisk() {
            string saveFile = $"{SavePath}/{saveFileName}.save";
            
            BinaryFormatter bf = new BinaryFormatter();
            Directory.CreateDirectory(SavePath);
            FileStream file = File.Create(saveFile);
            bf.Serialize(file, this);
            file.Close();
        }
    }
}