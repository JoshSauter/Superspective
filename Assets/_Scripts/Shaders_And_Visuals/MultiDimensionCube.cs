using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using Saving;
using System;
using System.Linq;
using NaughtyAttributes;
using PortalMechanics;
using StateUtils;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

[RequireComponent(typeof(UniqueId))]
public class MultiDimensionCube : SaveableObject<MultiDimensionCube, MultiDimensionCube.MultiDimensionCubeSave> {
	public enum State {
		Idle, // Same dimension or different dimension from the player (determined by PillarDimensionObject)
		Materializing // Corporeal cube dissolves in as player picks it up and brings it into player's dimension
	}
	private StateMachine<State> stateMachine;
	
	InteractableObject interactableObject;
	public PickupObject pickupCube;
	PillarDimensionObject corporealCubeDimensionObj, invertedCubeDimensionObj;
	public SuperspectiveRenderer cubeFrameRenderer, corporealGlassRenderer, invertedCubeRenderer, raymarchRenderer;

	#region Portal Copy References
	// Assumes the following hierarchy:
	// MultiDimensionCubePortalCopy
	// - CorporealCube
	// -- Glass
	// - InvertedCube
	// -- Glass (Raymarching)
	private Renderer _cubeFramePortalCopy;
	private Renderer CubeFramePortalCopy {
		get {
			if (_cubeFramePortalCopy == null) {
				_cubeFramePortalCopy = PortalableObject.PortalCopy.transform.GetChild(0).GetComponent<Renderer>();
			}

			return _cubeFramePortalCopy;
		}
	}
	
	private Renderer _corporealGlassPortalCopy;
	private Renderer CorporealGlassPortalCopy {
		get {
			if (_corporealGlassPortalCopy == null) {
				_corporealGlassPortalCopy = PortalableObject.PortalCopy.transform.GetChild(0).GetChild(0).GetComponent<Renderer>();
			}

			return _corporealGlassPortalCopy;
		}
	}
	
	private Renderer _invertedCubePortalCopy;
	private Renderer InvertedCubePortalCopy {
		get {
			if (_invertedCubePortalCopy == null) {
				_invertedCubePortalCopy = PortalableObject.PortalCopy.transform.GetChild(1).GetComponent<Renderer>();
			}

			return _invertedCubePortalCopy;
		}
	}
	
	private Renderer _raymarchPortalCopy;
	private Renderer RaymarchPortalCopy {
		get {
			if (_raymarchPortalCopy == null) {
				_raymarchPortalCopy = PortalableObject.PortalCopy.transform.GetChild(1).GetChild(0).GetComponent<Renderer>();
			}

			return _raymarchPortalCopy;
		}
	}
	
	#endregion

	Transform[] cubeTransforms;

	public BoxCollider thisCollider;
	PhysicMaterial defaultPhysicsMaterial;

	public BoxCollider kinematicCollider;
	BoxCollider detectWhenPlayerIsNearCollider;

	public const string DISSOLVE_PROPERTY_NAME = "_DissolveAmount";
	const float OUTLINE_FADE_IN_TIME = .5f;
	const float INVERT_COLORS_DELAY = 0.4f; // Should be less than OUTLINE_FADE_IN_TIME for state machine purposes
	const float INVERT_COLORS_FADE_OUT_TIME = .5f;
	private float OutlineFadeInTime => materializeMultiplier * OUTLINE_FADE_IN_TIME;
	private float InvertColorsDelay => materializeMultiplier * INVERT_COLORS_DELAY;
	private float InvertColorsFadeOutTime => materializeMultiplier * INVERT_COLORS_FADE_OUT_TIME;
	private float TotalTime => Mathf.Max(OutlineFadeInTime, InvertColorsDelay + InvertColorsFadeOutTime);

	[ReadOnly]
	public int corporealDimensionBeforeMaterialize = -1;
	[ReadOnly]
	public VisibilityState corporealVisibilityStateBeforeRenderPortal;
	[ReadOnly]
	public VisibilityState invertedVisibilityStateBeforeRenderPortal;

	PortalableObject PortalableObject => pickupCube?.portalableObject;

	// Multiplier on the animation curve to make the dissolve effect faster or slower
	[FormerlySerializedAs("materializeTime")]
	public float materializeMultiplier = 5;

	void MaterializeIfHeldInOtherDimension() {
		bool shouldMaterialize = corporealCubeDimensionObj.visibilityState != VisibilityState.Visible;
		if (PortalableObject.IsHeldThroughPortal) {
			Portal portal = PortalableObject.PortalHeldThrough;
		
			PillarDimensionObject portalDimensionObj = portal?.otherPortal?.pillarDimensionObject;
			DimensionPillar activePillar = portalDimensionObj?.activePillar;
			if (portalDimensionObj != null && activePillar != null) {
				debug.Log($"Pillar dimension: {activePillar.curDimension}, Cube dimension: {corporealCubeDimensionObj.Dimension}, Portal dimension: {portalDimensionObj.Dimension}\nFor OnPickup for {portal.name}");
				if (portalDimensionObj.Dimension == corporealCubeDimensionObj.Dimension) {
					shouldMaterialize = false;
				}
			}
		}
		
		if (shouldMaterialize) {
			Materialize();
		}
	}

	private void InitStateMachines() {
		// Corporeal state machine:
		// Set initial state
		stateMachine = this.StateMachine(State.Idle);
		
		// State transitions
		stateMachine.AddStateTransition(State.Materializing, State.Idle, TotalTime);
		
		// Triggers
		stateMachine.AddTrigger(State.Idle, () => {
			corporealDimensionBeforeMaterialize = -1;
			invertedCubeDimensionObj.Dimension = corporealCubeDimensionObj.Dimension;
			UpdateDissolveValuesCorporeal(0);
			UpdateDissolveValuesInverted(0);
		});
		stateMachine.AddTrigger(State.Materializing, () => {
			corporealDimensionBeforeMaterialize = corporealCubeDimensionObj.Dimension;
			UpdateDissolveValuesCorporeal(1);
			UpdateDissolveValuesInverted(0);
		});
		
		// Updates
		stateMachine.WithUpdate(State.Materializing, time => {
			kinematicCollider.enabled = false;

			UpdateDissolveValues();
		});
	}

	protected override void Awake() {
		base.Awake();
		
		InitStateMachines();
		
		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
		}

		Transform corporealCube = transform.Find("CorporealCube");
		Transform invertedCube = transform.Find("InvertedCube");
		pickupCube = GetComponent<PickupObject>();

		corporealCubeDimensionObj = corporealCube.GetComponent<PillarDimensionObject>();
		invertedCubeDimensionObj = invertedCube.GetComponent<PillarDimensionObject>();

		cubeFrameRenderer = corporealCube.GetComponent<SuperspectiveRenderer>();
		corporealGlassRenderer = corporealCube.Find("Glass").GetComponent<SuperspectiveRenderer>();
		invertedCubeRenderer = invertedCube.GetComponent<SuperspectiveRenderer>();
		raymarchRenderer = invertedCube.Find("Glass (Raymarching)").GetComponent<SuperspectiveRenderer>();

		thisCollider = GetComponent<BoxCollider>();
		defaultPhysicsMaterial = thisCollider.material;
		kinematicCollider = invertedCube.Find("KinematicCollider").GetComponent<BoxCollider>();
		detectWhenPlayerIsNearCollider = invertedCube.Find("DetectPlayerIsNearCollider").GetComponent<BoxCollider>();
	}

	protected override void Start() {
		base.Start();

		cubeTransforms = corporealCubeDimensionObj.transform.GetComponentsInChildrenRecursively<Transform>();

		SubscribeToEvents();

		_cubeFrameStartingLayer = CubeFramePortalCopy.gameObject.layer;
		_corporealGlassStartingLayer = CorporealGlassPortalCopy.gameObject.layer;
		_invertedCubeStartingLayer = InvertedCubePortalCopy.gameObject.layer;
		_raymarchStartingLayer = RaymarchPortalCopy.gameObject.layer;

		UpdateDissolveValuesInverted(1);
	}

	private void OnDisable() {
		UnsubscribeToEvents();
	}

	void SubscribeToEvents() {
		// Listen to cube pickup event
		pickupCube.OnPickupSimple += MaterializeIfHeldInOtherDimension;
		
		// Listen to dimension object state change event
		corporealCubeDimensionObj.OnStateChangeImmediate += HandleStateChangeImmediate;
		corporealCubeDimensionObj.OnStateChange += HandleStateChange;
		
		// Listen to portal rendering events
		VirtualPortalCamera.OnEarlyPreRenderPortal += OnEarlyPreRenderPortal;
		VirtualPortalCamera.OnPreRenderPortal += OnPreRenderPortal;
		VirtualPortalCamera.OnPostRenderPortal += OnPostRenderPortal;
	}

	void UnsubscribeToEvents() {
		pickupCube.OnPickupSimple -= MaterializeIfHeldInOtherDimension;
		
		corporealCubeDimensionObj.OnStateChangeImmediate -= HandleStateChangeImmediate;
		corporealCubeDimensionObj.OnStateChange -= HandleStateChange;
		
		VirtualPortalCamera.OnEarlyPreRenderPortal -= OnEarlyPreRenderPortal;
		VirtualPortalCamera.OnPreRenderPortal -= OnPreRenderPortal;
		VirtualPortalCamera.OnPostRenderPortal -= OnPostRenderPortal;
	}

	private bool IsVisibleFrom(Camera cam) {
		return cubeFrameRenderer.r.IsVisibleFrom(cam) ||
		       corporealGlassRenderer.r.IsVisibleFrom(cam) ||
		       invertedCubeRenderer.r.IsVisibleFrom(cam) ||
		       raymarchRenderer.r.IsVisibleFrom(cam);
	}

	private void LateUpdate() {
		if (pickupCube.isHeld && stateMachine.State == State.Idle) {
			MaterializeIfHeldInOtherDimension();
		}
		SetPortalCopyVisibility();
	}
	
	#region Portal Rendering

	// These values carry state read during OnEarlyPreRenderPortal to OnPreRenderPortal
	private int _pillarDimension;
	private int _cubeDimension;
	private int _portalDimension;

	// The original layers are used to swap between invisible and normal layers
	private int _cubeFrameStartingLayer;
	private int _corporealGlassStartingLayer;
	private int _invertedCubeStartingLayer;
	private int	_raymarchStartingLayer;
	
	// These are used to remember the layers of the portal copy before rendering a portal
	private int _cubeFrameLayerPreRender;
	private int _corporealGlassLayerPreRender;
	private int _invertedCubeLayerPreRender;
	private int _raymarchLayerPreRender;
	
	/// <summary>
	/// Collects state data before the portal renders the cube
	/// </summary>
	/// <param name="portal"></param>
	private void OnEarlyPreRenderPortal(Portal portal) {
		corporealVisibilityStateBeforeRenderPortal = corporealCubeDimensionObj.visibilityState;
		invertedVisibilityStateBeforeRenderPortal = invertedCubeDimensionObj.visibilityState;

		_cubeFrameLayerPreRender = cubeFrameRenderer.gameObject.layer;
		_corporealGlassLayerPreRender = corporealGlassRenderer.gameObject.layer;
		_invertedCubeLayerPreRender = invertedCubeRenderer.gameObject.layer;
		_raymarchLayerPreRender = raymarchRenderer.gameObject.layer;
		
		if (!IsVisibleFrom(VirtualPortalCamera.instance.portalCamera) && !CubeFramePortalCopy.IsVisibleFrom(VirtualPortalCamera.instance.portalCamera)) return;
		
		PillarDimensionObject portalDimensionObj = portal?.otherPortal?.pillarDimensionObject;
		DimensionPillar activePillar = portalDimensionObj?.activePillar;
		if (portalDimensionObj != null && activePillar != null) {
			_pillarDimension = activePillar.curDimension;
			_cubeDimension = corporealCubeDimensionObj.Dimension;
			_portalDimension = portalDimensionObj.Dimension;
		}
	}
	
	/// <summary>
	/// Updates the dissolve values of the corporeal/inverted cube and the visibility of the portal copy,
	/// based on how it should be rendered through the provided portal
	/// </summary>
	/// <param name="portal"></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	private void OnPreRenderPortal(Portal portal) {
		if (!IsVisibleFrom(VirtualPortalCamera.instance.portalCamera) && !CubeFramePortalCopy.IsVisibleFrom(VirtualPortalCamera.instance.portalCamera)) return;
		
		PillarDimensionObject portalDimensionObj = portal?.otherPortal?.pillarDimensionObject;
		DimensionPillar activePillar = portalDimensionObj?.activePillar;
		if (portalDimensionObj != null && activePillar != null) {
			switch (stateMachine.State) {
				case State.Idle:
					if (_portalDimension != _cubeDimension) {
						// Different dimension, corporeal cube should be invisible
						UpdateDissolveValuesCorporeal(1);
						UpdateDissolveValuesInverted(0);
						SetPortalCopyVisibility(false);
					}
					else {
						// Same dimension, corporeal cube should be visible
						UpdateDissolveValuesCorporeal(0);
						UpdateDissolveValuesInverted(0);
						SetPortalCopyVisibility(true);
					}
					break;
				case State.Materializing:
					corporealCubeDimensionObj.SwitchEffectiveVisibilityState(VisibilityState.Visible, true, false, true);
					invertedCubeDimensionObj.SwitchEffectiveVisibilityState(VisibilityState.Visible, true, false, true);
					debug.Log($"Pillar dimension: {_pillarDimension}, Cube dimension: {_cubeDimension}, Portal dimension: {_portalDimension}\nState: {stateMachine.State}\nEffective VisibilityState (corporeal): {corporealCubeDimensionObj.EffectiveVisibilityState}\nEffective VisibilityState (inverted): {invertedCubeDimensionObj.EffectiveVisibilityState}\nFor OnPreRenderPortal for {portal.name}");

					Assert.IsFalse(corporealDimensionBeforeMaterialize == -1, "Corporeal dimension before materialize is not set");
					// The cube was visible through the portal, play the materialize effect backwards to make it inverted over materialize duration
					if (corporealDimensionBeforeMaterialize == _portalDimension) {
						UpdateDissolveValuesReverse();
					}
					else {
						if (_cubeDimension == _portalDimension) {
							// Play materialize effect normally
							UpdateDissolveValues();
						}
						else {
							// Stay inverted
							UpdateDissolveValuesCorporeal(1);
							UpdateDissolveValuesInverted(0);
						}
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	private void OnPostRenderPortal(Portal _) {
		cubeFrameRenderer.gameObject.layer = _cubeFrameLayerPreRender;
		corporealGlassRenderer.gameObject.layer = _corporealGlassLayerPreRender;
		invertedCubeRenderer.gameObject.layer = _invertedCubeLayerPreRender;
		raymarchRenderer.gameObject.layer = _raymarchLayerPreRender;
		
		if (stateMachine == State.Materializing) {
			corporealCubeDimensionObj.SwitchVisibilityState(corporealVisibilityStateBeforeRenderPortal, true, false, true);
			invertedCubeDimensionObj.SwitchVisibilityState(invertedVisibilityStateBeforeRenderPortal, true, false, true);
		}
		UpdateDissolveValues();
		SetPortalCopyVisibility();
	}

	void SetPortalCopyVisibility() {
		SetPortalCopyVisibility(false);
		if (PortalableObject) {
			// PortalCopys of MultiDimensionCubes should swap their dimensions if the cube is inside a portal that is in a different dimension
			if (PortalableObject.IsInPortal) {
				Portal portalCubeIsIn = PortalableObject.Portal;
				// Only if the portal and its other portal are PillarDimensionObjects, and are different dimensions
				if (portalCubeIsIn.pillarDimensionObject && portalCubeIsIn.otherPortal.pillarDimensionObject &&
				    portalCubeIsIn.pillarDimensionObject.Dimension != portalCubeIsIn.otherPortal.pillarDimensionObject.Dimension) {
					// If the real corporeal cube is visible, the portal copy should be the opposite (confusing, I know)
					bool corporealCubeIsVisible = corporealCubeDimensionObj.EffectiveVisibilityState == VisibilityState.Visible;
					SetPortalCopyVisibility(corporealCubeIsVisible);
				}
			}
		}
	}

	void SetPortalCopyVisibility(bool swappedDimensions) {
		// debug.Log($"Setting portal copy visibility to {swappedDimensions}");
		if (swappedDimensions) {
			CubeFramePortalCopy.gameObject.layer = SuperspectivePhysics.InvisibleLayer;
			CorporealGlassPortalCopy.gameObject.layer = SuperspectivePhysics.InvisibleLayer;
			
			InvertedCubePortalCopy.gameObject.layer = _invertedCubeStartingLayer;
			RaymarchPortalCopy.gameObject.layer = _raymarchStartingLayer;
		}
		else {
			CubeFramePortalCopy.gameObject.layer = _cubeFrameStartingLayer;
			CorporealGlassPortalCopy.gameObject.layer = _corporealGlassStartingLayer;

			InvertedCubePortalCopy.gameObject.layer = SuperspectivePhysics.InvisibleLayer;
			RaymarchPortalCopy.gameObject.layer = SuperspectivePhysics.InvisibleLayer;
		}

	}
	#endregion

	void FixedUpdate() {
		kinematicCollider.enabled = thisCollider.enabled;
		kinematicCollider.transform.position = transform.position;
		kinematicCollider.transform.rotation = transform.rotation;
		kinematicCollider.size = thisCollider.size * 1.01f;

		kinematicCollider.enabled = detectWhenPlayerIsNearCollider.enabled = corporealCubeDimensionObj.visibilityState == VisibilityState.Invisible;
		
		// TODO: Remove this, just testing something:
		// if (DEBUG) return;

		PhysicMaterial colliderMaterial = kinematicCollider.enabled && corporealCubeDimensionObj.thisRigidbody.isKinematic ? kinematicCollider.material : defaultPhysicsMaterial;
		if (!pickupCube.isHeld && thisCollider.material != colliderMaterial) {
			debug.Log($"Collider material changed from {thisCollider.material.name} to {colliderMaterial.name}");
			thisCollider.material = colliderMaterial;
		}
	}

	void UpdateDissolveValues() {
		switch (stateMachine.State) {
			case State.Idle:
				UpdateDissolveValuesCorporeal(0);
				UpdateDissolveValuesInverted(0);
				break;
			case State.Materializing:
				float time = stateMachine.Time;
				float corporealT = Mathf.Clamp01(1 - time / OutlineFadeInTime);
				float invertedT = Mathf.Clamp01((time - InvertColorsDelay) / InvertColorsFadeOutTime);
				UpdateDissolveValuesCorporeal(corporealT);
				UpdateDissolveValuesInverted(invertedT);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	void UpdateDissolveValuesReverse() {
		switch (stateMachine.State) {
			case State.Idle:
				// Same dimension, corporeal cube should be visible
				UpdateDissolveValuesCorporeal(0);
				UpdateDissolveValuesInverted(0);
				break;
			case State.Materializing:
				float time = stateMachine.Time;

				float reverseCorporealDelay = TotalTime - OutlineFadeInTime;
				
				float corporealT = 1 - Mathf.Clamp01(1 - (time - reverseCorporealDelay) / OutlineFadeInTime);
				float invertedT = 1 - Mathf.Clamp01(time / InvertColorsFadeOutTime);
				UpdateDissolveValuesCorporeal(corporealT);
				UpdateDissolveValuesInverted(invertedT);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
	
	void UpdateDissolveValuesCorporeal(float corporealT) {
		corporealGlassRenderer.SetFloat(DISSOLVE_PROPERTY_NAME, corporealT);
		cubeFrameRenderer.SetFloat(DISSOLVE_PROPERTY_NAME, corporealT);
	}
	
	void UpdateDissolveValuesInverted(float invertedT) {
		invertedCubeRenderer.SetFloat(DISSOLVE_PROPERTY_NAME, invertedT);
		raymarchRenderer.SetFloat(DISSOLVE_PROPERTY_NAME, invertedT);
	}

	void HandleStateChangeImmediate(DimensionObject dimensionObj) {
		if (stateMachine != State.Materializing) {
			switch (dimensionObj.visibilityState) {
				case VisibilityState.Invisible:
					// Corporeal cube should be invisible, inverted cube visible
					UpdateDissolveValuesCorporeal(1);
					UpdateDissolveValuesInverted(0);
					break;
				case VisibilityState.PartiallyVisible:
				case VisibilityState.PartiallyInvisible:
					break;
				case VisibilityState.Visible:
					// Corporeal cube should be visible, inverted cube invisible (by dimension object, not dissolve value)
					UpdateDissolveValuesCorporeal(0);
					UpdateDissolveValuesInverted(0);
					break;
				default:
					break;
			}
		}
	}
	
	void HandleStateChange(DimensionObject dimensionObj) {
		if (stateMachine != State.Materializing && dimensionObj.visibilityState == VisibilityState.Invisible) {
			if (pickupCube.portalableObject.IsHeldThroughPortal) {
				Portal portalHeldThrough = pickupCube.portalableObject.PortalHeldThrough;
				if (portalHeldThrough != null && portalHeldThrough.pillarDimensionObject != null) {
					int cubeDimension = corporealCubeDimensionObj.Dimension;
					int portalDimension = portalHeldThrough.otherPortal.pillarDimensionObject.Dimension;
					// If the state changed because the cube passed through a portal, don't drop it
					if (cubeDimension == portalDimension) {
						return;
					}
				}
			}
			
			// Don't allow the players to hold inverted cubes
			pickupCube.Drop();
		}
	}

	void Materialize() {
		stateMachine.Set(State.Materializing);
		
		VisibilityState desiredState = corporealCubeDimensionObj.visibilityState.Opposite();
		int desiredDimension = corporealCubeDimensionObj.GetDimensionWhereThisObjectWouldBeInVisibilityState(v => v == desiredState);
		if (desiredDimension == -1) {
			Debug.LogError($"Could not find a dimension where {corporealCubeDimensionObj.visibilityState} becomes {desiredState}");
		}
		else {
			corporealCubeDimensionObj.Dimension = desiredDimension;
		}
	}

	#region Saving

	[Serializable]
	public class MultiDimensionCubeSave : SerializableSaveObject<MultiDimensionCube> {
		private StateMachine<State>.StateMachineSave state;

		public MultiDimensionCubeSave(MultiDimensionCube multiDimensionCube) : base(multiDimensionCube) {
			this.state = multiDimensionCube.stateMachine.ToSave();
		}

		public override void LoadSave(MultiDimensionCube multiDimensionCube) {
			multiDimensionCube.stateMachine.LoadFromSave(state);
		}
	}
	#endregion
}
