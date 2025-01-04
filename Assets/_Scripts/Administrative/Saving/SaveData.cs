using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using LevelManagement;
using UnityEngine.Serialization;

namespace Saving {

    [Serializable]
    public struct SaveData {
        [FormerlySerializedAs("managerScene")]
        public SaveDataForLevel managerLevel;
        public Dictionary<Levels, SaveDataForLevel> levels;
        public DynamicObjectManager.DynamicObjectsSaveFile dynamicObjects;

        public static SaveData CreateSaveDataFromCurrentState() {
            // We force the ManagerScene to find all SaveableObjects because of the ExecuteInEditMode scripts in this scene
            SaveManager.SaveManagerForLevel managerLevelSaveManager = SaveManager.saveManagers[Levels.ManagerScene];
            managerLevelSaveManager.ForceGetAllSaveableObjectsInScene();
            return new SaveData {
                managerLevel = SaveManager.saveManagers[Levels.ManagerScene].GetSaveFileForLevel(),
                levels = SaveManager.saveManagers
                    .Where(kv => kv.Key != Levels.ManagerScene)
                    .ToDictionary(kv => kv.Key, kv => kv.Value.GetSaveFileForLevel()),
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

            return default;
        }
    }

}
