using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using EpitaphUtils.ShaderUtils;
using System.Linq;
using Saving;
using System;

public enum VisibilityState {
	invisible,
	partiallyVisible,
	visible,
	partiallyInvisible,
};

[RequireComponent(typeof(UniqueId))]
public class DimensionObject : SaveableObject<DimensionObject, DimensionObject.DimensionObjectSave> {
	UniqueId _id;
	public UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}

	public bool treatChildrenAsOneObjectRecursively = false;
	public bool ignoreChildrenWithDimensionObject = true;

	protected bool initialized = false;
	[Range(0, 1)]
	public int channel;
	public bool reverseVisibilityStates = false;
	public bool ignoreMaterialChanges = false;
	public bool disableColliderWhileInvisible = true;
	protected int curDimensionSetInMaterial;

	public EpitaphRenderer[] renderers;
	public Dictionary<EpitaphRenderer, Material[]> startingMaterials;
	public Dictionary<EpitaphRenderer, int> startingLayers;

	public VisibilityState startingVisibilityState = VisibilityState.visible;
	public VisibilityState visibilityState = VisibilityState.visible;
	protected static Dictionary<VisibilityState, HashSet<VisibilityState>> nextStates = new Dictionary<VisibilityState, HashSet<VisibilityState>> {
		{ VisibilityState.invisible, new HashSet<VisibilityState> { VisibilityState.partiallyVisible, VisibilityState.partiallyInvisible } },
		{ VisibilityState.partiallyVisible, new HashSet<VisibilityState> { VisibilityState.invisible, VisibilityState.visible } },
		{ VisibilityState.visible, new HashSet<VisibilityState> { VisibilityState.partiallyVisible, VisibilityState.partiallyInvisible } },
		{ VisibilityState.partiallyInvisible, new HashSet<VisibilityState> { VisibilityState.invisible, VisibilityState.visible } }
	};

#region events
	public delegate void DimensionObjectStateChangeAction(VisibilityState visibilityState);
	public event DimensionObjectStateChangeAction OnStateChange;
	#endregion

	protected override void Awake() {
		base.Awake();

		renderers = GetAllEpitaphRenderers().ToArray();
		if (renderers.Length == 0) {
			Debug.LogError("No renderers found for: " + gameObject.name, gameObject);
			enabled = false;
		}
		
		startingMaterials = GetAllStartingMaterials(renderers);
		startingLayers = GetAllStartingLayers(renderers);
	}

	protected override void Init() {
		SetChannelValuesInMaterials();

		if (!initialized && gameObject.activeInHierarchy) {
			StartCoroutine(Initialize());
		}
	}

	IEnumerator Initialize() {
		yield return new WaitUntil(() => gameObject.IsInLoadedScene());
		yield return null;
		
		SwitchVisibilityState(startingVisibilityState, true);
		initialized = true;
	}

	public void OverrideStartingMaterials(Dictionary<EpitaphRenderer, Material[]> newStartingMaterials) {
		startingMaterials = newStartingMaterials;
	}

	void OnDisable() {
		SetChannelValuesInMaterials(false);
	}

	void OnEnable() {
		SetChannelValuesInMaterials();
	}

	////////////////////////
	// State Change Logic //
	////////////////////////
	#region stateChange

	public virtual void SwitchVisibilityState(VisibilityState nextState, bool ignoreTransitionRules = false) {
		if (gameObject.activeInHierarchy) {
			StartCoroutine(SwitchVisibilityStateCoroutine(nextState, ignoreTransitionRules));
		}
	}

	public virtual IEnumerator SwitchVisibilityStateCoroutine(VisibilityState nextState, bool ignoreTransitionRules = false) {
		if (!(ignoreTransitionRules || IsValidNextState(nextState))) yield break;

		debug.Log("State transition: " + visibilityState + " --> " + nextState);

		switch (nextState) {
			case VisibilityState.invisible:
				visibilityState = VisibilityState.invisible;
				break;
			case VisibilityState.partiallyVisible:
				visibilityState = VisibilityState.partiallyVisible;
				break;
			case VisibilityState.visible:
				visibilityState = VisibilityState.visible;
				break;
			case VisibilityState.partiallyInvisible:
				visibilityState = VisibilityState.partiallyInvisible;
				break;
		}

		if (!ignoreMaterialChanges) {
			foreach (var r in renderers) {
				SetMaterials(r);
			}
			SetChannelValuesInMaterials();
		}

		// Give a frame to let new materials switch before calling state change event
		yield return null;
		OnStateChange?.Invoke(nextState);
	}

	bool IsValidNextState(VisibilityState nextState) {
		return nextStates[visibilityState].Contains(nextState);
	}
	#endregion

	///////////////////////////
	// Material Change Logic //
	///////////////////////////
	#region materials
	void SetMaterials(EpitaphRenderer renderer) {
		if (!startingMaterials.ContainsKey(renderer)) {
			startingMaterials.Add(renderer, renderer.GetMaterials());
			startingLayers.Add(renderer, renderer.gameObject.layer);
		}
		Material[] normalMaterials = startingMaterials[renderer];
		Material[] newMaterials;
		bool inverseShader = false;
		if (!reverseVisibilityStates) {
			if (visibilityState == VisibilityState.partiallyVisible) {
				newMaterials = normalMaterials.Select(GetDimensionObjectMaterial).ToArray();
			}
			else if (visibilityState == VisibilityState.partiallyInvisible) {
				newMaterials = normalMaterials.Select(GetDimensionObjectMaterial).ToArray();
				inverseShader = true;
			}
			else {
				newMaterials = normalMaterials;
			}
		}
		else {
			if (visibilityState == VisibilityState.partiallyVisible) {
				newMaterials = normalMaterials.Select(m => GetDimensionObjectMaterial(m)).ToArray();
				inverseShader = true;
			}
			else if (visibilityState == VisibilityState.partiallyInvisible) {
				newMaterials = normalMaterials.Select(m => GetDimensionObjectMaterial(m)).ToArray();
			}
			else {
				newMaterials = normalMaterials;
			}
		}

		switch (visibilityState) {
			case VisibilityState.invisible:
				renderer.gameObject.layer = reverseVisibilityStates ? startingLayers[renderer] : LayerMask.NameToLayer("Invisible");
				break;
			case VisibilityState.partiallyVisible:
				renderer.gameObject.layer = reverseVisibilityStates ? startingLayers[renderer] : LayerMask.NameToLayer("VisibleButNoPlayerCollision");
				break;
			case VisibilityState.visible:
				renderer.gameObject.layer = reverseVisibilityStates ? LayerMask.NameToLayer("Invisible") : startingLayers[renderer];
				break;
			case VisibilityState.partiallyInvisible:
				renderer.gameObject.layer = reverseVisibilityStates ? LayerMask.NameToLayer("VisibleButNoPlayerCollision") : startingLayers[renderer];
				break;
		}
		
		// Disable colliders while invisible
		if (disableColliderWhileInvisible && renderer.TryGetComponent(out Collider c)) {
			c.enabled = (!reverseVisibilityStates && visibilityState != VisibilityState.invisible) ||
			            (reverseVisibilityStates && visibilityState != VisibilityState.visible);
		}

		renderer.SetMaterials(newMaterials);
		renderer.SetInt("_Inverse", inverseShader ? 1 : 0);
	}

	void SetChannelValuesInMaterials(bool turnOn = true) {
		foreach (var r in renderers) {
			float[] buffer = r.GetFloatArray("_Channels") ?? new float[2];
			buffer[channel] = turnOn ? 1 : 0;
			r.SetFloatArray("_Channels", buffer);
		}
	}

	protected List<EpitaphRenderer> GetAllEpitaphRenderers() {
		List<EpitaphRenderer> allRenderers = new List<EpitaphRenderer>();
		if (!treatChildrenAsOneObjectRecursively) {
			EpitaphRenderer thisRenderer = GetComponent<EpitaphRenderer>();
			if (thisRenderer == null && GetComponent<Renderer>() != null) {
				thisRenderer = gameObject.AddComponent<EpitaphRenderer>();
			}
			if (thisRenderer != null) {
				allRenderers.Add(thisRenderer);
			}
		}
		else {
			SetEpitaphRenderersRecursively(transform, ref allRenderers);
		}
		return allRenderers;
	}

	void SetEpitaphRenderersRecursively(Transform parent, ref List<EpitaphRenderer> renderersSoFar) {
		// Children who have DimensionObject scripts are treated on only by their own settings
		if (parent != transform && ignoreChildrenWithDimensionObject && parent.GetComponent<DimensionObject>() != null) return;

		EpitaphRenderer thisRenderer = parent.GetComponent<EpitaphRenderer>();
		if (thisRenderer == null && parent.GetComponent<Renderer>() != null) {
			thisRenderer = parent.gameObject.AddComponent<EpitaphRenderer>();
		}

		if (thisRenderer != null) {
			renderersSoFar.Add(thisRenderer);
		}

		if (parent.childCount > 0) {
			foreach (Transform child in parent) {
				SetEpitaphRenderersRecursively(child, ref renderersSoFar);
			}
		}
	}

	Dictionary<EpitaphRenderer, Material[]> GetAllStartingMaterials(EpitaphRenderer[] renderers) {
		return renderers.ToDictionary(r => r, r => r.GetMaterials());
	}

	Dictionary<EpitaphRenderer, int> GetAllStartingLayers(EpitaphRenderer[] renderers) {
		return renderers.ToDictionary(r => r, r => r.gameObject.layer);
	}

	Material GetDimensionObjectMaterial(Material normalMaterial) {
		Material newMaterial = null;
		bool powerTrailShader = false;
		switch (normalMaterial.shader.name) {
			case "Custom/Unlit":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionObject"));
				break;
			case "Custom/UnlitDissolve":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionDissolve"));
				break;
			case "Unlit/Texture":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionUnlitTexture"));
				break;
			case "Custom/UnlitDissolveTransparent":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionDissolveTransparent"));
				break;
			case "Standard (Specular setup)":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionObjectSpecular"));
				break;
			case "TextMeshPro/Mobile/Distance Field":
				if (normalMaterial.name.Contains("LiberationSans SDF")) {
					newMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - InGameTextDimensionObject");
				}
				else {
					Debug.LogWarning("No DimensionObject font material for " + normalMaterial.name);
				}
				break;
			case "Hidden/Raymarching":
			case "Hidden/RaymarchingDissolve":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionRaymarching"));
				break;
			case "Custom/InvertColorsObject":
			case "Custom/InvertColorsObjectDissolve":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionInvertColorsObject"));
				break;
			case "Custom/Water":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionWater"));
				break;
			case "Custom/PowerTrailLight":
				powerTrailShader = true;
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionPowerTrail"));
				break;
			case "Portals/PortalMaterial":
                newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionPortalMaterial"));
                break;
			case "Custom/UnlitNoDepth":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionUnlitNoDepth"));
				break;
			default:
				debug.LogWarning("No matching dimensionObjectShader for shader " + normalMaterial.shader.name);
				break;
		}

		if (newMaterial != null && normalMaterial != null) {
			newMaterial.CopyMatchingPropertiesFromMaterial(normalMaterial);
			
			// Special case handling
			if (powerTrailShader) {
				newMaterial.SetVectorArray("_NodePositions", normalMaterial.GetVectorArray("_NodePositions"));
				newMaterial.SetFloatArray("_StartPositionIDs", normalMaterial.GetFloatArray("_StartPositionIDs"));
				newMaterial.SetFloatArray("_EndPositionIDs", normalMaterial.GetFloatArray("_EndPositionIDs"));
				newMaterial.SetFloatArray("_InterpolationValues", normalMaterial.GetFloatArray("_InterpolationValues"));
			}
		}
		return (newMaterial != null) ? newMaterial : normalMaterial;
	}
	#endregion

	#region Saving
	public override string ID => $"DimensionObject_{id.uniqueId}";

	[Serializable]
	public class DimensionObjectSave : SerializableSaveObject<DimensionObject> {
		bool treatChildrenAsOneObjectRecursively;
		bool ignoreChildrenWithDimensionObject;
		bool disableColliderWhileInvisible;

		bool initialized;
		int channel;
		bool reverseVisibilityStates;
		bool ignoreMaterialChanges;
		int curDimensionSetInMaterial;

		int startingVisibilityState;
		public int visibilityState;

		public DimensionObjectSave(DimensionObject dimensionObj) : base(dimensionObj) {
			this.treatChildrenAsOneObjectRecursively = dimensionObj.treatChildrenAsOneObjectRecursively;
			this.ignoreChildrenWithDimensionObject = dimensionObj.ignoreChildrenWithDimensionObject;
			this.disableColliderWhileInvisible = dimensionObj.disableColliderWhileInvisible;
			this.initialized = dimensionObj.initialized;
			this.channel = dimensionObj.channel;
			this.reverseVisibilityStates = dimensionObj.reverseVisibilityStates;
			this.ignoreMaterialChanges = dimensionObj.ignoreMaterialChanges;
			this.curDimensionSetInMaterial = dimensionObj.curDimensionSetInMaterial;
			this.startingVisibilityState = (int)dimensionObj.startingVisibilityState;
			this.visibilityState = (int)dimensionObj.visibilityState;
		}

		public override void LoadSave(DimensionObject dimensionObj) {
			dimensionObj.treatChildrenAsOneObjectRecursively = this.treatChildrenAsOneObjectRecursively;
			dimensionObj.ignoreChildrenWithDimensionObject = this.ignoreChildrenWithDimensionObject;
			dimensionObj.disableColliderWhileInvisible = this.disableColliderWhileInvisible;
			dimensionObj.initialized = this.initialized;
			dimensionObj.channel = this.channel;
			dimensionObj.reverseVisibilityStates = this.reverseVisibilityStates;
			dimensionObj.ignoreMaterialChanges = this.ignoreMaterialChanges;
			dimensionObj.curDimensionSetInMaterial = this.curDimensionSetInMaterial;
			dimensionObj.startingVisibilityState = (VisibilityState)this.startingVisibilityState;
			dimensionObj.visibilityState = (VisibilityState)this.visibilityState;

			dimensionObj.SwitchVisibilityState(dimensionObj.visibilityState, true);
		}
	}
	#endregion
}
