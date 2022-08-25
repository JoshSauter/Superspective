using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Saving {
    [Serializable]
    public class SaveDataForScene {
        public string sceneName;
        public Dictionary<string, SerializableSaveObject> serializedSaveObjects;
        public Dictionary<string, DynamicObject.DynamicObjectSave> serializedDynamicObjects;

        public SaveDataForScene(string sceneName, Dictionary<string, SerializableSaveObject> serializedSaveObjects, Dictionary<string, DynamicObject.DynamicObjectSave> serializedDynamicObjects) {
            this.sceneName = sceneName;
            this.serializedSaveObjects = serializedSaveObjects;
            this.serializedDynamicObjects = serializedDynamicObjects;
        }
    }
}