using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameWindow : Singleton<GameWindow> {
#if UNITY_EDITOR
	private EditorWindow _gameWindow = null;
	public EditorWindow gameWindow {
		get {
			if (_gameWindow == null) {
				var windows = (EditorWindow[])Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
				foreach (var window in windows) {
					if (window != null && window.GetType().FullName == "UnityEditor.GameView") {
						_gameWindow = window;
						break;
					}
				}
			}

			return _gameWindow;
		}
	}

	public bool maximizeOnPlay {
		get {
			if (gameWindow != null) {
				System.Reflection.PropertyInfo property = gameWindow.GetType().GetProperty("maximizeOnPlay");
				bool maximized = (bool)property.GetValue(gameWindow, null);
				return maximized;
			}

			Debug.LogError("No game window found, returning false");
			return false;
		}
		set {
			System.Reflection.PropertyInfo property = gameWindow.GetType().GetProperty("maximizeOnPlay");
			property.SetValue(gameWindow, value);
		}
	}

#endif
}
