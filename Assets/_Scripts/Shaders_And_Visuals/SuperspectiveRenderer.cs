using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DimensionObjectMechanics;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(Renderer))]
public class SuperspectiveRenderer : MonoBehaviour {
#region Events

	public delegate void MaterialChangedAction(Material newMaterial);
	public delegate void MaterialsChangedAction(Material[] newMaterials);
	
	public MaterialChangedAction OnMaterialChanged;
	public MaterialsChangedAction OnMaterialsChanged;
#endregion
	
	
	public enum PropBlockType {
		ShaderKeyword,
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

	private MaterialPropertyBlock _mpb;
	private MaterialPropertyBlock GetPropBlock() {
		_mpb ??= new MaterialPropertyBlock();
		r.GetPropertyBlock(_mpb);
		return _mpb;
	}

	// SuperspectiveRenderer.enabled = false will disable the underlying renderer
	public new bool enabled {
		get => r.enabled;
		set => r.enabled = value;
	}

	// Try to initialize during Awake. If it fails, try again during Start.
	void Awake() => Initialize();
	void Start() => Initialize();

	private bool hasInitialized = false;
	void Initialize() {
		if (hasInitialized) return;

		hasInitialized = true;
		try {
			List<string> propertiesToCheckForOnAwake = new List<string>() { mainColor, emissionColor };
			foreach (string prop in propertiesToCheckForOnAwake) {
				if (GetMaterial().HasProperty(prop)) {
					SetColor(prop, GetMaterial().GetColor(prop));
				}
			}
		}
		catch (Exception e) {
			hasInitialized = false;
		}
		
	}

	///////////////
	// Materials //
	///////////////
	#region Materials
	public Material GetMaterial() {
		return r.sharedMaterial;
	}

	public void SetSharedMaterial(Material newMaterial, bool keepMainColor = true) {
		Color prevColor = GetMainColor();

		r.sharedMaterial = newMaterial;

		if (keepMainColor) {
			SetMainColor(prevColor);
		}
		
		OnMaterialChanged?.Invoke(newMaterial);
		OnMaterialsChanged?.Invoke(new Material[] {newMaterial});
	}

	public void SetSharedMaterials(Material[] newMaterials, bool keepMainColor = true) {
		Color prevColor = GetMainColor();

		r.sharedMaterials = newMaterials;

		if (keepMainColor) {
			SetMainColor(prevColor);
		}
		
		OnMaterialsChanged?.Invoke(newMaterials);
		if (newMaterials.Length == 1) {
			OnMaterialChanged?.Invoke(newMaterials[0]);
		}
	}

	public Material[] GetSharedMaterials() {
		// Drop null materials
		return r.sharedMaterials.Where(m => m).ToArray();
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
		return GetPropBlock().GetColor(colorName);
	}
	
	public Color GetMainColor() {
		return GetColor(mainColor);
	}

	public float GetFloat(string propName) {
		return GetPropBlock().GetFloat(propName);
	}

	public int GetInt(string propName) {
		return GetPropBlock().GetInt(propName);
	}
	
	public float[] GetFloatArray(string propName) {
		return GetPropBlock().GetFloatArray(propName);
	}
	
	public Texture GetTexture(string propName) {
		return GetPropBlock().GetTexture(propName);
	}

	public Vector4 GetVector(string propName) {
		return GetPropBlock().GetVector(propName);
	}

	#endregion

	/////////////
	// Setters //
	/////////////
	#region Setters
	public void SetColor(string colorName, Color color) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetColor(colorName, color);
		SetPropBlock(mpb);
	}

	public void SetMainColor(Color color) {
		SetColor(mainColor, color);
	}

	public void SetFloat(string propName, float value) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetFloat(propName, value);
		SetPropBlock(mpb);
	}

	public void SetInt(string propName, int value) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetInt(propName, value);
		SetPropBlock(mpb);
	}
	
	public void SetFloatArray(string propName, float[] value) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetFloatArray(propName, value);
		SetPropBlock(mpb);
	}

	public void SetBuffer(string propName, ComputeBuffer buffer) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetBuffer(propName, buffer);
		SetPropBlock(mpb);
	}

	public void SetTexture(string propName, Texture value) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetTexture(propName, value);
		SetPropBlock(mpb);
	}
	
	public void SetVector(string propName, Vector4 v) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetVector(propName, v);
		SetPropBlock(mpb);
	}

	public void SetMatrix(string propName, Matrix4x4 m) {
		MaterialPropertyBlock mpb = GetPropBlock();
		mpb.SetMatrix(propName, m);
		SetPropBlock(mpb);
	}
	
	private void SetPropBlock(MaterialPropertyBlock mpb) {
		r.SetPropertyBlock(mpb);
	}

	public Bounds GetRendererBounds() {
		return r.bounds;
	}
	#endregion

	void PrintPropBlockValue(PropBlockType pbType, string key) {
		switch (pbType) {
			case PropBlockType.Color:
				Debug.Log($"{key}: {GetPropBlock().GetColor(key)}");
				break;
			case PropBlockType.Float:
				Debug.Log($"{key}: {GetPropBlock().GetFloat(key)}");
				break;
			case PropBlockType.Int:
				Debug.Log($"{key}: {GetPropBlock().GetInt(key)}");
				break;
			case PropBlockType.Vector:
				Debug.Log($"{key}: {GetPropBlock().GetVector(key)}");
				break;
			case PropBlockType.FloatArray:
				Debug.Log($"{key}: {string.Join(", ", GetPropBlock().GetFloatArray(key))}");
				break;
			case PropBlockType.ColorOnMaterial:
				Debug.Log($"{key}: {string.Join(", ", r.material.GetColor(key))}");
				break;
			case PropBlockType.ShaderKeyword:
				Debug.Log($"{key}: {r.material.IsKeywordEnabled(key)}");
				break;
		}
	}
	
	// Don't tell anyone I put this here
	[Button("Print Dimension Object Mask Info")]
	public void DebugPrintDimensionObjectMaskInfo() {
		ListHashSet<DimensionObject> dimensionObjectsAffecting = DimensionObjectManager.instance.GetDimensionObjectsAffectingRenderer(this);
		string msg = $"DimensionObjects affecting: {string.Join(", ", dimensionObjectsAffecting)}\n{DimensionObjectManager.instance.GetDimensionObjectBitmask(this).DebugPrettyPrint()}";
		Debug.Log(msg, this);
	}
}
