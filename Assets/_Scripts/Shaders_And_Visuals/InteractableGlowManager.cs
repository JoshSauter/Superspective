using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SuperspectiveUtils;

public class InteractableGlowManager : Singleton<InteractableGlowManager> {
	public bool DEBUG = false;
	DebugLogger debug;
	Camera thisCamera;
	BladeEdgeDetection edgeDetection;
	CommandBuffer commandBuffer;

	HashSet<InteractableGlow> glowableObjects = new HashSet<InteractableGlow>();

	public const int blurIterations = 4;

	Material prePassMaterial;
	Material prePassMaterialLarger;
	Material prePassMaterialSmaller;
	Material blurMaterial;
	Vector2 blurTexelSize;

	int prePassRenderTexID;
	int blurPassRenderTexID;
	int tempRenderTexID;
	int blurSizeID;
	int glowColorID;

	public void Add(InteractableGlow glowObj) {
		glowableObjects.Add(glowObj);
	}

	public void Remove(InteractableGlow glowObj) {
		glowableObjects.Remove(glowObj);
	}

	/// <summary>
	/// On Awake, we cache various values and setup our command buffer to be called Before Image Effects.
	/// </summary>
	void Awake() {
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
	int RebuildCommandBuffer() {
		commandBuffer.Clear();
		commandBuffer.GetTemporaryRT(blurPassRenderTexID, SuperspectiveScreen.currentWidth >> 1, SuperspectiveScreen.currentHeight >> 1, 0, FilterMode.Bilinear);
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
		commandBuffer.GetTemporaryRT(prePassRenderTexID, SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 0, FilterMode.Bilinear);
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

		commandBuffer.GetTemporaryRT(tempRenderTexID, SuperspectiveScreen.currentWidth >> 1, SuperspectiveScreen.currentHeight >> 1, 0, FilterMode.Bilinear);

		blurTexelSize = new Vector2(1.5f / (SuperspectiveScreen.currentWidth >> 1), 1.5f / (SuperspectiveScreen.currentHeight >> 1));
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
	void OnPreCull() {
		debug.LogError(RebuildCommandBuffer());
	}

	void OnPostRender() {
		Graphics.ExecuteCommandBuffer(commandBuffer);
	}

	Color GetColor(InteractableGlow objGlow) {
		Color color = new Color();
		if (edgeDetection == null || objGlow.overrideGlowColor) {
			color = objGlow.currentColor;
		}
		else {
			switch (edgeDetection.edgeColorMode) {
				case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
					color = objGlow.currentColor;
					break;
				case BladeEdgeDetection.EdgeColorMode.Gradient:
					color = ColorOfGradient(edgeDetection.edgeColorGradient);
					color.a = objGlow.currentColor.a;
					break;
				case BladeEdgeDetection.EdgeColorMode.SimpleColor:
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

	Color ColorOfGradient(Gradient gradient) {
		return gradient.Evaluate(Interact.instance.interactionDistance / thisCamera.farClipPlane);
	}
}
