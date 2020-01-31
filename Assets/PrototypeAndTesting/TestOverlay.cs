using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TestOverlay : MonoBehaviour {
	public Shader shader;
	Material mat;

	private void OnEnable() {
		if (mat == null && shader != null) {
			mat = new Material(shader);
		}
	}

	[ImageEffectOpaque]
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (mat != null) {
			Graphics.Blit(source, destination, mat);
		}
		else {
			Graphics.Blit(source, destination);
		}
	}
}
