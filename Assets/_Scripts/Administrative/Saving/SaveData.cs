using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using LevelManagement;
using UnityEngine;

namespace Saving {
    [Serializable]
    public class SaveData {
        public SaveDataForScene managerScene;
        public Dictionary<string, SaveDataForScene> scenes;
        public DynamicObjectManager.DynamicObjectsSaveFile dynamicObjects;

        public static SaveData CreateSaveDataFromCurrentState() {
            // We force the ManagerScene to find all SaveableObjects because of the ExecuteInEditMode scripts in this scene
            SaveManagerForScene managerSceneSaveManager = SaveManager.saveManagers[LevelManager.ManagerScene];
            managerSceneSaveManager.ForceGetAllSaveableObjectsInScene();
            return new SaveData {
                managerScene = SaveManager.saveManagers[LevelManager.ManagerScene].GetSaveFileForScene(),
                scenes = SaveManager.saveManagers
                    .Where(kv => kv.Key != LevelManager.ManagerScene)
                    .ToDictionary(kv => kv.Key, kv => kv.Value.GetSaveFileForScene()),
                dynamicObjects = DynamicObjectManager.GetDynamicObjectRecordsSave()
            };
        }

        public static SaveData RetrieveSaveDataFromDisk(string saveFileName) {
            string saveFile = $"{SaveFileUtils.SaveDataPath}/{saveFileName}.save";

            if (Directory.Exists(SaveFileUtils.SaveDataPath) && File.Exists(saveFile)) {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(saveFile, FileMode.Open);
                SaveData save = (SaveData)bf.Deserialize(file);
                file.Close();

                return save;
            }

            return null;
        }

        public SaveData WriteToDisk(string saveFileName) {
            string saveDataFile = $"{SaveFileUtils.SaveDataPath}/{saveFileName}.save";
            
            BinaryFormatter bf = new BinaryFormatter();
            Directory.CreateDirectory(SaveFileUtils.SaveDataPath);
            FileStream file = File.Create(saveDataFile);
            bf.Serialize(file, this);
            file.Close();

            return this;
        }
    }
}