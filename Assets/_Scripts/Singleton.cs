using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {

	private static T _instance = null;

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
					Debug.LogError("No Object of type " + typeof(T).Name + " exists. Make sure you add one to the scene.");
				}
			}

			return _instance;
		}
	}
}

public class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour {
	public virtual void Awake() {
		if (instance != null) {
			DontDestroyOnLoad(instance);
		}
	}
}
