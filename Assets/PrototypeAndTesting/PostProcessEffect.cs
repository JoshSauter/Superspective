using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;

public class PostProcessEffect : MonoBehaviour {
	public int ks = 17;
	Material mat;

	// Use this for initialization
	void Awake() {
		mat = new Material(Shader.Find("SMO/Complete/Painting"));
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		mat.SetInt("_KernelSize", ks);
		Graphics.Blit(source, destination, mat);
	}
}
