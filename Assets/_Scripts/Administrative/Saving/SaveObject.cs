using System;
using System.Linq;
using LevelManagement;
using SerializableClasses;
using UnityEngine;
using SuperspectiveUtils;

namespace Saving {
    [Serializable]
    public class SaveObject {
        public string ID;
        // AssociationID is the UUID component of ID, or, if there is none, just ID
        // It is used to find all associated objects to be deregistered if a save object is Destroyed while unloaded
        public string associationID;
        public Levels level;
        public bool isGameObjectActive;
        public bool isScriptEnabled;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        // Scale always handled by GrowShrinkObject

        protected SaveObject() { }

        // Must be overridden by inheriting classes
        public SaveObject(SuperspectiveObject superspectiveObject) {
            try {
                this.ID = superspectiveObject.ID;
                this.associationID = superspectiveObject.AssociationID;
                this.level = superspectiveObject.Level;
                this.isGameObjectActive = superspectiveObject.gameObject.activeSelf;
                this.isScriptEnabled = superspectiveObject.enabled;
                this.position = superspectiveObject.transform.position;
                this.rotation = superspectiveObject.transform.rotation;
            }
            catch (Exception e) {
                if (superspectiveObject != null) {
                    new DebugLogger(superspectiveObject).LogError(e);
                }
                else {
                    Debug.LogError($"Script is null! Can't save this object.");
                }
            }
        }

        /// <summary>
        /// Unregisters this save object while in an unloaded scene.
        /// </summary>
        public virtual void Destroy() {
            SaveManager.Unregister(ID);
        }
    }

    [Serializable]
    public abstract class SaveObject<T> : SaveObject where T : SuperspectiveObject {
        protected SaveObject(T saveableObject) : base(saveableObject) { }
    }
}
