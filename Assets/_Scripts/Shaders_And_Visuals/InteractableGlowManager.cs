using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using EpitaphUtils;

public class InteractableGlowManager : Singleton<InteractableGlowManager> {
	public bool DEBUG = false;
	DebugLogger debug;
	private Camera thisCamera;
	private BladeEdgeDetection edgeDetection;
	private CommandBuffer commandBuffer;

	private HashSet<InteractableGlow> glowableObjects = new HashSet<InteractableGlow>();

	public int blurIterations = 4;

	private Material prePassMaterial;
	private Material prePassMaterialLarger;
	private Material prePassMaterialSmaller;
	private Material blurMaterial;
	private Vector2 blurTexelSize;

	private int prePassRenderTexID;
	private int blurPassRenderTexID;
	private int tempRenderTexID;
	private int blurSizeID;
	private int glowColorID;

	private int glowLayer;

	public void Add(InteractableGlow glowObj) {
		glowableObjects.Add(glowObj);
	}

	public void Remove(InteractableGlow glowObj) {
		glowableObjects.Remove(glowObj);
	}

	/// <summary>
	/// On Awake, we cache various values and setup our command buffer to be called Before Image Effects.
	/// </summary>
	private void Awake() {
		debug = new DebugLogger(gameObject, () => DEBUG);
		prePassMaterial = new Material(Shader.Find("Hidden/GlowCmdShader"));
		prePassMaterialLarger = new Material(Shader.Find("Hidden/GlowCmdShaderLarger"));
		prePassMaterialSmaller = new Material(Shader.Find("Hidden/GlowCmdShaderSmaller"));
		blurMaterial = new Material(Shader.Find("Hidden/Blur"));

		prePassRenderTexID = Shader.PropertyToID("_GlowPrePassTex");
		blurPassRenderTexID = Shader.PropertyToID("_GlowBlurredTex");
		tempRenderTexID = Shader.PropertyToID("_TempTex0");
		blurSizeID = Shader.PropertyToID("_BlurSize");
		glowColorID = Shader.PropertyToID("_GlowColor");

		commandBuffer = new CommandBuffer();
		commandBuffer.name = "Glowing Objects Buffer"; // This name is visible in the Frame Debugger, so make it a descriptive!
		thisCamera = GetComponent<Camera>();
		edgeDetection = thisCamera.GetComponent<BladeEdgeDetection>();
	}

	/// <summary>
	/// Adds all the commands, in order, we want our command buffer to execute.
	/// Similar to calling sequential rendering methods insde of OnRenderImage().
	/// </summary>
	private int RebuildCommandBuffer() {
		commandBuffer.Clear();
		commandBuffer.GetTemporaryRT(blurPassRenderTexID, EpitaphScreen.currentWidth >> 1, EpitaphScreen.currentHeight >> 1, 0, FilterMode.Bilinear);
		commandBuffer.SetRenderTarget(blurPassRenderTexID);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);

		// Blur 0-th iteration
		foreach (var glowObject in glowableObjects) {
			if (glowObject.renderers == null) continue;
			commandBuffer.SetGlobalColor(glowColorID, GetColor(glowObject));

			for (int j = 0; j < glowObject.renderers.Count; j++) {
				//Debug.Log(glowObject.name + "length: " + glowObject.Renderers.Length);
				commandBuffer.DrawRenderer(glowObject.renderers[j], glowObject.useLargerPrepassMaterial ? prePassMaterialLarger : prePassMaterial);
			}
		}

		// Prepass
		commandBuffer.GetTemporaryRT(prePassRenderTexID, EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 0, FilterMode.Bilinear);
		commandBuffer.SetRenderTarget(prePassRenderTexID);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		foreach (var glowObject in glowableObjects) {
			if (glowObject.renderers == null) continue;
			commandBuffer.SetGlobalColor(glowColorID, GetColor(glowObject));

			for (int j = 0; j < glowObject.renderers.Count; j++) {
				//Debug.Log(glowObject.name + "length: " + glowObject.Renderers.Length);
				commandBuffer.DrawRenderer(glowObject.renderers[j], glowObject.useLargerPrepassMaterial ? prePassMaterial : prePassMaterialSmaller);
			}
		}

		commandBuffer.GetTemporaryRT(tempRenderTexID, EpitaphScreen.currentWidth >> 1, EpitaphScreen.currentHeight >> 1, 0, FilterMode.Bilinear);

		blurTexelSize = new Vector2(1.5f / (EpitaphScreen.currentWidth >> 1), 1.5f / (EpitaphScreen.currentHeight >> 1));
		commandBuffer.SetGlobalVector(blurSizeID, blurTexelSize);

		for (int i = 0; i < blurIterations; i++) {
			commandBuffer.Blit(blurPassRenderTexID, tempRenderTexID, blurMaterial, 0);
			commandBuffer.Blit(tempRenderTexID, blurPassRenderTexID, blurMaterial, 1);
		}

		Graphics.ExecuteCommandBuffer(commandBuffer);

		return glowableObjects.Count;
	}

	/// <summary>
	/// Rebuild the Command Buffer each frame to account for changes in color.
	/// This could be improved to only rebuild when necessary when colors are changing.
	/// 
	/// Could be further optimized to not include objects which are currently black and not
	/// affect thing the glow image.
	/// </summary>
	private void OnPreCull() {
		debug.LogError(RebuildCommandBuffer());
	}

	void OnPostRender() {
		Graphics.ExecuteCommandBuffer(commandBuffer);
	}

	private Color GetColor(InteractableGlow objGlow) {
		Color color = new Color();
		if (edgeDetection == null || objGlow.overrideGlowColor) {
			color = objGlow.currentColor;
		}
		else {
			switch (edgeDetection.edgeColorMode) {
				case BladeEdgeDetection.EdgeColorMode.colorRampTexture:
					color = objGlow.currentColor;
					break;
				case BladeEdgeDetection.EdgeColorMode.gradient:
					color = ColorOfGradient(edgeDetection.edgeColorGradient);
					color.a = objGlow.currentColor.a;
					break;
				case BladeEdgeDetection.EdgeColorMode.simpleColor:
					color = edgeDetection.edgeColor;
					color.a = objGlow.currentColor.a;
					break;
				default:
					color = objGlow.currentColor;
					break;
			}
		}

		return color;
	}

	private Color ColorOfGradient(Gradient gradient) {
		return gradient.Evaluate(Interact.instance.interactionDistance / thisCamera.farClipPlane);
	}
}
