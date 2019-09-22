using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MultipassTest : MonoBehaviour {
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
			mat.SetInt("_Vertical", 0);
			mat.SetColor("_StripeColor", Color.black);
			Graphics.Blit(source, destination, mat, 0);
			mat.SetInt("_Vertical", 1);
			mat.SetColor("_StripeColor", Color.white);
			Graphics.Blit(source, destination, mat, 0);
		}
		else {
			Graphics.Blit(source, destination);
		}
	}
}
