using UnityEngine;

// Creates and handles the visibility masks and any other render texture buffers used for rendering
public class MaskBufferRenderTextures : Singleton<MaskBufferRenderTextures> {
	public RenderTexture visibilityMaskTexture;
	public RenderTexture portalMaskTexture;
	static readonly int ResolutionX = Shader.PropertyToID("_ResolutionX");
	static readonly int ResolutionY = Shader.PropertyToID("_ResolutionY");
	static readonly int DimensionMask = Shader.PropertyToID("_DimensionMask");
	static readonly int PortalMask = Shader.PropertyToID("_PortalMask");

	// Use this for initialization
	void Start () {
		SuperspectiveScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
		CreateAllRenderTextures(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);

		SuperspectiveScreen.instance.portalMaskCamera.SetReplacementShader(Shader.Find("Hidden/PortalMask"), "PortalTag");
	}

	void OnDisable() {
		ReleaseAllTextures();
	}

	void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		ReleaseAllTextures();
		CreateAllRenderTextures(newWidth, newHeight);
	}

	// Update is called once per frame
	void Update () {
		// Will write to global texture named _DimensionMask
		Shader.SetGlobalTexture(DimensionMask, visibilityMaskTexture);

		// Will write to global texture named _PortalMask
		Shader.SetGlobalTexture(PortalMask, portalMaskTexture);
	}

	void ReleaseAllTextures() {
		visibilityMaskTexture.Release();
		portalMaskTexture.Release();
	}

	void CreateAllRenderTextures(int currentWidth, int currentHeight) {
		CreateRenderTexture(
			currentWidth,
			currentHeight,
			out visibilityMaskTexture,
			SuperspectiveScreen.instance.dimensionCamera);

		CreateRenderTexture(
			currentWidth,
			currentHeight,
			out portalMaskTexture,
			SuperspectiveScreen.instance.portalMaskCamera
		);
	}

	RenderTexture CreateRenderTexture(int currentWidth, int currentHeight, out RenderTexture rt, Camera targetCamera) {
		rt = new RenderTexture(currentWidth, currentHeight, 24);
		rt.enableRandomWrite = true;
		rt.Create();

		Shader.SetGlobalFloat(ResolutionX, currentWidth);
		Shader.SetGlobalFloat(ResolutionY, currentHeight);

		targetCamera.targetTexture = rt;

		return rt;
	}
}
