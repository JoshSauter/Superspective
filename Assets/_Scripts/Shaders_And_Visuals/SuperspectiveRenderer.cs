using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(Renderer))]
public class SuperspectiveRenderer : MonoBehaviour {
	public enum PropBlockType {
		Color,
		Float,
		Int,
		Vector,
		FloatArray,
		ColorOnMaterial,
		Texture
	}
	[Button("Print Property Block Value")]
	void PrintLookupValueCallback() {
		PrintPropBlockValue(lookupType, lookupString);
	}
	public bool printLookupValue = false;
	public PropBlockType lookupType = PropBlockType.Int;
	public string lookupString = "Look up anything";
	public const string mainColor = "_Color";
	public const string emissionColor = "_EmissionColor";

	Renderer lazy_r;
	public Renderer r {
		get {
			if (lazy_r == null) lazy_r = GetComponent<Renderer>();
			return lazy_r;
		}
	}

	MaterialPropertyBlock lazy_propBlock;
	MaterialPropertyBlock propBlock {
		get {
			if (lazy_propBlock == null) lazy_propBlock = new MaterialPropertyBlock();
			return lazy_propBlock;
		}
	}

	// SuperspectiveRenderer.enabled = false will disable the underlying renderer
	public new bool enabled {
		get => r.enabled;
		set => r.enabled = value;
	}

	// Use this for initialization
	void Awake () {
		List<string> propertiesToCheckForOnAwake = new List<string>() { mainColor, emissionColor };
		foreach (string prop in propertiesToCheckForOnAwake) {
			if (GetMaterial().HasProperty(prop)) {
				SetColor(prop, r.material.GetColor(prop));
			}
		}
	}

	///////////////
	// Materials //
	///////////////
	#region Materials
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
	#endregion

	/////////////
	// Getters //
	/////////////
	#region Getters
	public Color GetColor(string colorName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetColor(colorName);
	}
	
	public Color GetMainColor() {
		return GetColor(mainColor);
	}

	public float GetFloat(string propName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetFloat(propName);
	}

	public int GetInt(string propName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetInt(propName);
	}
	
	public float[] GetFloatArray(string propName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetFloatArray(propName);
	}

	public Texture GetTexture(string propName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetTexture(propName);
	}

	public Vector4 GetVector(string propName) {
		r.GetPropertyBlock(propBlock);
		return propBlock.GetVector(propName);
	}

	#endregion

	/////////////
	// Setters //
	/////////////
	#region Setters
	public void SetColor(string colorName, Color color) {
		r.GetPropertyBlock(propBlock);
		propBlock.SetColor(colorName, color);
		r.SetPropertyBlock(propBlock);
	}

	public void SetMainColor(Color color) {
		SetColor(mainColor, color);
	}

	public void SetFloat(string propName, float value) {
		r.GetPropertyBlock(propBlock);
		propBlock.SetFloat(propName, value);
		r.SetPropertyBlock(propBlock);
	}

	public void SetInt(string propName, int value) {
		r.GetPropertyBlock(propBlock);
		propBlock.SetInt(propName, value);
		r.SetPropertyBlock(propBlock);
	}
	
	public void SetFloatArray(string propName, float[] value) {
		r.GetPropertyBlock(propBlock);
		propBlock.SetFloatArray(propName, value);
		r.SetPropertyBlock(propBlock);
	}

	public void SetTexture(string propName, Texture value) {
		r.GetPropertyBlock(propBlock);
		propBlock.SetTexture(propName, value);
		r.SetPropertyBlock(propBlock);
	}
	
	public void SetVector(string propName, Vector4 v) {
		r.GetPropertyBlock(propBlock);
		propBlock.SetVector(propName, v);
		r.SetPropertyBlock(propBlock);
	}

	public Bounds GetRendererBounds() {
		return r.bounds;
	}
	#endregion

	void PrintPropBlockValue(PropBlockType pbType, string key) {
		switch (pbType) {
			case PropBlockType.Color:
				Debug.Log($"{key}: {propBlock.GetColor(key)}");
				break;
			case PropBlockType.Float:
				Debug.Log($"{key}: {propBlock.GetFloat(key)}");
				break;
			case PropBlockType.Int:
				Debug.Log($"{key}: {propBlock.GetInt(key)}");
				break;
			case PropBlockType.Vector:
				Debug.Log($"{key}: {propBlock.GetVector(key)}");
				break;
			case PropBlockType.FloatArray:
				Debug.Log($"{key}: {string.Join(", ", propBlock.GetFloatArray(key))}");
				break;
			case PropBlockType.ColorOnMaterial:
				Debug.Log($"{key}: {string.Join(", ", r.material.GetColor(key))}");
				break;
		}
	}
}
