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
		if (this == null || gameObject == null || r == null) return null;

		_mpb ??= new MaterialPropertyBlock();

		try {
			r.GetPropertyBlock(_mpb);
		} catch (Exception e) {
			Debug.LogWarning($"GetPropertyBlock failed on {gameObject.name}: {e.Message}", this);
			return null;
		}

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
	public Color GetMainColor() {
		return GetColor(mainColor);
	}
	
	public Color GetColor(string colorName) {
		return GetPropBlock()?.GetColor(colorName) ?? default;
	}

	public Color GetColor(int colorId) {
		return GetPropBlock()?.GetColor(colorId) ?? default;
	}
	
	public Color[] GetColorArray(string propName) {
		Vector4[] colorVecs = GetVectorArray(propName);
		return colorVecs.Select(v => (Color)v).ToArray();
	}
	
	public Color[] GetColorArray(int propId) {
		Vector4[] colorVecs = GetVectorArray(propId);
		return colorVecs.Select(v => (Color)v).ToArray();
	}

	public float GetFloat(string propName) {
		return GetPropBlock()?.GetFloat(propName) ?? 0;
	}

	public float GetFloat(int propId) {
		return GetPropBlock()?.GetFloat(propId) ?? 0;
	}

	public int GetInt(string propName) {
		return GetPropBlock()?.GetInt(propName) ?? 0;
	}
	
	public int GetInt(int propId) {
		return GetPropBlock()?.GetInt(propId) ?? 0;
	}
	
	public float[] GetFloatArray(string propName) {
		return GetPropBlock()?.GetFloatArray(propName);
	}
	
	public float[] GetFloatArray(int propId) {
		return GetPropBlock()?.GetFloatArray(propId);
	}
	
	public Texture GetTexture(string propName) {
		return GetPropBlock()?.GetTexture(propName);
	}
	
	public Texture GetTexture(int propId) {
		return GetPropBlock()?.GetTexture(propId);
	}

	public Vector4 GetVector(string propName) {
		return GetPropBlock()?.GetVector(propName) ?? default;
	}
	
	public Vector4 GetVector(int propId) {
		return GetPropBlock()?.GetVector(propId) ?? default;
	}
	
	public Vector4[] GetVectorArray(string propName) {
		return GetPropBlock()?.GetVectorArray(propName);
	}
	
	public Vector4[] GetVectorArray(int propId) {
		return GetPropBlock()?.GetVectorArray(propId);
	}

	#endregion

	/////////////
	// Setters //
	/////////////
	#region Setters
	public void SetMainColor(Color color) {
		SetColor(mainColor, color);
	}
	
	public void SetColor(string colorName, Color color) {
		ApplySetting(mpb => mpb.SetColor(colorName, color));
	}
	
	public void SetColor(int colorId, Color color) {
		ApplySetting(mpb => mpb.SetColor(colorId, color));
	}
	
	// Convenience pass-through for SetVectorArray
	public void SetColorArray(string propName, Color[] colors) {
		Vector4[] colorVecs = colors.Select(c => (Vector4)c).ToArray();
		SetVectorArray(propName, colorVecs);
	}
	
	// Convenience pass-through for SetVectorArray
	public void SetColorArray(int propId, Color[] colors) {
		Vector4[] colorVecs = colors.Select(c => (Vector4)c).ToArray();
		SetVectorArray(propId, colorVecs);
	}

	public void SetFloat(string propName, float value) {
		ApplySetting(mpb => mpb.SetFloat(propName, value));
	}
	
	public void SetFloat(int propId, float value) {
		ApplySetting(mpb => mpb.SetFloat(propId, value));
	}

	public void SetInt(string propName, int value) {
		ApplySetting(mpb => mpb.SetInt(propName, value));
	}
	
	public void SetInt(int propId, int value) {
		ApplySetting(mpb => mpb.SetInt(propId, value));
	}
	
	public void SetFloatArray(string propName, float[] value) {
		ApplySetting(mpb => mpb.SetFloatArray(propName, value));
	}
	
	public void SetFloatArray(int propId, float[] value) {
		ApplySetting(mpb => mpb.SetFloatArray(propId, value));
	}

	public void SetBuffer(string propName, ComputeBuffer buffer) {
		ApplySetting(mpb => mpb.SetBuffer(propName, buffer));
	}
	
	public void SetBuffer(int propId, ComputeBuffer buffer) {
		ApplySetting(mpb => mpb.SetBuffer(propId, buffer));
	}

	public void SetTexture(string propName, Texture value) {
		ApplySetting(mpb => mpb.SetTexture(propName, value));
	}
	
	public void SetTexture(int propId, Texture value) {
		ApplySetting(mpb => mpb.SetTexture(propId, value));
	}
	
	public void SetVector(string propName, Vector4 v) {
		ApplySetting(mpb => mpb.SetVector(propName, v));
	}
	
	public void SetVector(int propId, Vector4 v) {
		ApplySetting(mpb => mpb.SetVector(propId, v));
	}

	public void SetVectorArray(string propName, Vector4[] v) {
		ApplySetting(mpb => mpb.SetVectorArray(propName, v));
	}
	
	public void SetVectorArray(int propId, Vector4[] v) {
		ApplySetting(mpb => mpb.SetVectorArray(propId, v));
	}

	public void SetMatrix(string propName, Matrix4x4 m) {
		ApplySetting(mpb => mpb.SetMatrix(propName, m));
	}
	
	public void SetMatrix(int propId, Matrix4x4 m) {
		ApplySetting(mpb => mpb.SetMatrix(propId, m));
	}

	private void ApplySetting(Action<MaterialPropertyBlock> setting) {
		MaterialPropertyBlock mpb = GetPropBlock();
		if (mpb == null) return;
		
		setting(mpb);
		r.SetPropertyBlock(mpb);
	}

	public Bounds GetRendererBounds() {
		return r.bounds;
	}
	#endregion

	void PrintPropBlockValue(PropBlockType pbType, string key) {
		switch (pbType) {
			case PropBlockType.Color:
				Debug.Log($"{key}: {GetPropBlock()?.GetColor(key)}");
				break;
			case PropBlockType.Float:
				Debug.Log($"{key}: {GetPropBlock()?.GetFloat(key)}");
				break;
			case PropBlockType.Int:
				Debug.Log($"{key}: {GetPropBlock()?.GetInt(key)}");
				break;
			case PropBlockType.Vector:
				Debug.Log($"{key}: {GetPropBlock()?.GetVector(key)}");
				break;
			case PropBlockType.FloatArray:
				Debug.Log($"{key}: {string.Join(", ", GetPropBlock()?.GetFloatArray(key))}");
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
