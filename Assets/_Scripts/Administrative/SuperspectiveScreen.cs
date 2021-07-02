using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;

[ExecuteInEditMode]
public class SuperspectiveScreen : Singleton<SuperspectiveScreen> {
	public Camera playerCamera;
	public MonoBehaviour[] postProcessEffects;
	public Camera dimensionCamera;
	public Camera portalMaskCamera;
	public static int currentWidth;
	public static int currentHeight;

	public delegate void ScreenResolutionChangedAction(int newWidth, int newHeight);
	public event ScreenResolutionChangedAction OnScreenResolutionChanged;

	// Used for receiving OnPreRender instructions from scripts not attached to the main camera
	public delegate void OnRenderAction();
	public event OnRenderAction OnPlayerCamPreRender;
	public event OnRenderAction OnPlayerCamPostRender;

	// Use this for initialization
	void Awake () {
		currentWidth = Screen.width;
		currentHeight = Screen.height;

		playerCamera = GetComponent<Camera>();
		Camera[] childrenCams = transform.GetComponentsInChildrenOnly<Camera>();
		dimensionCamera = childrenCams[0];
		portalMaskCamera = childrenCams[1];
	}

	// Update is called once per frame
	void Update() {
		// Update the resolution if necessary
		if (Screen.width != currentWidth || Screen.height != currentHeight) {

			currentWidth = Screen.width;
			currentHeight = Screen.height;

			OnScreenResolutionChanged?.Invoke(currentWidth, currentHeight);
		}
	}

	void OnPreRender() {
		OnPlayerCamPreRender?.Invoke();
	}

	IEnumerator OnPostRender() {
		yield return new WaitForEndOfFrame();
		OnPlayerCamPostRender?.Invoke();
	}
}
