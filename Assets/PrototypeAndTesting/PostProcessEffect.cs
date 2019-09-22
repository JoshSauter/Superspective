using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class PostProcessEffect : MonoBehaviour {
	public int ks = 17;
	Material mat;

	// Use this for initialization
	void Awake() {
		mat = new Material(Shader.Find("SMO/Complete/Painting"));
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination) {
		mat.SetInt("_KernelSize", ks);
		Graphics.Blit(source, destination, mat);
	}
}
