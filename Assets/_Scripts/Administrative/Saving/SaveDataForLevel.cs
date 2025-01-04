using System;
using System.Collections.Generic;
using LevelManagement;

namespace Saving {
    [Serializable]
    public class SaveDataForLevel {
        public Levels level;
        public Dictionary<string, SaveObject> serializedSaveObjects;
        public Dictionary<string, DynamicObject.DynamicObjectSave> serializedDynamicObjects;

        public SaveDataForLevel(Levels level, Dictionary<string, SaveObject> serializedSaveObjects, Dictionary<string, DynamicObject.DynamicObjectSave> serializedDynamicObjects) {
            this.level = level;
            this.serializedSaveObjects = serializedSaveObjects;
            this.serializedDynamicObjects = serializedDynamicObjects;
        }
    }
}