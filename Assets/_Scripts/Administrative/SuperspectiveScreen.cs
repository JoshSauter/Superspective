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
	public int currentPortalWidth => Mathf.Min(currentWidth, maxPortalWidth);
	public int currentPortalHeight => Mathf.Min(currentHeight, maxPortalHeight);
	public int maxPortalWidth => currentWidth / (1 << portalDownsampleAmount);
	public int maxPortalHeight => currentHeight / (1 << portalDownsampleAmount);

	private int prevPortalDownsampleAmount = 0;
	private static readonly int CameraDepthNormalsTexture = Shader.PropertyToID("_CameraDepthNormalsTexture");

	public int portalDownsampleAmount => Mathf.RoundToInt(Settings.Video.PortalDownsampleAmount);

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
		if (prevPortalDownsampleAmount != portalDownsampleAmount) {
			// Hack to refresh portal RenderTextures
			OnScreenResolutionChanged?.Invoke(currentWidth, currentHeight);
			prevPortalDownsampleAmount = portalDownsampleAmount;
		}
		
		int targetWidth = Screen.width;
		int targetHeight = Screen.height;
		
		// For some reason Screen.width/height doesn't always match the _CameraDepthNormalTexture size.
		// Since we don't have control over the DepthNormalsTexture size, we mimic it instead when it is available
		Texture depthNormals = Shader.GetGlobalTexture(CameraDepthNormalsTexture);
		if (depthNormals != null) {
			Vector2Int depthNormalsSize = new Vector2Int(depthNormals.width, depthNormals.height);
			// Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
			// Debug.Log($"DepthNormals size: {depthNormalsSize}\nScreen size: {screenSize}");
			// Don't run this code in the editor because it will take the depthNormals created for the scene view camera
#if !UNITY_EDITOR
			targetWidth = depthNormalsSize.x;
			targetHeight = depthNormalsSize.y;
#endif
		}

		// Update the resolution if necessary
		if (targetWidth != currentWidth || targetHeight != currentHeight) {

			currentWidth = targetWidth;
			currentHeight = targetHeight;

			OnScreenResolutionChanged?.Invoke(currentWidth, currentHeight);
		}
	}

	void OnPreRender() {
#if UNITY_EDITOR
		if (UnityEditor.EditorApplication.isPaused) {
			//return;
		}
#endif
		OnPlayerCamPreRender?.Invoke();
	}

	IEnumerator OnPostRender() {
		yield return new WaitForEndOfFrame();
		OnPlayerCamPostRender?.Invoke();
	}
}
