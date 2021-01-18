using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Saving.DynamicObjectManager;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Saving {
    // Dynamic objects are objects that may be created or destroyed at runtime
    // They must provide what type of object they are so they can be instantiated from a prefab if it does not exist
	// They are loaded before SaveableObjects so that the instance exists for other Component loads
	[ExecuteInEditMode]
    public class DynamicObject : MonoBehaviour, SaveableObject {
		UniqueId _id;
		public UniqueId id {
			get {
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
		public string ID => id.uniqueId;
		public bool isGlobal = true;

		PickupObject pickup;

		void OnValidate() {
			if ((gameObject.scene == null || gameObject.scene.name == null || gameObject.scene.name == "") && (prefabPath == null || prefabPath == "")) {
#if UNITY_EDITOR
				prefabPath = AssetDatabase.GetAssetPath(this)
					// Strip prefix and suffix to get the Resources-relative path
					.Replace("Assets/Resources/", "")
					.Replace(".prefab", "");
#endif
			}
		}

		void Awake() {
			if (prefabPath == "") {
				Debug.LogError($"{gameObject.name}: No prefab for this DynamicObject", gameObject);
			}
			pickup = GetComponent<PickupObject>();
		}

		void Update() {
			if (pickup != null && pickup.isHeld) {
				SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName(LevelManager.instance.activeSceneName));
			}
		}

		// GlobalObjects are moved between scenes as they hit objects from various scenes
		void OnCollisionEnter(Collision collision) {
			if (isGlobal) {
				Scene sceneOfContact = collision.collider.gameObject.scene;
				if (transform.parent != null) {
					transform.parent = null;
				}
				SceneManager.MoveGameObjectToScene(gameObject, sceneOfContact);
			}
		}

		[Serializable]
		public class DynamicObjectSave {
			public string prefabPath;
			public bool isGlobal;
			public string scene;
			public bool active;

			public DynamicObjectSave(DynamicObject obj) {
				this.prefabPath = obj.prefabPath;
				this.isGlobal = obj.isGlobal;
				this.scene = obj.gameObject.scene.name;
				this.active = obj.gameObject.activeSelf;
			}

			public void LoadSave(DynamicObject obj) {
				obj.prefabPath = this.prefabPath;
				obj.isGlobal = this.isGlobal;
				if (scene != null && scene != "") {
					if (obj.transform.parent != null) {
						obj.transform.SetParent(null);
					}
					SceneManager.MoveGameObjectToScene(obj.gameObject, SceneManager.GetSceneByName(scene));
				}
				obj.gameObject.SetActive(this.active);
			}
		}

		public object GetSaveObject() {
			return new DynamicObjectSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			(savedObject as DynamicObjectSave).LoadSave(this);
		}
	}
}
