using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Saving.DynamicObjectManager;

namespace Saving {
    // Dynamic objects are objects that may be created or destroyed at runtime
    // They must provide what type of object they are so they can be instantiated from a prefab if it does not exist
	// They are loaded before SaveableObjects so that the instance exists for other Component loads
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
		public DynamicObjectType Type;

		public string ID => $"{Type:g}_{id.uniqueId}";
		public bool isGlobal = true;

		// GlobalObjects are moved between scenes as they hit objects from various scenes
		private void OnCollisionEnter(Collision collision) {
			if (isGlobal) {
				Scene sceneOfContact = collision.collider.gameObject.scene;
				SceneManager.MoveGameObjectToScene(gameObject, sceneOfContact);
			}
		}

		[Serializable]
		public class DynamicObjectSave {
			public int Type;
			public bool isGlobal;
			public string scene;

			public DynamicObjectSave(DynamicObject obj) {
				this.Type = (int)obj.Type;
				this.isGlobal = obj.isGlobal;
				this.scene = obj.gameObject.scene.name;
			}

			public void LoadSave(DynamicObject obj) {
				obj.Type = (DynamicObjectType)this.Type;
				obj.isGlobal = this.isGlobal;
				if (scene != null && scene != "") {
					if (obj.transform.parent != null) {
						obj.transform.SetParent(null);
					}
					SceneManager.MoveGameObjectToScene(obj.gameObject, SceneManager.GetSceneByName(scene));
				}
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
