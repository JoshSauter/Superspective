using UnityEngine;

// Creates and handles the visibility masks and any other render texture buffers used for rendering
public class MaskBufferRenderTextures : Singleton<MaskBufferRenderTextures> {
	public const int numVisibilityMaskChannels = 2;
	public RenderTexture[] visibilityMaskTextures;
	public RenderTexture portalMaskTexture;

	// Use this for initialization
	void Start () {
		visibilityMaskTextures = new RenderTexture[numVisibilityMaskChannels];

		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
		CreateAllRenderTextures(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);

		EpitaphScreen.instance.portalMaskCamera.SetReplacementShader(Shader.Find("Hidden/PortalMask"), "PortalTag");
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
		for (int i = 0; i < numVisibilityMaskChannels; i++) {
			Shader.SetGlobalTexture("_DimensionMask" + i, visibilityMaskTextures[i]);
		}

		// Will write to global texture named _PortalMask
		Shader.SetGlobalTexture("_PortalMask", portalMaskTexture);
	}

	void ReleaseAllTextures() {
		for (int i = 0; i < numVisibilityMaskChannels; i++) {
			visibilityMaskTextures[i].Release();
		}
		portalMaskTexture.Release();
	}

	void CreateAllRenderTextures(int currentWidth, int currentHeight) {
		for (int i = 0; i < numVisibilityMaskChannels; i++) {
			CreateRenderTexture(currentWidth, currentHeight, out visibilityMaskTextures[i], EpitaphScreen.instance.dimensionCameras[i]);
		}
		CreateRenderTexture(currentWidth, currentHeight, out portalMaskTexture, EpitaphScreen.instance.portalMaskCamera);
	}

	RenderTexture CreateRenderTexture(int currentWidth, int currentHeight, out RenderTexture rt, Camera targetCamera) {
		rt = new RenderTexture(currentWidth, currentHeight, 24);
		rt.enableRandomWrite = true;
		rt.Create();

		Shader.SetGlobalFloat("_ResolutionX", currentWidth);
		Shader.SetGlobalFloat("_ResolutionY", currentHeight);

		targetCamera.targetTexture = rt;

		return rt;
	}
}
