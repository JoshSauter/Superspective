using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class Fog : MonoBehaviour {
	public Texture fogColorRamp;
	public float fogStartDistance = 0;
	public float fogEndDistance = 1000;
	public float fogDensity = 1;
	[Range(1, 8)]
	public int fogExponentiality = 2;

	Shader fogShader;
	Material shaderMaterial;
	Camera cam;

	private void OnEnable() {
		fogShader = Resources.Load<Shader>("Shaders/ColoredFog");
		cam = GetComponent<Camera>();
	}


	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (shaderMaterial == null && !CreateMaterial()) {
			Debug.LogError("Failed to create shader material!");
			Graphics.Blit(source, destination);
			this.enabled = false;
		}

		float camDepth = (cam.farClipPlane - cam.nearClipPlane);
		shaderMaterial.SetFloat("_FogStartDistance", fogStartDistance / camDepth);
		shaderMaterial.SetFloat("_FogEndDistance", fogEndDistance / camDepth);
		shaderMaterial.SetFloat("_FogDensity", fogDensity);
		shaderMaterial.SetInt("_FogExponent", fogExponentiality);
		shaderMaterial.SetMatrix("_FrustumCornersWS", frustumCornersMatrix);
		if (fogColorRamp != null) {
			shaderMaterial.SetTexture("_FogColorRamp", fogColorRamp);
		}
		Graphics.Blit(source, destination, shaderMaterial);
	}

	private Matrix4x4 frustumCornersMatrix {
		get {
			Vector3[] frustumCorners = new Vector3[4];
			Matrix4x4 returnMatrix = Matrix4x4.identity;
			cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, cam.stereoActiveEye, frustumCorners);
			for (int i = 0; i < 4; i++) {
				returnMatrix.SetRow(i, cam.transform.TransformVector(frustumCorners[i]));
			}

			return returnMatrix;
		}
	}

	private void SetDepthTextureFlag() {
		Camera cam = GetComponent<Camera>();
		if (cam.depthTextureMode == DepthTextureMode.None) {
			cam.depthTextureMode = DepthTextureMode.Depth;
		}
	}

	private bool CreateMaterial() {
		if (!fogShader.isSupported) {
			return false;
		}
		shaderMaterial = new Material(fogShader);
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
