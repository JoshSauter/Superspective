using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class EpitaphScreen : Singleton<EpitaphScreen> {
	public Camera playerCamera;
	public Camera[] dimensionCameras;
	public Camera invertMaskCamera;
	public static int currentWidth;
	public static int currentHeight;

	public delegate void ScreenResolutionChangedAction(int newWidth, int newHeight);
	public event ScreenResolutionChangedAction OnScreenResolutionChanged;

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
		invertMaskCamera = childrenCams[MaskBufferRenderTextures.numVisibilityMaskChannels];
	}

	// Update is called once per frame
	void Update() {
		// Update the resolution if necessary
		if (Screen.width != currentWidth || Screen.height != currentHeight) {

			currentWidth = Screen.width;
			currentHeight = Screen.height;

			if (OnScreenResolutionChanged != null) {
				OnScreenResolutionChanged(currentWidth, currentHeight);
			}
		}
	}
}
