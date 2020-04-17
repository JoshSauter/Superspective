using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using EpitaphUtils;

public class InteractableGlowManager : Singleton<InteractableGlowManager> {

	private Camera thisCamera;
	private BladeEdgeDetection edgeDetection;
	private CommandBuffer commandBuffer;

	private HashSet<InteractableGlow> glowableObjects = new HashSet<InteractableGlow>();

	public int blurIterations = 4;

	private Material prePassMaterial;
	private Material prePassMaterialLarger;
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
		prePassMaterial = new Material(Shader.Find("Hidden/GlowCmdShader"));
		prePassMaterialLarger = new Material(Shader.Find("Hidden/GlowCmdShaderLarger"));
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
	private void RebuildCommandBuffer() {
		commandBuffer.Clear();
		commandBuffer.GetTemporaryRT(blurPassRenderTexID, Screen.width >> 1, Screen.height >> 1, 0, FilterMode.Bilinear);
		commandBuffer.SetRenderTarget(blurPassRenderTexID);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);

		// TODO: Temporarily move glowable objects to their projected position/rotation for rendering if they are being hovered through a portal

		// Blur 0-th iteration
		foreach (var glowObject in glowableObjects) {
			commandBuffer.SetGlobalColor(glowColorID, GetColor(glowObject));

			for (int j = 0; j < glowObject.Renderers.Length; j++) {
				//Debug.Log(glowObject.name + "length: " + glowObject.Renderers.Length);
				commandBuffer.DrawRenderer(glowObject.Renderers[j], prePassMaterialLarger);
			}
		}

		// Prepass
		commandBuffer.GetTemporaryRT(prePassRenderTexID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);
		commandBuffer.SetRenderTarget(prePassRenderTexID);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		foreach (var glowObject in glowableObjects) {
			commandBuffer.SetGlobalColor(glowColorID, GetColor(glowObject));

			for (int j = 0; j < glowObject.Renderers.Length; j++) {
				//Debug.Log(glowObject.name + "length: " + glowObject.Renderers.Length);
				commandBuffer.DrawRenderer(glowObject.Renderers[j], prePassMaterial);
			}
		}

		commandBuffer.GetTemporaryRT(tempRenderTexID, Screen.width >> 1, Screen.height >> 1, 0, FilterMode.Bilinear);

		blurTexelSize = new Vector2(1.5f / (Screen.width >> 1), 1.5f / (Screen.height >> 1));
		commandBuffer.SetGlobalVector(blurSizeID, blurTexelSize);

		for (int i = 0; i < blurIterations; i++) {
			commandBuffer.Blit(blurPassRenderTexID, tempRenderTexID, blurMaterial, 0);
			commandBuffer.Blit(tempRenderTexID, blurPassRenderTexID, blurMaterial, 1);
		}

		Graphics.ExecuteCommandBuffer(commandBuffer);
	}

	/// <summary>
	/// Rebuild the Command Buffer each frame to account for changes in color.
	/// This could be improved to only rebuild when necessary when colors are changing.
	/// 
	/// Could be further optimized to not include objects which are currently black and not
	/// affect thing the glow image.
	/// </summary>
	private void OnPreCull() {
		RebuildCommandBuffer();
	}

	private void OnPostRender() {
		Dictionary<PickupObject, TransformInfo> cachedTransforms = new Dictionary<PickupObject, TransformInfo>();
		foreach (var glowObject in glowableObjects) {
			commandBuffer.SetGlobalColor(glowColorID, GetColor(glowObject));

			// WTF
			PickupObject pickupCube = glowObject.GetComponent<PickupObject>();
			if (pickupCube != null && pickupCube.grabbedThroughPortal != null) {
				//pickupCube.grabbedThroughPortal.otherPortal.TransformObject(glowObject.transform);
				cachedTransforms.Add(pickupCube, new TransformInfo(pickupCube.transform));
				//pickupCube.transform.position -= Vector3.one * 0.1f;
			}
		}

		Graphics.ExecuteCommandBuffer(commandBuffer);

		foreach (var glowObject in glowableObjects) {
			commandBuffer.SetGlobalColor(glowColorID, GetColor(glowObject));

			PickupObject pickupCube = glowObject.GetComponent<PickupObject>();
			if (pickupCube != null && pickupCube.grabbedThroughPortal != null) {
				//pickupCube.grabbedThroughPortal.TransformObject(glowObject.transform);
				cachedTransforms[pickupCube].ApplyToTransform(pickupCube.transform);
			}
		}
	}

	private Color GetColor(InteractableGlow objGlow) {
		Color color = new Color();
		if (edgeDetection == null) {
			color = objGlow.CurrentColor;
		}
		else {
			switch (edgeDetection.edgeColorMode) {
				case BladeEdgeDetection.EdgeColorMode.colorRampTexture:
					color = objGlow.CurrentColor;
					break;
				case BladeEdgeDetection.EdgeColorMode.gradient:
					color = ColorOfGradient(edgeDetection.edgeColorGradient);
					color.a = objGlow.CurrentColor.a;
					break;
				case BladeEdgeDetection.EdgeColorMode.simpleColor:
					color = edgeDetection.edgeColor;
					color.a = objGlow.CurrentColor.a;
					break;
				default:
					color = objGlow.CurrentColor;
					break;
			}
		}

		return color;
	}

	private Color ColorOfGradient(Gradient gradient) {
		return gradient.Evaluate(Interact.instance.interactionDistance / thisCamera.farClipPlane);
	}
}
