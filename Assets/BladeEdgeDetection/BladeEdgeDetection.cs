using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Saving;
using SerializableClasses;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BladeEdgeDetection : SuperspectiveObject<BladeEdgeDetection, BladeEdgeDetection.BladeEdgeDetectionSave> {
	public enum EdgeColorMode : byte {
		SimpleColor,
		Gradient,
		ColorRampTexture
	}
	public enum WeightedEdgeMode : byte {
		Unweighted,
		WeightedByDepth,
		WeightedByNormals,
		WeightedByDepthAndNormals
	}
	// In debug mode, red indicates a depth-detected edge, green indicates a normal-detected edge, and yellow indicates that both checks detected an edge
	public bool debugMode = false;

	public bool doubleSidedEdges => Settings.Video.DoubleThickEdges;
	public bool checkPortalDepth = false;
	public float depthSensitivity = 1;
	public float normalSensitivity = 1;
	public int sampleDistance = 1;

	// Weighted edges options
	public WeightedEdgeMode weightedEdgeMode = WeightedEdgeMode.Unweighted;
	public float depthWeightEffect = 0f;
	public float normalWeightEffect = 0f;
	public float depthWeightMin = 0f;
	public float normalWeightMin = 0f;

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
	public const int GradientArraySize = 10;

	// Allocate once to save GC every frame
	readonly float[] floatGradientBuffer = new float[GradientArraySize];
	readonly Color[] colorGradientBuffer = new Color[GradientArraySize];

	[NonSerialized]
	public Vector3[] frustumCorners;
	[NonSerialized]
	public Vector4[] frustumCornersOrdered;

	public delegate void EdgeDetectRenderAction();

	public event EdgeDetectRenderAction BeforeRenderEdgeDetection;

	static readonly int DepthSensitivityID = Shader.PropertyToID("_DepthSensitivity");
	static readonly int NormalSensitivityID = Shader.PropertyToID("_NormalSensitivity");
	static readonly int SampleDistanceID = Shader.PropertyToID("_SampleDistance");
	public static readonly int ColorModeID = Shader.PropertyToID("_ColorMode");
	public static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
	static readonly int DebugModeID = Shader.PropertyToID("_DebugMode");
	public static readonly int GradientTextureID = Shader.PropertyToID("_GradientTexture");
	static readonly int WeightedEdgeModeID = Shader.PropertyToID("_WeightedEdgeMode");
	static readonly int DepthWeightEffectID = Shader.PropertyToID("_DepthWeightEffect");
	static readonly int NormalWeightEffectID = Shader.PropertyToID("_NormalWeightEffect");
	static readonly int DepthWeightMinID = Shader.PropertyToID("_DepthWeightMin");
	static readonly int NormalWeightMinID = Shader.PropertyToID("_NormalWeightMin");
	
	public static readonly int GradientKeyTimesID = Shader.PropertyToID("_GradientKeyTimes");
	public static readonly int EdgeColorGradientID = Shader.PropertyToID("_EdgeColorGradient");
	public static readonly int GradientAlphaKeyTimesID = Shader.PropertyToID("_GradientAlphaKeyTimes");
	public static readonly int AlphaGradientID = Shader.PropertyToID("_AlphaGradient");
	public static readonly int GradientModeID = Shader.PropertyToID("_GradientMode");
	public static readonly int FrustumCorners = Shader.PropertyToID("_FrustumCorners");

	protected override void OnEnable () {
		base.OnEnable();
		SetDepthNormalTextureFlag();
		frustumCorners = new Vector3[4];
		frustumCornersOrdered = new Vector4[4];
	}
	
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (shaderMaterial == null && !CreateMaterial()) {
			Debug.LogError("Failed to create shader material!");
			Graphics.Blit(source, destination);
			this.enabled = false;
		}

		BeforeRenderEdgeDetection?.Invoke();

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
		shaderMaterial.SetFloat(DepthWeightMinID, depthWeightMin);
		shaderMaterial.SetFloat(NormalWeightMinID, normalWeightMin);
		
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

	protected override void OnDisable() {
		base.OnDisable();
		if (shaderMaterial != null) {
			DestroyImmediate(shaderMaterial);
			shaderMaterial = null;
		}
	}

	protected override void OnValidate() {
		base.OnValidate();
		depthSensitivity = Mathf.Max(0.0f, depthSensitivity);
		normalSensitivity = Mathf.Max(0.0f, normalSensitivity);
	}

#region Saving

	public override void LoadSave(BladeEdgeDetectionSave save) {
		debugMode = save.debugMode;
		checkPortalDepth = save.checkPortalDepth;
		depthSensitivity = save.depthSensitivity;
		normalSensitivity = save.normalSensitivity;
		sampleDistance = save.sampleDistance;
		weightedEdgeMode = save.weightedEdgeMode;
		depthWeightEffect = save.depthWeightEffect;
		normalWeightEffect = save.normalWeightEffect;
		depthWeightMin = save.depthWeightMin;
		normalWeightMin = save.normalWeightMin;
		edgeColorMode = save.edgeColorMode;
		edgeColor = save.edgeColor;
		edgeColorGradient = save.edgeColorGradient;
	}

	public override string ID => $"{gameObject.name}_BladeEdgeDetection";

	[Serializable]
	public class BladeEdgeDetectionSave : SaveObject<BladeEdgeDetection> {
		public readonly SerializableColor edgeColor;
		public readonly SerializableGradient edgeColorGradient;
		public readonly float depthWeightEffect;
		public readonly float normalWeightEffect;
		public readonly float depthSensitivity;
		public readonly float normalSensitivity;
		public readonly float depthWeightMin;
		public readonly float normalWeightMin;
		public readonly int sampleDistance;
		public readonly WeightedEdgeMode weightedEdgeMode;
		public readonly EdgeColorMode edgeColorMode;
		public readonly bool debugMode;
		public readonly bool checkPortalDepth;

		public BladeEdgeDetectionSave(BladeEdgeDetection edgeDetection) : base(edgeDetection) {
			this.debugMode = edgeDetection.debugMode;
			this.checkPortalDepth = edgeDetection.checkPortalDepth;
			this.depthSensitivity = edgeDetection.depthSensitivity;
			this.normalSensitivity = edgeDetection.normalSensitivity;
			this.sampleDistance = edgeDetection.sampleDistance;
			this.weightedEdgeMode = edgeDetection.weightedEdgeMode;
			this.depthWeightEffect = edgeDetection.depthWeightEffect;
			this.normalWeightEffect = edgeDetection.normalWeightEffect;
			this.edgeColorMode = edgeDetection.edgeColorMode;
			this.edgeColor = edgeDetection.edgeColor;
			this.edgeColorGradient = edgeDetection.edgeColorGradient;
			this.depthWeightMin = edgeDetection.depthWeightMin;
			this.normalWeightMin = edgeDetection.normalWeightMin;
		}
	}
#endregion
}
