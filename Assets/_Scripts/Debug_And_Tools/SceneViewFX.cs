using System.Collections.Generic;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using EpitaphUtils;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SceneViewFX : Singleton<SceneViewFX> {
	public bool DEBUG = false;
	DebugLogger debug;

#if UNITY_EDITOR
	private SceneView sceneView;
	private Camera sceneViewCamera;
	private Camera myCamera;
	//[HideInInspector]
	public bool cachedEnableState;

	[MenuItem("Custom/SceneFxToggle _F1")]
	private static void ToggleFx() {
		DebugPrintState("ToggleFx()");
		if (instance != null) {
			instance.enabled = !instance.enabled;
			instance.cachedEnableState = instance.enabled;
		}
	}

	private void OnReloadScripts() {
		DebugPrintState("OnReloadScripts()");
		// Re-enabling the script prevents the Scene view window bug
		cachedEnableState = instance.enabled;
		debug.Log("Cached enabled state after: " + cachedEnableState);
		instance.enabled = false;
		if (Application.isPlaying) {
			instance.enabled = true;
			cachedEnableState = instance.enabled;
		}
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	private static void AfterScriptsReloaded() {
		DebugPrintState("AfterScriptsReloaded()");
		if (UnityEditorInternal.InternalEditorUtility.isApplicationActive) {
			instance.enabled = instance.cachedEnableState;
			instance?.debug?.Log("SceneViewFX: " + ((instance.enabled) ? "On" : "Off"));
		}
	}

	private void OnEnable() {
		debug = new DebugLogger(this, () => DEBUG);

		DebugPrintState("OnEnable()");
		if (instance != null && instance != this) {
			debug.LogError("Cannot add SceneViewFX. Already one active in this scene");
			DestroyImmediate(this);
			return;
		}
		sceneViewCamera = GetCamera();
		if (!Application.isPlaying) {
			UpdateComponents();
		}

		AssemblyReloadEvents.beforeAssemblyReload += OnReloadScripts;
	}

	private void OnDisable() {
		DebugPrintState("OnDisable()");
		ClearCurrentEffects();
	}

	private Camera GetCamera() {
		myCamera = GetComponent<Camera>();
		sceneView = EditorWindow.GetWindow<SceneView>();
		return sceneView.camera;
	}

	// get components from main game camera.
	private Component[] GetComponents() {
		var result = myCamera.GetComponents<Component>();
		if (result != null && result.Length > 1) {
			// exlude these components:
			List<Component> excludes = new List<Component>();
			excludes.Add(myCamera.transform);
			excludes.Add(myCamera);
			if (myCamera.GetComponent<AudioListener>()) excludes.Add(myCamera.GetComponent<AudioListener>());
			if (myCamera.GetComponent<EpitaphScreen>()) excludes.Add(myCamera.GetComponent<EpitaphScreen>());
			if (myCamera.GetComponent<MaskBufferRenderTextures>()) excludes.Add(myCamera.GetComponent<MaskBufferRenderTextures>());
			if (myCamera.GetComponent<SketchOverlay>()) excludes.Add(myCamera.GetComponent<SketchOverlay>());
			if (myCamera.GetComponent("FlareLayer")) excludes.Add(myCamera.GetComponent("FlareLayer"));
			if (myCamera.GetComponent<SceneViewFX>()) excludes.Add(myCamera.GetComponent<SceneViewFX>());
			if (myCamera.GetComponent<CameraFollow>()) excludes.Add(myCamera.GetComponent<CameraFollow>());
			if (myCamera.GetComponent<InteractableGlowController>()) excludes.Add(myCamera.GetComponent<InteractableGlowController>());
			if (myCamera.GetComponent<GlowComposite>()) excludes.Add(myCamera.GetComponent<GlowComposite>());
			result = result.Except(excludes).ToArray();
		}
		return result;
	}

	public void Update() {
		if (Application.isPlaying) UpdateComponents();
		else if (enabled) UpdateComponents();
	}

	// update scene view components
	public void UpdateComponents() {
		if (sceneViewCamera == null) sceneViewCamera = GetCamera();
		if (sceneViewCamera == null) return;
		ClearCurrentEffects();
		var components = GetComponents();
		if (components != null && components.Length > 1) {
			var cameraGo = sceneViewCamera.gameObject;
			for (int i = 0; i < components.Length; i++) {
				var c = components[i];
				if (c == null) continue;
				var cType = c.GetType();
				var existing = cameraGo.AddComponent(cType);
				EditorUtility.CopySerialized(c, existing);
			}
		}
		sceneViewCamera.allowHDR = myCamera.allowHDR;
	}

	public void ClearCurrentEffects() {
		// clear sceneview camera of any previous components / fx.
		if (sceneViewCamera == null) sceneViewCamera = GetCamera();
		if (sceneViewCamera == null) return;
		Component[] compsOnCam = sceneViewCamera.GetComponents<Component>();
		for (int i = compsOnCam.Length - 1; i >= 0; i--) {
			// these components are default on the SceneView camera...
			if (sceneViewCamera.GetComponent("HaloLayer") == compsOnCam[i]) continue;
			if (sceneViewCamera.GetComponent("FlareLayer") == compsOnCam[i]) continue;
			if (compsOnCam[i] is Transform) continue;
			if (compsOnCam[i] is Camera) continue;
			DestroyImmediate(compsOnCam[i]);
		}
	}

	private static void DebugPrintState(string methodName) {
		instance?.debug?.Log("SceneViewFX." + methodName + "\nEnabled: " + instance.enabled + "\nCachedEnableState: " + instance.cachedEnableState);
	}

#endif
}