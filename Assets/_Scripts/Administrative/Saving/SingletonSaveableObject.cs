using System;
using System.Collections;
using EpitaphUtils;
using LevelManagement;
using NaughtyAttributes;
using Saving;
using UnityEngine;

namespace Saving {
    /// <summary>
    /// A SaveableObject has a unique identifier, and methods for getting, saving, and loading some Serializable save class S
    /// Example declaration: PlayerLook : SaveableObject<PlayerLook, PlayerLookSave>
    /// </summary>
    /// <typeparam name="T">Type of class whose state should be saved</typeparam>
    /// <typeparam name="S">Type of the serializable Save class</typeparam>
    public abstract class SingletonSaveableObject<T, S> : SaveableObject
        where S : SerializableSaveObject<T>
        where T : SaveableObject {
        static T _instance = null;

        public static T instance {
            get {
                // If the singleton reference doens't yet exist
                if (_instance == null) {
                    // Search for a matching singleton that exists
                    var matches = FindObjectsOfType<T>();

                    if (matches.Length > 0) {
                        _instance = matches[0];
                        if (matches.Length > 1) {
                            Debug.LogError("There is more than one " + typeof(T).Name + " in the scene.");
                        }
                    }

                    if (_instance == null) {
                        // Debug.LogError("No Object of type " + typeof(T).Name + " exists. Make sure you add one to the scene.");
                    }
                }

                return _instance;
            }
        }

        public override SerializableSaveObject GetSaveObject() {
            return (S)Activator.CreateInstance(typeof(S), new object[] { this });
        }

        public override void RestoreStateFromSave(SerializableSaveObject savedObject) {
            S save = savedObject as S;

            save?.LoadSave(this as T);
        }

        public override bool SkipSave { get; set; }
    }
}