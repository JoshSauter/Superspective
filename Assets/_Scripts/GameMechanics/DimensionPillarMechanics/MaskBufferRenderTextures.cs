using System.Collections;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Creates and handles the visibility masks and any other render texture buffers used for rendering
public class MaskBufferRenderTextures : Singleton<MaskBufferRenderTextures> {
	public Shader portalMaskReplacementShader;
	
	public BladeEdgeDetection edgeDetection;
	public RenderTexture visibilityMaskTexture;
	public RenderTexture portalMaskTexture;
	public RenderTexture edgeDetectionColorsThroughPortals;
	public int visibilityMaskValue; // The value of the visibility masks at the reticle
	static readonly int ResolutionX = Shader.PropertyToID("_ResolutionX");
	static readonly int ResolutionY = Shader.PropertyToID("_ResolutionY");
	static readonly int PortalResolutionX = Shader.PropertyToID("_PortalResolutionX");
	static readonly int PortalResolutionY = Shader.PropertyToID("_PortalResolutionY");
	
	public static readonly int DimensionMask = Shader.PropertyToID("_DimensionMask");
	public static readonly int PortalMask = Shader.PropertyToID("_PortalMask");
	public static readonly int EdgeColorsThroughPortalsMask = Shader.PropertyToID("_EdgeColorsThroughPortalsMask");

	private static readonly int PlayerCamPos = Shader.PropertyToID("_PlayerCamPos");
	
	public const string PORTAL_MASK_REPLACEMENT_TAG = "PortalTag";

	[SerializeField]
	private Shader edgeDetectionColorsThroughPortalShader;

	// Use this for initialization
	void Awake () {
		SuperspectiveScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
		CreateAllRenderTextures(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);

		portalMaskReplacementShader = Shader.Find("Hidden/PortalMask");
		
		SuperspectiveScreen.instance.portalMaskCamera.SetReplacementShader(portalMaskReplacementShader, PORTAL_MASK_REPLACEMENT_TAG);

		edgeDetection.BeforeRenderEdgeDetection += RenderEdgeColorsThroughPortalTexture;

		StartCoroutine(ContinuouslyRequestVisibilityMask());
	}

	private void RenderEdgeColorsThroughPortalTexture() {
		if (edgeDetectionColorsThroughPortalShader == null) {
			Debug.LogError("Edge detection colors through portal shader not set, disabling.");
			this.enabled = false;
			return;
		}
		Camera portalMaskCam = SuperspectiveScreen.instance.portalMaskCamera;
		RenderTexture prevTargetTexture = portalMaskCam.targetTexture;
		portalMaskCam.targetTexture = edgeDetectionColorsThroughPortals;
		portalMaskCam.RenderWithShader(edgeDetectionColorsThroughPortalShader, "PortalTag");
		Shader.SetGlobalTexture(EdgeColorsThroughPortalsMask, edgeDetectionColorsThroughPortals);
		portalMaskCam.targetTexture = prevTargetTexture;
	}

	IEnumerator ContinuouslyRequestVisibilityMask() {
		while (true) {
			yield return new WaitForSeconds(0.25f);
            
			RequestVisibilityMask();
		}
	}

	public void RequestVisibilityMask() {
		Vector2 pixelPositionOfReticle = Interact.instance.PixelPositionOfReticle();
		AsyncGPUReadback.Request(
			visibilityMaskTexture,
			0,
			(int)pixelPositionOfReticle.x,
			1,
			(int)pixelPositionOfReticle.y,
			1,
			0,
			1,
			visibilityMaskTexture.graphicsFormat,
			OnCompleteReadback
		);
	}
	
	void OnCompleteReadback(AsyncGPUReadbackRequest request) {
		if (request.hasError) {
			Debug.LogError("GPU readback error detected");
			return;
		}

		// This happens when the delayed async readback happens as we're exiting play mode
		if (instance == null || visibilityMaskTexture == null) {
			return;
		}
        
		// Read the color of the visibility mask texture to determine which visibility masks are active on cursor
		Texture2D visibilityMaskTex = new Texture2D(
			1,
			1,
			GraphicsFormatUtility.GetTextureFormat(visibilityMaskTexture.graphicsFormat),
			false
		);
		visibilityMaskTex.LoadRawTextureData(request.GetData<Color32>());
		visibilityMaskTex.Apply();

		// Only one pixel so we can sample at 0, 0
		Color sample = visibilityMaskTex.GetPixel(0, 0);

		visibilityMaskValue = DimensionShaderUtils.ChannelFromColor(sample.linear);
	}

	void OnDisable() {
		ReleaseAllTextures();
	}

	void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		ReleaseAllTextures();
		CreateAllRenderTextures(newWidth, newHeight);
		UpdatePortalResolutions();
	}

	private void UpdatePortalResolutions() {
		Shader.SetGlobalFloat(PortalResolutionX, SuperspectiveScreen.instance.currentPortalWidth);
		Shader.SetGlobalFloat(PortalResolutionY, SuperspectiveScreen.instance.currentPortalHeight);
	}

	// Update is called once per frame
	void Update () {
		// Will write to global texture named _DimensionMask
		Shader.SetGlobalTexture(DimensionMask, visibilityMaskTexture);

		// Will write to global texture named _PortalMask
		Shader.SetGlobalTexture(PortalMask, portalMaskTexture);
		
		Shader.SetGlobalVector(PlayerCamPos, Player.instance.playerCam.transform.position);
	}

	void ReleaseAllTextures() {
		visibilityMaskTexture.Release();
		portalMaskTexture.Release();
		edgeDetectionColorsThroughPortals.Release();
	}

	void CreateAllRenderTextures(int currentWidth, int currentHeight) {
		CreateRenderTexture(
			currentWidth,
			currentHeight,
			out visibilityMaskTexture,
			SuperspectiveScreen.instance.dimensionCamera
		).name = "VisibilityMask";

		CreateRenderTexture(
			currentWidth,
			currentHeight,
			out portalMaskTexture,
			SuperspectiveScreen.instance.portalMaskCamera
		).name = "PortalMask";
		
		CreateRenderTexture(
			currentWidth,
			currentHeight,
			out edgeDetectionColorsThroughPortals,
			null
		).name = "EdgeDetectionColorsThroughPortals";
	}

	RenderTexture CreateRenderTexture(int currentWidth, int currentHeight, out RenderTexture rt, Camera targetCamera) {
		rt = new RenderTexture(currentWidth, currentHeight, 24, RenderTextureFormat.ARGB32);
		rt.enableRandomWrite = true;
		rt.Create();

		Shader.SetGlobalFloat(ResolutionX, currentWidth);
		Shader.SetGlobalFloat(ResolutionY, currentHeight);

		if (targetCamera != null) {
			targetCamera.targetTexture = rt;
		}

		return rt;
	}
}
