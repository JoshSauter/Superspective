using System;
using UnityEngine;

namespace Saving {
    /// <summary>
    /// A SuperspectiveObject has a unique identifier, and methods for getting, saving, and loading some Serializable save class S
    /// Example declaration: PlayerLook : SaveableObject<PlayerLook, PlayerLookSave>
    /// </summary>
    /// <typeparam name="T">Type of SuperspectiveObject class whose state should be saved</typeparam>
    /// <typeparam name="S">Type of the serializable SaveObject class</typeparam>
    public abstract class SingletonSuperspectiveObject<T, S> : SuperspectiveObject<T, S>
        where T : SuperspectiveObject
        where S : SaveObject<T> {
        
        public override string ID => typeof(T).Name;
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
    }
}