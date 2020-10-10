using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Saving;
using SerializableClasses;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BladeEdgeDetection : MonoBehaviour, SaveableObject {
	public enum EdgeColorMode {
		simpleColor,
		gradient,
		colorRampTexture
	}
	public enum WeightedEdgeMode {
		unweighted,
		weightedByDepth,
		weightedByNormals,
		weightedByDepthAndNormals
	}
	// In debug mode, red indicates a depth-detected edge, green indicates a normal-detected edge, and yellow indicates that both checks detected an edge
	public bool debugMode = false;

	public bool doubleSidedEdges = false;
	public bool checkPortalDepth = false;
	public float depthSensitivity = 1;
	public float normalSensitivity = 1;
	public int sampleDistance = 1;

	// Weighted edges options
	public WeightedEdgeMode weightedEdgeMode = WeightedEdgeMode.unweighted;
	public float depthWeightEffect = 0f;
	public float normalWeightEffect = 0f;

	// Edge color options
	public EdgeColorMode edgeColorMode = EdgeColorMode.simpleColor;
	public Color edgeColor = Color.black;
	public Gradient edgeColorGradient;
	public Texture2D edgeColorGradientTexture;

	[SerializeField]
	Shader edgeDetectShader;
	Material shaderMaterial;
	Camera thisCamera;

	private const float DEPTH_SENSITIVITY_MULTIPLIER = 40;	// Keeps depth-sensitivity values close to normal-sensitivity values in the inspector
	private const int GRADIENT_ARRAY_SIZE = 10;

	// Allocate once to save GC every frame
	private float[] floatGradientBuffer = new float[GRADIENT_ARRAY_SIZE];
	private Color[] colorGradientBuffer = new Color[GRADIENT_ARRAY_SIZE];

	[NonSerialized]
	Vector3[] frustumCorners;
	[NonSerialized]
	Vector4[] frustumCornersOrdered;

	private void OnEnable () {
		SetDepthNormalTextureFlag();
		frustumCorners = new Vector3[4];
		frustumCornersOrdered = new Vector4[4];
	}
	
	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (shaderMaterial == null && !CreateMaterial()) {
			Debug.LogError("Failed to create shader material!");
			Graphics.Blit(source, destination);
			this.enabled = false;
		}

		shaderMaterial.SetFloat("_DebugMode", debugMode ? 1 : 0);

		if (doubleSidedEdges) {
			shaderMaterial.EnableKeyword("DOUBLE_SIDED_EDGES");
		}
		else {
			shaderMaterial.DisableKeyword("DOUBLE_SIDED_EDGES");
		}
		if (checkPortalDepth) {
			shaderMaterial.EnableKeyword("CHECK_PORTAL_DEPTH");
		}
		else {
			shaderMaterial.DisableKeyword("CHECK_PORTAL_DEPTH");
		}
		// Note: Depth sensitivity originally calibrated for camera with a far plane of 400, this normalizes it for other cameras
		shaderMaterial.SetFloat("_DepthSensitivity", depthSensitivity * DEPTH_SENSITIVITY_MULTIPLIER * (thisCamera.farClipPlane/400));
		shaderMaterial.SetFloat("_NormalSensitivity", normalSensitivity);
		shaderMaterial.SetInt("_SampleDistance", sampleDistance);

		shaderMaterial.SetInt("_ColorMode", (int)edgeColorMode);	
		switch (edgeColorMode) {
			case EdgeColorMode.simpleColor:
				shaderMaterial.SetColor("_EdgeColor", edgeColor);
				break;
			case EdgeColorMode.gradient:
				SetEdgeColorGradient();
				break;
			case EdgeColorMode.colorRampTexture:
				shaderMaterial.SetTexture("_GradientTexture", edgeColorGradientTexture);
				break;
		}

		shaderMaterial.SetInt("_WeightedEdgeMode", (int)weightedEdgeMode);
		shaderMaterial.SetFloat("_DepthWeightEffect", depthWeightEffect);
		shaderMaterial.SetFloat("_NormalWeightEffect", normalWeightEffect);

		Graphics.Blit(source, destination, shaderMaterial);
	}

	/// <summary>
	/// Sets the _GradientKeyTimes and _EdgeColorGradient float and Color arrays, respectively, in the BladeEdgeDetectionShader
	/// Populates _GradientKeyTimes with the times of each colorKey in edgeColorGradient (as well as a 0 as the first key and a series of 1s to fill out the array at the end)
	/// Populates _EdgeColorGradient with the colors of each colorKey in edgeColorGradient (as well as values for the times filled in as described above)
	/// </summary>
	private void SetEdgeColorGradient() {
		Color startColor = edgeColorGradient.Evaluate(0);
		Color endColor = edgeColorGradient.Evaluate(1);
		float startAlpha = startColor.a;
		float endAlpha = endColor.a;

		shaderMaterial.SetFloatArray("_GradientKeyTimes", GetGradientFloatValues(0f, edgeColorGradient.colorKeys.Select(x => x.time), 1f));
		shaderMaterial.SetColorArray("_EdgeColorGradient", GetGradientColorValues(startColor, edgeColorGradient.colorKeys.Select(x => x.color), endColor));
		shaderMaterial.SetFloatArray("_GradientAlphaKeyTimes", GetGradientFloatValues(0f, edgeColorGradient.alphaKeys.Select(x => x.time), 1f));
		shaderMaterial.SetFloatArray("_AlphaGradient", GetGradientFloatValues(startAlpha, edgeColorGradient.alphaKeys.Select(x => x.alpha), endAlpha));

		shaderMaterial.SetInt("_GradientMode", edgeColorGradient.mode == GradientMode.Blend ? 0 : 1);

		SetFrustumCornersVector();
	}

	private void SetFrustumCornersVector() {
		thisCamera.CalculateFrustumCorners(
			new Rect(0, 0, 1, 1),
			thisCamera.farClipPlane,
			thisCamera.stereoActiveEye,
			frustumCorners
		);

		frustumCornersOrdered[0] = frustumCorners[0];   // Bottom-left
		frustumCornersOrdered[1] = frustumCorners[3];   // Bottom-right
		frustumCornersOrdered[2] = frustumCorners[1];   // Top-left
		frustumCornersOrdered[3] = frustumCorners[2];   // Top-right
		shaderMaterial.SetVectorArray("_FrustumCorners", frustumCornersOrdered);
	}

	// Actually just populates the float buffer with the values provided, then returns a reference to the float buffer
	private float[] GetGradientFloatValues(float startValue, IEnumerable<float> middleValues, float endValue) {
		float[] middleValuesArray = middleValues.ToArray();
		floatGradientBuffer[0] = startValue;
		for (int i = 1; i < middleValuesArray.Length + 1; i++) {
			floatGradientBuffer[i] = middleValuesArray[i - 1];
		}
		for (int j = middleValuesArray.Length + 1; j < GRADIENT_ARRAY_SIZE; j++) {
			floatGradientBuffer[j] = endValue;
		}
		return floatGradientBuffer;
	}

	// Actually just populates the color buffer with the values provided, then returns a reference to the color buffer
	private Color[] GetGradientColorValues(Color startValue, IEnumerable<Color> middleValues, Color endValue) {
		Color[] middleValuesArray = middleValues.ToArray();
		colorGradientBuffer[0] = startValue;
		for (int i = 1; i < middleValuesArray.Length + 1; i++) {
			colorGradientBuffer[i] = middleValuesArray[i - 1];
		}
		for (int j = middleValuesArray.Length + 1; j < GRADIENT_ARRAY_SIZE; j++) {
			colorGradientBuffer[j] = endValue;
		}
		return colorGradientBuffer;
	}

	private void SetDepthNormalTextureFlag () {
		if (thisCamera == null) thisCamera = GetComponent<Camera>();
		thisCamera.depthTextureMode = DepthTextureMode.DepthNormals;
	}

	private bool CreateMaterial() {
		if (!edgeDetectShader.isSupported) {
			return false;
		}
		shaderMaterial = new Material(edgeDetectShader);
		shaderMaterial.hideFlags = HideFlags.HideAndDontSave;

		return shaderMaterial != null;
	}

	private void OnDisable() {
		if (shaderMaterial != null) {
			DestroyImmediate(shaderMaterial);
			shaderMaterial = null;
		}
	}

	private void OnValidate() {
		depthSensitivity = Mathf.Max(0.0f, depthSensitivity);
		normalSensitivity = Mathf.Max(0.0f, normalSensitivity);
	}

	#region Saving

	public string ID => $"{gameObject.name}_BladeEdgeDetection";

	[Serializable]
	class BladeEdgeDetectionSave {
		bool debugMode;
		bool doubleSidedEdges;
		bool checkPortalDepth;
		float depthSensitivity;
		float normalSensitivity;
		int sampleDistance;

		int weightedEdgeMode;
		float depthWeightEffect;
		float normalWeightEffect;

		int edgeColorMode;
		SerializableColor edgeColor;
		SerializableGradient edgeColorGradient;

		public BladeEdgeDetectionSave(BladeEdgeDetection edgeDetection) {
			this.debugMode = edgeDetection.debugMode;
			this.doubleSidedEdges = edgeDetection.doubleSidedEdges;
			this.checkPortalDepth = edgeDetection.checkPortalDepth;
			this.depthSensitivity = edgeDetection.depthSensitivity;
			this.normalSensitivity = edgeDetection.normalSensitivity;
			this.sampleDistance = edgeDetection.sampleDistance;
			this.weightedEdgeMode = (int)edgeDetection.weightedEdgeMode;
			this.depthWeightEffect = edgeDetection.depthWeightEffect;
			this.normalWeightEffect = edgeDetection.normalWeightEffect;
			this.edgeColorMode = (int)edgeDetection.edgeColorMode;
			this.edgeColor = edgeDetection.edgeColor;
			this.edgeColorGradient = edgeDetection.edgeColorGradient;
		}

		public void LoadSave(BladeEdgeDetection edgeDetection) {
			edgeDetection.debugMode = this.debugMode;
			edgeDetection.doubleSidedEdges = this.doubleSidedEdges;
			edgeDetection.checkPortalDepth = this.checkPortalDepth;
			edgeDetection.depthSensitivity = this.depthSensitivity;
			edgeDetection.normalSensitivity = this.normalSensitivity;
			edgeDetection.sampleDistance = this.sampleDistance;
			edgeDetection.weightedEdgeMode = (WeightedEdgeMode)this.weightedEdgeMode;
			edgeDetection.depthWeightEffect = this.depthWeightEffect;
			edgeDetection.normalWeightEffect = this.normalWeightEffect;
			edgeDetection.edgeColorMode = (EdgeColorMode)this.edgeColorMode;
			edgeDetection.edgeColor = this.edgeColor;
			edgeDetection.edgeColorGradient = this.edgeColorGradient;
		}
	}

	public object GetSaveObject() {
		return new BladeEdgeDetectionSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		BladeEdgeDetectionSave save = savedObject as BladeEdgeDetectionSave;

		save.LoadSave(this);
	}
	#endregion
}
