using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GlowComposite : MonoBehaviour {
	[Range(0, 10)]
	public float Intensity = 2;

	public RenderTexture test;
	private Material _compositeMat;

	void OnEnable() {
		_compositeMat = new Material(Shader.Find("Hidden/GlowComposite"));
		test = new RenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 24);
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		_compositeMat.SetFloat("_GlowIntensity", Intensity);
		Graphics.Blit(src, dst, _compositeMat, 0);
	}
}
