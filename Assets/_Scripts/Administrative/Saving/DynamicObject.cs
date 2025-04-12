using System;
using System.Collections;
using System.Linq;
using SuperspectiveUtils;
using LevelManagement;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
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
    public class DynamicObject : SuperspectiveObject<DynamicObject, DynamicObject.DynamicObjectSave> {
		public string prefabPath;
		
		[Button("Refresh Prefab Path")]
		private void RefreshPrefabPath() {
#if UNITY_EDITOR
			prefabPath = AssetDatabase.GetAssetPath(this)
				// Strip prefix and suffix to get the Resources-relative path
				.Replace("Assets/Resources/", "")
				.Replace(".prefab", "");
#endif
		}

		// Expose the UniqueId as a property so that its GUID can be set by the DynamicObjectManager when created
		public new UniqueId id => base.id;

		// Always save DynamicObjects
		public override bool SkipSave => false;
		
		[FormerlySerializedAs("wasInstantiatedAtRuntime")]
		[Unity.Collections.ReadOnly]
		public bool instantiatedAtRuntime = true;
		private string lastKnownID = "";
		public string ID {
			get {
				if (id == null) return lastKnownID;
				lastKnownID = id.uniqueId;
				return id.uniqueId;
			}
		}
        
		[FormerlySerializedAs("isGlobal")]
		public bool isAllowedToChangeScenes = true;

		protected override void OnValidate() {
			base.OnValidate();
			
			bool noScene = string.IsNullOrEmpty(SceneName);
			if (noScene && string.IsNullOrEmpty(prefabPath)) {
#if UNITY_EDITOR
				RefreshPrefabPath();
#endif
			}

			if (!Application.isPlaying) {
				instantiatedAtRuntime = noScene;
			}
		}

		protected override void Awake() {
			base.Awake();
			if (prefabPath == "") {
				debug.LogError($"{gameObject.name}: No prefab for this DynamicObject");
			}

			StartCoroutine(SubscribeToEventsOnceLevelManagerExists());
		}

		IEnumerator SubscribeToEventsOnceLevelManagerExists() {
			while (LevelManager.instance == null) {
				yield return null;
			}
			
			LevelManager.instance.BeforeSceneRestoreState += RegisterIfChangedToThisLevel;
			LevelManager.instance.BeforeSceneSerializeState += RegisterIfChangedToThisLevel;
		}
		
		void RegisterIfChangedToThisLevel(Levels levelSwitchedTo) {
			if (hasRegistered) return;
            
			if (levelSwitchedTo == Level) {
				Register();
				LevelManager.instance.BeforeSceneRestoreState -= RegisterIfChangedToThisLevel;
				LevelManager.instance.BeforeSceneSerializeState -= RegisterIfChangedToThisLevel;
			}
		}

		public override void Register() {
			if (!this || hasRegistered) {
				return;
			}
			base.Register();

			RegistrationStatus dynamicObjectRegistrationStatus = DynamicObjectManager.RegisterDynamicObject(this);
			bool dynamicObjectManagerRegistered = dynamicObjectRegistrationStatus.IsSuccess() || (SaveManager.isCurrentlyLoadingSave && !dynamicObjectRegistrationStatus.IsAlreadyDestroyed());
			if (dynamicObjectManagerRegistered) {
				SaveManager.RegisterDynamicObject(this);
				debug.Log($"Registration succeeded for {ID}, scene {SceneName}");
				hasRegistered = true;
			}
			else {
				debug.LogError($"Registration failed. DynamicObjectManager registration result: {dynamicObjectRegistrationStatus}", true);
				Destroy(gameObject);
			}
		}
		
		public void RegisterDynamicObjectUponCreation(string idOfCreatedObj) {
			if (this && id.uniqueId == idOfCreatedObj) {
				Register();
			}
		}

		// DynamicObject.Destroy should be used instead of Object.Destroy to ensure proper record keeping
		// for when the object is explicitly destroyed (not just from a scene change, etc)
		public override void Destroy() {
			Unregister();
			bool unregistered = SaveManager.UnregisterDynamicObject(ID);
			bool markedAsDestroyed = DynamicObjectManager.MarkDynamicObjectAsDestroyed(this);

			if (unregistered && markedAsDestroyed) {
				debug.Log($"Marked as Destroyed in scene {SceneName}", true);
			}
			else {
				debug.LogError($"Failed in Destroy. Unregistration successful: {unregistered}, Marked as destroyed successful: {markedAsDestroyed}", true);
			}
			
			// Unregister any other SaveableObject scripts that may have existed on this object
			SaveManager.GetAllAssociatedSuperspectiveObjects(AssociationID)
				// Don't destroy DynamicObjects, they are already being destroyed
				// Without this where filter, we are opening ourselves up to a stack overflow
				.Where(associatedObj => associatedObj is not DynamicObject)
				.ForEach(saveObj => saveObj.Destroy());

			Destroy(gameObject);
		}

		protected override void OnDestroy() {
			base.OnDestroy();
			if (LevelManager.instance != null) {
				LevelManager.instance.BeforeSceneRestoreState -= RegisterIfChangedToThisLevel;
				LevelManager.instance.BeforeSceneSerializeState -= RegisterIfChangedToThisLevel;
			}
		}

		// DynamicObjects are moved between scenes as they hit objects from various scenes
		void OnCollisionEnter(Collision collision) {
			if (isAllowedToChangeScenes) {
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

		public bool ChangeScene(Scene newScene) {
			if (isAllowedToChangeScenes && gameObject.scene != newScene) {
				if (!newScene.IsValid() || !newScene.isLoaded) {
					debug.LogError($"Can't move {gameObject.FullPath()} to {newScene.name}. Scene is not valid or not loaded.", true);
					return false;
				}
				
				Levels oldLevel = Level;
				gameObject.transform.SetParent(null);
				// Move the GameObject to the new scene
				SceneManager.MoveGameObjectToScene(gameObject, newScene);
				// Update the record of the DynamicObject in DynamicObjectManager
				DynamicObjectManager.ChangeDynamicObjectLevel(this, oldLevel, newScene.name.ToLevel());
				return true;
			}

			return false;
		}
		
		public override void LoadSave(DynamicObjectSave save) {
			prefabPath = save.prefabPath;
			isAllowedToChangeScenes = save.isGlobal;
			if (save.level.IsValid()) {
				ChangeScene(SceneManager.GetSceneByName(save.level.ToName()));
			}
		}

		[Serializable]
		public class DynamicObjectSave : SaveObject<DynamicObject> {
			public string prefabPath;
			public bool isGlobal;

			public DynamicObjectSave(DynamicObject obj) : base(obj) {
				this.prefabPath = obj.prefabPath;
				this.isGlobal = obj.isAllowedToChangeScenes;
			}

			// Similar to DynamicObject.Destroy but for objects in unloaded scenes
			// Unregisters this save object and any associated save object while in an unloaded scene
			public override void Destroy() {
				base.Destroy();
				
				SaveManager.GetAllAssociatedSaveObjects(associationID)
					// Avoid destroying DynamicObjects, they are already being destroyed
					// Without this where filter, we are opening ourselves up to a stack overflow
					.Where(saveObj => saveObj is not DynamicObjectSave)
					.ForEach(saveObj => saveObj.Destroy());
				DynamicObjectManager.MarkDynamicObjectAsDestroyed(this);
			}
		}
	}
}
