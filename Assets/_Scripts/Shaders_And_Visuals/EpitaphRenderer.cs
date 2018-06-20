using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class EpitaphRenderer : MonoBehaviour {
	private const string mainColor = "_Color";
	MaterialPropertyBlock propBlock;
	public Renderer r;

	// Use this for initialization
	void Awake () {
		r = GetComponent<Renderer>();
		propBlock = new MaterialPropertyBlock();
	}

	public Color GetColor(string colorName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetColor(colorName);
	}
	
	public Color GetMainColor() {
		return GetColor(mainColor);
	}

	public void SetColor(Color color, string colorName) {
		r.GetPropertyBlock(propBlock);
		propBlock.SetColor(colorName, color);
		r.SetPropertyBlock(propBlock);
	}

	public void SetMainColor(Color color) {
		SetColor(color, mainColor);
	}

	public Material GetMaterial() {
		return r.material;
	}

	public void SetMaterial(Material newMaterial, bool keepMainColor = true) {
		Color prevColor = GetMainColor();

		r.material = newMaterial;
		MaterialPropertyBlock pb = new MaterialPropertyBlock();

		if (keepMainColor) {
			r.GetPropertyBlock(propBlock);
			pb.SetColor("_Color", prevColor);
			r.SetPropertyBlock(pb);
		}
	}

	public Bounds GetRendererBounds() {
		return r.bounds;
	}
}
