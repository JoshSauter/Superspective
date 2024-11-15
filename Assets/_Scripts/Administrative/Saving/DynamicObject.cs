﻿using System;
using System.Collections;
using SuperspectiveUtils;
using LevelManagement;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Saving {
    // Dynamic objects are objects that may be created or destroyed at runtime
    // They must provide what type of object they are so they can be instantiated from a prefab if it does not exist
	// They are loaded before SaveableObjects so that the instance exists for other Component loads
    public class DynamicObject : MonoBehaviour, ISaveableObject {
	    [SerializeField]
	    protected bool DEBUG => ID.StartsWith("6ef");
	    private DebugLogger _debug;
	    DebugLogger debug => _debug ??= new DebugLogger(gameObject, () => DEBUG);

	    UniqueId _id;
		public UniqueId id {
			get {
				if (this == null || this.gameObject == null) return null;
				
				if (_id == null) {
					_id = GetComponent<UniqueId>();
				}
				if (_id == null) {
					_id = gameObject.AddComponent<UniqueId>();
				}
				return _id;
			}
		}
		public string prefabPath;

		public bool SkipSave { get; set; }
		public string ID {
			get {
				if (id != null) {
					LastKnownID = id.uniqueId;
					return id.uniqueId;
				}
				else return LastKnownID;
			}
		}
		
		[FormerlySerializedAs("wasInstantiatedAtRuntime")]
		[ReadOnly]
		public bool instantiatedAtRuntime = true;
		private string LastKnownID = "";
		public bool isGlobal = true;
		bool hasRegistered = false;

		string SceneName => gameObject.scene.name;
		SaveManagerForScene SaveForScene => SaveManager.GetOrCreateSaveManagerForScene(SceneName);

		PickupObject pickup;

		void OnValidate() {
			bool noScene = gameObject.scene == null || string.IsNullOrEmpty(SceneName);
			if (noScene && string.IsNullOrEmpty(prefabPath)) {
#if UNITY_EDITOR
				prefabPath = AssetDatabase.GetAssetPath(this)
					// Strip prefix and suffix to get the Resources-relative path
					.Replace("Assets/Resources/", "")
					.Replace(".prefab", "");
#endif
			}

			if (!Application.isPlaying) {
				instantiatedAtRuntime = noScene;
			}
		}
		
		void Awake() {
			if (prefabPath == "") {
				debug.LogError($"{gameObject.name}: No prefab for this DynamicObject");
			}
			pickup = GetComponent<PickupObject>();

			StartCoroutine(SubscribeToEventsOnceLevelManagerExists());
		}

		IEnumerator SubscribeToEventsOnceLevelManagerExists() {
			while (LevelManager.instance == null) {
				yield return null;
			}
			
			LevelManager.instance.BeforeSceneRestoreState += RegisterOnLevelChangeEvents;
			LevelManager.instance.BeforeSceneSerializeState += RegisterOnLevelChangeEvents;
		}

		IEnumerator Start() {
			yield return new WaitUntil(() => !LevelManager.instance.IsCurrentlySwitchingScenes);
			
			Register();
		}
		
		void RegisterOnLevelChangeEvents(string scene) {
			if (hasRegistered) return;
            
			if (scene == SceneName) {
				Register();
				LevelManager.instance.BeforeSceneRestoreState -= RegisterOnLevelChangeEvents;
				LevelManager.instance.BeforeSceneSerializeState -= RegisterOnLevelChangeEvents;
			}
		}

		public void Register() {
			if (this == null || hasRegistered) {
				return;
			}

			RegistrationStatus dynamicObjectRegistrationStatus = DynamicObjectManager.RegisterDynamicObject(this, SceneName);
			bool dynamicObjectManagerRegistered = dynamicObjectRegistrationStatus.IsSuccess() || (SaveManager.isCurrentlyLoadingSave && !dynamicObjectRegistrationStatus.IsAlreadyDestroyed());
			if (dynamicObjectManagerRegistered) {
				SaveForScene.RegisterDynamicObject(this);
				debug.Log($"Registration succeeded for {ID}, scene {SceneName}");
				hasRegistered = true;
			}
			else {
				debug.LogError($"Registration failed. DynamicObjectManager registration result: {dynamicObjectManagerRegistered}", true);
				Destroy(gameObject);
			}
		}
		
		public void RegisterDynamicObjectUponCreation(string idOfCreatedObj) {
			if (this != null && id.uniqueId == idOfCreatedObj) {
				Register();
			}
		}

		// DynamicObject.Destroy should be used instead of Object.Destroy to ensure proper record keeping
		// for when the object is explicitly destroyed (not just from a scene change, etc)
		public void Destroy() {
			bool unregistered = SaveForScene.UnregisterDynamicObject(ID);
			bool markedAsDestroyed = DynamicObjectManager.MarkDynamicObjectAsDestroyed(this, SceneName);

			if (unregistered && markedAsDestroyed) {
				debug.Log($"Marked as Destroyed in scene {SceneName}", true);
			}
			else {
				debug.LogError($"Failed in Destroy. Unregistration successful: {unregistered}, Marked as destroyed successful: {markedAsDestroyed}", true);
			}
			
			// Unregister any other SaveableObject scripts that may have existed on this object
			SaveForScene.UnregisterAllAssociatedObjects(ID);

			Destroy(gameObject);
		}

		void OnDestroy() {
			if (LevelManager.instance != null) {
				LevelManager.instance.BeforeSceneRestoreState -= RegisterOnLevelChangeEvents;
				LevelManager.instance.BeforeSceneSerializeState -= RegisterOnLevelChangeEvents;
			}
		}

		void Update() {
			if (pickup != null && pickup.isHeld) {
				Scene activeScene = SceneManager.GetSceneByName(LevelManager.instance.activeSceneName);
				if (gameObject.scene != activeScene) {
					ChangeScene(activeScene);
				}
			}
		}

		// GlobalObjects are moved between scenes as they hit objects from various scenes
		void OnCollisionEnter(Collision collision) {
			if (pickup != null && pickup.isHeld) return;
			
			if (isGlobal) {
				Scene sceneOfContact = collision.collider.gameObject.scene;
				if (sceneOfContact.name == LevelManager.ManagerScene) {
					return;
				}
				
				if (gameObject.scene != sceneOfContact) {
					if (transform.parent != null) {
						transform.parent = null;
					}
					ChangeScene(sceneOfContact);
				}
			}
		}

		public void ChangeScene(Scene newScene) {
			if (isGlobal && gameObject.scene != newScene) {
				string oldScene = SceneName;
				gameObject.transform.SetParent(null);
				// Move the GameObject to the new scene
				SceneManager.MoveGameObjectToScene(gameObject, newScene);
				// Update the record of the DynamicObject in DynamicObjectManager
				DynamicObjectManager.ChangeDynamicObjectScene(this, oldScene, newScene.name);
			}
		}

		[Serializable]
		public class DynamicObjectSave : SerializableSaveObject<DynamicObject> {
			public string prefabPath;
			public bool isGlobal;
			public string scene;
			public bool active;

			public DynamicObjectSave(DynamicObject obj) : base(obj) {
				this.prefabPath = obj.prefabPath;
				this.isGlobal = obj.isGlobal;
				this.scene = obj.SceneName;
				this.active = obj.gameObject.activeSelf;
			}

			public override void LoadSave(DynamicObject obj) {
				obj.prefabPath = this.prefabPath;
				obj.isGlobal = this.isGlobal;
				if (!string.IsNullOrEmpty(scene)) {
					if (obj.transform.parent != null) {
						//obj.transform.SetParent(null);
					}
					obj.ChangeScene(SceneManager.GetSceneByName(scene));
				}
				obj.gameObject.SetActive(this.active);
			}

			// Similar to DynamicObject.Destroy but for objects in unloaded scenes
			public override void Destroy() {
				base.Destroy();
				DynamicObjectManager.MarkDynamicObjectAsDestroyed(this, scene);
			}
		}

		public SerializableSaveObject GetSaveObject() {
			return new DynamicObjectSave(this);
		}

		public void RestoreStateFromSave(SerializableSaveObject savedObject) {
			(savedObject as DynamicObjectSave)?.LoadSave(this);
		}
	}
}
