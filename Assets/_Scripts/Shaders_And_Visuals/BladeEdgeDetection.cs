using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeEdgeDetection : MonoBehaviour {
	public bool debugMode = false;
	public float depthSensitivity = 1;
	public float normalSensitivity = 1;
	public int sampleDistance = 1;
	public Color edgeColor = Color.black;

	Shader edgeDetectShader;
	Material shaderMaterial;
	
	void Awake () {
		edgeDetectShader = Resources.Load<Shader>("Shaders/BladeEdgeDetection");
		shaderMaterial = new Material(edgeDetectShader);

		Camera.main.depthTextureMode = DepthTextureMode.DepthNormals;
	}
	
	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (shaderMaterial == null) {
			Debug.LogError("Shader material not yet set!");
			this.enabled = false;
		}

		shaderMaterial.SetFloat("_DepthSensitivity", depthSensitivity);
		shaderMaterial.SetFloat("_NormalSensitivity", normalSensitivity);
		shaderMaterial.SetColor("_EdgeColor", edgeColor);
		shaderMaterial.SetFloat("_DebugMode", debugMode ? 1 : 0);
		shaderMaterial.SetInt("_SampleDistance", sampleDistance);
		Graphics.Blit(source, destination, shaderMaterial);
	}
}
