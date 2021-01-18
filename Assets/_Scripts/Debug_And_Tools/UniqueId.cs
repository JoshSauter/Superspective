using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class UniqueId : MonoBehaviour, ISerializationCallbackReceiver {
    [ReadOnly]
    public string uniqueId;

    void Awake() {
        // Don't generate an ID for objects without scenes (prefabs)
        bool notInitialized = string.IsNullOrEmpty(uniqueId);
        if (notInitialized && HasScene) {
            //Debug.LogWarning($"{gameObject.name} in {gameObject.scene.name} has an uninitialized id. Creating one now.");
            Guid guid = Guid.NewGuid();
            uniqueId = guid.ToString();
		}
        bool idAlreadyTaken = !string.IsNullOrEmpty(uniqueId) && !IsUnique(uniqueId);
        // If we are playing and the id is non-null & already taken, destroy this instance
        if (Application.isPlaying && idAlreadyTaken) {
            Debug.LogWarning($"ID {uniqueId} already taken, {gameObject.name} in {gameObject.scene.name} self-destructing.");
            Destroy(gameObject);
            return;
        }
    }

	public void OnAfterDeserialize() { }

	public void OnBeforeSerialize() {
        bool idAlreadyTaken = !string.IsNullOrEmpty(uniqueId) && !IsUnique(uniqueId);

        // Don't generate an ID for objects without scenes (prefabs)
        // Generate a unique ID, defaults to an empty string if nothing has been serialized yet
        if (HasScene && (string.IsNullOrEmpty(uniqueId) || idAlreadyTaken)) {
            Guid guid = Guid.NewGuid();
            uniqueId = guid.ToString();
        }
    }

    // Last check is for components on nested prefabs edited in prefab editor. It will list the gameObject.scene as the prefab root name
    bool HasScene => gameObject != null && gameObject.scene != null && gameObject.scene.name != null && gameObject.scene.name != "" && gameObject.scene.name != transform.root.name;
    static bool IsUnique(string id) {
        return Resources.FindObjectsOfTypeAll<UniqueId>().Count(x => x.uniqueId == id) == 1;
    }
}
