using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BladeEdgeDetection : MonoBehaviour {
	public enum EdgeColorMode {
		simpleColor,
		gradient,
		colorRampTexture
	}
	// In debug mode, red indicates a depth-detected edge, green indicates a normal-detected edge, and yellow indicates that both checks detected an edge
	public bool debugMode = false;

	public bool doubleSidedEdges = false;
	public float depthSensitivity = 1;
	public float normalSensitivity = 1;
	public int sampleDistance = 1;

	// Edge color options
	public EdgeColorMode edgeColorMode = EdgeColorMode.simpleColor;
	public Color edgeColor = Color.black;
	public Gradient edgeColorGradient;
	public Texture2D edgeColorGradientTexture;

	Shader edgeDetectShader;
	Material shaderMaterial;

	private const int GRADIENT_ARRAY_SIZE = 10;
	
	private void OnEnable () {
		edgeDetectShader = Shader.Find("Hidden/BladeEdgeDetection");

		SetDepthNormalTextureFlag();
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
		shaderMaterial.SetFloat("_DepthSensitivity", depthSensitivity);
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

		shaderMaterial.SetFloatArray("_GradientKeyTimes", GetGradientValues(0f, edgeColorGradient.colorKeys.Select(x => x.time), 1f));
		shaderMaterial.SetColorArray("_EdgeColorGradient", GetGradientValues(startColor, edgeColorGradient.colorKeys.Select(x => x.color), endColor));
		shaderMaterial.SetFloatArray("_GradientAlphaKeyTimes", GetGradientValues(0f, edgeColorGradient.alphaKeys.Select(x => x.time), 1f));
		shaderMaterial.SetFloatArray("_AlphaGradient", GetGradientValues(startAlpha, edgeColorGradient.alphaKeys.Select(x => x.alpha), endAlpha));

		shaderMaterial.SetInt("_GradientMode", edgeColorGradient.mode == GradientMode.Blend ? 0 : 1);
	}

	private List<T> GetGradientValues<T>(T startValue, IEnumerable<T> middleValues, T endValue) {
		List<T> gradientValues = new List<T> { startValue };
		gradientValues.AddRange(middleValues);
		int numElementsToBackfill = GRADIENT_ARRAY_SIZE - gradientValues.Count;
		gradientValues.AddRange(Enumerable.Repeat(endValue, numElementsToBackfill));
		return gradientValues;
	}

	private void SetDepthNormalTextureFlag () {
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
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
}
