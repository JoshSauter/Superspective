using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class EpitaphScreen : Singleton<EpitaphScreen> {
	public Camera playerCamera;
	public Camera dimensionCamera;
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
		dimensionCamera = childrenCams[0];
		invertMaskCamera = childrenCams[1];
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
