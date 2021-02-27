using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Saving;
using SerializableClasses;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BladeEdgeDetection : SaveableObject<BladeEdgeDetection, BladeEdgeDetection.BladeEdgeDetectionSave> {
	public enum EdgeColorMode {
		SimpleColor,
		Gradient,
		ColorRampTexture
	}
	public enum WeightedEdgeMode {
		Unweighted,
		WeightedByDepth,
		WeightedByNormals,
		WeightedByDepthAndNormals
	}
	// In debug mode, red indicates a depth-detected edge, green indicates a normal-detected edge, and yellow indicates that both checks detected an edge
	public bool debugMode = false;

	public bool doubleSidedEdges = false;
	public bool checkPortalDepth = false;
	public float depthSensitivity = 1;
	public float normalSensitivity = 1;
	public int sampleDistance = 1;

	// Weighted edges options
	public WeightedEdgeMode weightedEdgeMode = WeightedEdgeMode.Unweighted;
	public float depthWeightEffect = 0f;
	public float normalWeightEffect = 0f;

	// Edge color options
	public EdgeColorMode edgeColorMode = EdgeColorMode.SimpleColor;
	public Color edgeColor = Color.black;
	public Gradient edgeColorGradient;
	public Texture2D edgeColorGradientTexture;

	[SerializeField]
	Shader edgeDetectShader;
	Material shaderMaterial;
	Camera thisCamera;

	const float DepthSensitivityMultiplier = 40;	// Keeps depth-sensitivity values close to normal-sensitivity values in the inspector
	const int GradientArraySize = 10;

	// Allocate once to save GC every frame
	readonly float[] floatGradientBuffer = new float[GradientArraySize];
	readonly Color[] colorGradientBuffer = new Color[GradientArraySize];

	[NonSerialized]
	Vector3[] frustumCorners;
	[NonSerialized]
	Vector4[] frustumCornersOrdered;

	static readonly int DepthSensitivityID = Shader.PropertyToID("_DepthSensitivity");
	static readonly int NormalSensitivityID = Shader.PropertyToID("_NormalSensitivity");
	static readonly int SampleDistanceID = Shader.PropertyToID("_SampleDistance");
	static readonly int ColorModeID = Shader.PropertyToID("_ColorMode");
	static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
	static readonly int DebugModeID = Shader.PropertyToID("_DebugMode");
	static readonly int GradientTextureID = Shader.PropertyToID("_GradientTexture");
	static readonly int WeightedEdgeModeID = Shader.PropertyToID("_WeightedEdgeMode");
	static readonly int DepthWeightEffectID = Shader.PropertyToID("_DepthWeightEffect");
	static readonly int NormalWeightEffectID = Shader.PropertyToID("_NormalWeightEffect");
	
	static readonly int GradientKeyTimesID = Shader.PropertyToID("_GradientKeyTimes");
	static readonly int EdgeColorGradientID = Shader.PropertyToID("_EdgeColorGradient");
	static readonly int GradientAlphaKeyTimesID = Shader.PropertyToID("_GradientAlphaKeyTimes");
	static readonly int AlphaGradientID = Shader.PropertyToID("_AlphaGradient");
	static readonly int GradientModeID = Shader.PropertyToID("_GradientMode");
	static readonly int FrustumCorners = Shader.PropertyToID("_FrustumCorners");

	void OnEnable () {
		SetDepthNormalTextureFlag();
		frustumCorners = new Vector3[4];
		frustumCornersOrdered = new Vector4[4];
	}
	
	[ImageEffectOpaque]
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (shaderMaterial == null && !CreateMaterial()) {
			Debug.LogError("Failed to create shader material!");
			Graphics.Blit(source, destination);
			this.enabled = false;
		}

		shaderMaterial.SetFloat(DebugModeID, debugMode ? 1 : 0);

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
		shaderMaterial.SetFloat(DepthSensitivityID, depthSensitivity * DepthSensitivityMultiplier * (thisCamera.farClipPlane/400));
		shaderMaterial.SetFloat(NormalSensitivityID, normalSensitivity);
		shaderMaterial.SetInt(SampleDistanceID, sampleDistance);

		shaderMaterial.SetInt(ColorModeID, (int)edgeColorMode);	
		switch (edgeColorMode) {
			case EdgeColorMode.SimpleColor:
				shaderMaterial.SetColor(EdgeColorID, edgeColor);
				break;
			case EdgeColorMode.Gradient:
				SetEdgeColorGradient();
				break;
			case EdgeColorMode.ColorRampTexture:
				shaderMaterial.SetTexture(GradientTextureID, edgeColorGradientTexture);
				break;
		}

		shaderMaterial.SetInt(WeightedEdgeModeID, (int)weightedEdgeMode);
		shaderMaterial.SetFloat(DepthWeightEffectID, depthWeightEffect);
		shaderMaterial.SetFloat(NormalWeightEffectID, normalWeightEffect);

		Graphics.Blit(source, destination, shaderMaterial);
	}

	/// <summary>
	/// Sets the _GradientKeyTimes and _EdgeColorGradient float and Color arrays, respectively, in the BladeEdgeDetectionShader
	/// Populates _GradientKeyTimes with the times of each colorKey in edgeColorGradient (as well as a 0 as the first key and a series of 1s to fill out the array at the end)
	/// Populates _EdgeColorGradient with the colors of each colorKey in edgeColorGradient (as well as values for the times filled in as described above)
	/// </summary>
	void SetEdgeColorGradient() {
		Color startColor = edgeColorGradient.Evaluate(0);
		Color endColor = edgeColorGradient.Evaluate(1);
		float startAlpha = startColor.a;
		float endAlpha = endColor.a;

		shaderMaterial.SetFloatArray(GradientKeyTimesID, GetGradientFloatValues(0f, edgeColorGradient.colorKeys.Select(x => x.time), 1f));
		shaderMaterial.SetColorArray(EdgeColorGradientID, GetGradientColorValues(startColor, edgeColorGradient.colorKeys.Select(x => x.color), endColor));
		shaderMaterial.SetFloatArray(GradientAlphaKeyTimesID, GetGradientFloatValues(0f, edgeColorGradient.alphaKeys.Select(x => x.time), 1f));
		shaderMaterial.SetFloatArray(AlphaGradientID, GetGradientFloatValues(startAlpha, edgeColorGradient.alphaKeys.Select(x => x.alpha), endAlpha));

		shaderMaterial.SetInt(GradientModeID, edgeColorGradient.mode == GradientMode.Blend ? 0 : 1);

		SetFrustumCornersVector();
	}

	void SetFrustumCornersVector() {
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
		shaderMaterial.SetVectorArray(FrustumCorners, frustumCornersOrdered);
	}

	// Actually just populates the float buffer with the values provided, then returns a reference to the float buffer
	float[] GetGradientFloatValues(float startValue, IEnumerable<float> middleValues, float endValue) {
		float[] middleValuesArray = middleValues.ToArray();
		floatGradientBuffer[0] = startValue;
		for (int i = 1; i < middleValuesArray.Length + 1; i++) {
			floatGradientBuffer[i] = middleValuesArray[i - 1];
		}
		for (int j = middleValuesArray.Length + 1; j < GradientArraySize; j++) {
			floatGradientBuffer[j] = endValue;
		}
		return floatGradientBuffer;
	}

	// Actually just populates the color buffer with the values provided, then returns a reference to the color buffer
	Color[] GetGradientColorValues(Color startValue, IEnumerable<Color> middleValues, Color endValue) {
		Color[] middleValuesArray = middleValues.ToArray();
		colorGradientBuffer[0] = startValue;
		for (int i = 1; i < middleValuesArray.Length + 1; i++) {
			colorGradientBuffer[i] = middleValuesArray[i - 1];
		}
		for (int j = middleValuesArray.Length + 1; j < GradientArraySize; j++) {
			colorGradientBuffer[j] = endValue;
		}
		return colorGradientBuffer;
	}

	void SetDepthNormalTextureFlag () {
		if (thisCamera == null) thisCamera = GetComponent<Camera>();
		thisCamera.depthTextureMode = DepthTextureMode.DepthNormals;
	}

	bool CreateMaterial() {
		if (!edgeDetectShader.isSupported) {
			return false;
		}

		shaderMaterial = new Material(edgeDetectShader) {
			hideFlags = HideFlags.HideAndDontSave
		};

		return shaderMaterial != null;
	}

	void OnDisable() {
		if (shaderMaterial != null) {
			DestroyImmediate(shaderMaterial);
			shaderMaterial = null;
		}
	}

	void OnValidate() {
		depthSensitivity = Mathf.Max(0.0f, depthSensitivity);
		normalSensitivity = Mathf.Max(0.0f, normalSensitivity);
	}

	#region Saving
	public override string ID => $"{gameObject.name}_BladeEdgeDetection";

	[Serializable]
	public class BladeEdgeDetectionSave : SerializableSaveObject<BladeEdgeDetection> {
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

		public BladeEdgeDetectionSave(BladeEdgeDetection edgeDetection) : base(edgeDetection) {
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

		public override void LoadSave(BladeEdgeDetection edgeDetection) {
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
	#endregion
}
