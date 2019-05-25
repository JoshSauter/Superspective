using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class EpitaphRenderer : MonoBehaviour {
	public const string mainColor = "_Color";

	private Renderer lazy_r;
	public Renderer r {
		get {
			if (lazy_r == null) lazy_r = GetComponent<Renderer>();
			return lazy_r;
		}
	}
	private MaterialPropertyBlock lazy_propBlock;
	MaterialPropertyBlock propBlock {
		get {
			if (lazy_propBlock == null) lazy_propBlock = new MaterialPropertyBlock();
			return lazy_propBlock;
		}
	}

	// Use this for initialization
	void Awake () {
		if (GetMaterial().HasProperty(mainColor)) {
			SetMainColor(r.material.color);
		}
	}

	public Color GetColor(string colorName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetColor(colorName);
	}
	
	public Color GetMainColor() {
		return GetColor(mainColor);
	}

	public void SetColor(string colorName, Color color) {
		if (GetMaterial().HasProperty(colorName)) {
			r.GetPropertyBlock(propBlock);
			propBlock.SetColor(colorName, color);
			r.SetPropertyBlock(propBlock);
		}
	}

	public void SetMainColor(Color color) {
		SetColor(mainColor, color);
	}

	public Material GetMaterial() {
		return r.material;
	}

	public void SetMaterial(Material newMaterial, bool keepMainColor = true) {
		Color prevColor = GetMainColor();

		r.material = newMaterial;

		if (keepMainColor) {
			SetMainColor(prevColor);
		}
	}

	public Material[] GetMaterials() {
		return r.materials;
	}

	public void SetMaterials(Material[] newMaterials) {
		r.materials = newMaterials;
	}

	public void SetInt(string propName, int value) {
		if (GetMaterial().HasProperty(propName)) {
			r.GetPropertyBlock(propBlock);
			propBlock.SetInt(propName, value);
			r.SetPropertyBlock(propBlock);
		}
	}

	public int GetInt(string propName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetInt(propName);
	}

	public Bounds GetRendererBounds() {
		return r.bounds;
	}
}
