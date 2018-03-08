using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BladeEdgeDetection : MonoBehaviour {
	public bool debugMode = false;
	public float depthSensitivity = 1;
	public float normalSensitivity = 1;
	public int sampleDistance = 1;
	public Color edgeColor = Color.black;

	Shader edgeDetectShader;
	Material shaderMaterial;
	
	private void OnEnable () {
		edgeDetectShader = Resources.Load<Shader>("Shaders/BladeEdgeDetection");

		SetDepthNormalTextureFlag();
	}
	
	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (shaderMaterial == null && !CreateMaterial()) {
			Debug.LogError("Failed to create shader material!");
			Graphics.Blit(source, destination);
			this.enabled = false;
		}

		shaderMaterial.SetFloat("_DepthSensitivity", depthSensitivity);
		shaderMaterial.SetFloat("_NormalSensitivity", normalSensitivity);
		shaderMaterial.SetColor("_EdgeColor", edgeColor);
		shaderMaterial.SetFloat("_DebugMode", debugMode ? 1 : 0);
		shaderMaterial.SetInt("_SampleDistance", sampleDistance);
		Graphics.Blit(source, destination, shaderMaterial);
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
