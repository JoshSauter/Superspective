using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using SuperspectiveUtils.ShaderUtils;
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
	const int NUM_CHANNELS = 16;
	public bool treatChildrenAsOneObjectRecursively = false;
	public bool ignoreChildrenWithDimensionObject = true;

	protected bool initialized = false;
	[Range(0, NUM_CHANNELS-1)]
	public int channel;
	public bool reverseVisibilityStates = false;
	public bool ignoreMaterialChanges = false;
	public bool disableColliderWhileInvisible = true;
	protected int curDimensionSetInMaterial;

	public SuperspectiveRenderer[] renderers;
	public Collider[] colliders;
	public Dictionary<SuperspectiveRenderer, Material[]> startingMaterials;
	public Dictionary<SuperspectiveRenderer, int> startingLayers;

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

		FindDefaultMaterials();
	}

	protected override void Init() {
		SetupDimensionCollisionLogic();
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

	void SetupDimensionCollisionLogic() {
		HashSet<GameObject> rigidbodies = new HashSet<GameObject>(
			renderers.SelectMany(r => r.GetComponentsInChildren<Rigidbody>().Select(rb => rb.gameObject)));
		HashSet<GameObject> colliders = new HashSet<GameObject>(
			renderers.SelectMany(r => r.GetComponentsInChildren<Collider>().Select(c => c.gameObject)));
		HashSet<GameObject> renderedObjectsWithColliderAndRigidbody = new HashSet<GameObject>(rigidbodies);
		renderedObjectsWithColliderAndRigidbody.IntersectWith(colliders);

		foreach (var objToAddCollisionLogicTo in renderedObjectsWithColliderAndRigidbody) {
			if (objToAddCollisionLogicTo.GetComponentInChildren<DimensionObjectCollisions>() == null) {
				CreateTriggerZone(objToAddCollisionLogicTo.transform);
			}
		}
	}
	
	void CreateTriggerZone(Transform parent) {
		GameObject triggerGO = new GameObject("IgnoreCollisionsTriggerZone") {
			layer = LayerMask.NameToLayer("Ignore Raycast")
		};
		triggerGO.transform.SetParent(parent, false);
		DimensionObjectCollisions collisionLogic = triggerGO.AddComponent<DimensionObjectCollisions>();
		collisionLogic.colliderOfObject = parent.GetComponent<Collider>();
		collisionLogic.rigidbodyOfObject = parent.GetComponent<Rigidbody>();
		SphereCollider trigger = triggerGO.AddComponent<SphereCollider>();
		trigger.isTrigger = true;
	}

	public void OverrideStartingMaterials(Dictionary<SuperspectiveRenderer, Material[]> newStartingMaterials) {
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
	public void FindDefaultMaterials() {
		renderers = GetAllSuperspectiveRenderers().ToArray();
		// TODO: Move this to a place that makes more sense
		colliders = transform.GetComponentsInChildrenRecursively<Collider>();
		if (renderers.Length == 0) {
			debug.LogError("No renderers found for: " + gameObject.name);
		}
		
		startingMaterials = GetAllStartingMaterials(renderers);
		startingLayers = GetAllStartingLayers(renderers);
	}
	
	void SetMaterials(SuperspectiveRenderer renderer) {
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
			float[] buffer = r.GetFloatArray("_Channels") ?? new float[NUM_CHANNELS];
			buffer[channel] = turnOn ? 1 : 0;
			r.SetFloatArray("_Channels", buffer);
		}
	}

	protected List<SuperspectiveRenderer> GetAllSuperspectiveRenderers() {
		List<SuperspectiveRenderer> allRenderers = new List<SuperspectiveRenderer>();
		if (!treatChildrenAsOneObjectRecursively) {
			SuperspectiveRenderer thisRenderer = GetComponent<SuperspectiveRenderer>();
			if (thisRenderer == null && GetComponent<Renderer>() != null) {
				thisRenderer = gameObject.AddComponent<SuperspectiveRenderer>();
			}
			if (thisRenderer != null) {
				allRenderers.Add(thisRenderer);
			}
		}
		else {
			SetSuperspectiveRenderersRecursively(transform, ref allRenderers);
		}
		return allRenderers;
	}

	void SetSuperspectiveRenderersRecursively(Transform parent, ref List<SuperspectiveRenderer> renderersSoFar) {
		// Children who have DimensionObject scripts are treated on only by their own settings
		if (parent != transform && ignoreChildrenWithDimensionObject && parent.GetComponent<DimensionObject>() != null) return;

		SuperspectiveRenderer thisRenderer = parent.GetComponent<SuperspectiveRenderer>();
		if (thisRenderer == null && parent.GetComponent<Renderer>() != null) {
			thisRenderer = parent.gameObject.AddComponent<SuperspectiveRenderer>();
		}

		if (thisRenderer != null) {
			renderersSoFar.Add(thisRenderer);
		}

		if (parent.childCount > 0) {
			foreach (Transform child in parent) {
				SetSuperspectiveRenderersRecursively(child, ref renderersSoFar);
			}
		}
	}

	Dictionary<SuperspectiveRenderer, Material[]> GetAllStartingMaterials(SuperspectiveRenderer[] renderers) {
		return renderers.ToDictionary(r => r, r => r.GetMaterials());
	}

	Dictionary<SuperspectiveRenderer, int> GetAllStartingLayers(SuperspectiveRenderer[] renderers) {
		return renderers.ToDictionary(r => r, r => r.gameObject.layer);
	}

	Material GetDimensionObjectMaterial(Material normalMaterial) {
		Material newMaterial = null;
		bool powerTrailShader = false;
		switch (normalMaterial.shader.name) {
			case "Custom/Unlit":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionObject"));
				break;
			case "Custom/UnlitTransparent":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionUnlitTransparent"));
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
