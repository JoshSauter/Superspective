using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class PhaseInOutMaterialAnimator : MonoBehaviour {
	public float animationTime = 2f;
	public AnimationCurve cutoffCurve;
	public IntVector2 noiseTextureSize = new IntVector2(100, 100);
	public float scale = 1.0f;
	private Texture2D perlinNoiseTex;
	private Color[] pixels;
	private Renderer thisRenderer;

	public Renderer[] otherRenderersToPhase;

	bool inPhaseInOutCoroutine = false;

	// Use this for initialization
	void Start () {
		thisRenderer = GetComponent<Renderer>();
		perlinNoiseTex = new Texture2D(noiseTextureSize.x, noiseTextureSize.y);
		perlinNoiseTex.wrapMode = TextureWrapMode.Repeat;
		pixels = new Color[perlinNoiseTex.width * perlinNoiseTex.height];
		thisRenderer.material.mainTexture = perlinNoiseTex;

		RecalculateMaterial();
	}

	public void PhaseInOut(bool phaseIn) {
		if (!inPhaseInOutCoroutine) {
			StartCoroutine(IPhaseInOut(phaseIn));
		}
	}

	void RecalculateMaterial() {
		for (int y = 0; y < perlinNoiseTex.height; y++) {
			for (int x = 0; x < perlinNoiseTex.width; x++) {
				float xCoord = scale * x / perlinNoiseTex.width;
				float yCoord = scale * y / perlinNoiseTex.height;
				float sample = Mathf.PerlinNoise(xCoord, yCoord);
				pixels[y * perlinNoiseTex.width + x] = new Color(sample, sample, sample);
			}
		}
		perlinNoiseTex.SetPixels(pixels);
		perlinNoiseTex.Apply();

		thisRenderer.material.mainTexture = perlinNoiseTex;
		foreach (Renderer anotherRenderer in otherRenderersToPhase) {
			anotherRenderer.material.mainTexture = perlinNoiseTex;
		}
	}

	IEnumerator IPhaseInOut(bool phaseIn) {
		inPhaseInOutCoroutine = true;
		
		thisRenderer.enabled = true;
		foreach (Renderer anotherRenderer in otherRenderersToPhase) {
			anotherRenderer.enabled = true;
		}

		float start = phaseIn ? 1 : 0;
		float end = 1 - start;

		float timeElapsed = 0;
		while (timeElapsed < animationTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / animationTime;
			t = Mathf.Lerp(start, end, t);
			
			thisRenderer.material.SetFloat("_Cutoff", cutoffCurve.Evaluate(t));
			foreach (Renderer anotherRenderer in otherRenderersToPhase) {
				anotherRenderer.material.SetFloat("_Cutoff", cutoffCurve.Evaluate(t));
			}

			yield return null;
		}

		thisRenderer.material.SetFloat("_Cutoff", cutoffCurve.Evaluate(end));
		thisRenderer.enabled = phaseIn;
		foreach (Renderer anotherRenderer in otherRenderersToPhase) {
			anotherRenderer.enabled = phaseIn;
		}

		inPhaseInOutCoroutine = false;
	}
}
