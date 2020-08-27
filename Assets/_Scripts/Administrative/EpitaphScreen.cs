using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

[ExecuteInEditMode]
public class EpitaphScreen : Singleton<EpitaphScreen> {
	public Camera playerCamera;
	public MonoBehaviour[] postProcessEffects;
	public Camera[] dimensionCameras;
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
		dimensionCameras = new Camera[MaskBufferRenderTextures.numVisibilityMaskChannels];
		for (int i = 0; i < MaskBufferRenderTextures.numVisibilityMaskChannels; i++) {
			dimensionCameras[i] = childrenCams[i];
		}
		portalMaskCamera = childrenCams[MaskBufferRenderTextures.numVisibilityMaskChannels];
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

	private void OnPreRender() {
		OnPlayerCamPreRender?.Invoke();
	}

	private IEnumerator OnPostRender() {
		yield return new WaitForEndOfFrame();
		OnPlayerCamPostRender?.Invoke();
	}
}
