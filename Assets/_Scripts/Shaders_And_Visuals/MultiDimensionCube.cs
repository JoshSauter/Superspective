using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using Saving;
using System;

[RequireComponent(typeof(UniqueId))]
public class MultiDimensionCube : SaveableObject<MultiDimensionCube, MultiDimensionCube.MultiDimensionCubeSave> {
	public enum State {
		Materialized,
		Materializing
	}

	State _state;
	public State state {
		get { return _state; }
		set {
			if (_state == value) {
				return;
			}

			timeSinceStateChange = 0f;
			switch (value) {
				case State.Materialized:
					ResetMaterialsThisDimension();
					break;
				case State.Materializing:
					ResetMaterialsOtherDimensions();
					break;
				default:
					break;
			}

			_state = value;
		}
	}
	float timeSinceStateChange = 0f;
	InteractableObject interactableObject;
	public PickupObject pickupCube;
	PillarDimensionObject corporealCubeDimensionObj, invertedCubeDimensionObj;
	SuperspectiveRenderer cubeFrameRenderer, corporealGlassRenderer, invertedCubeRenderer, raymarchRenderer;

	Transform[] cubeTransforms;

	public BoxCollider thisCollider;
	PhysicMaterial defaultPhysicsMaterial;

	public BoxCollider kinematicCollider;
	BoxCollider detectWhenPlayerIsNearCollider;

	const string defaultLayerName = "Default";
	const string propName = "_DissolveValue";
	const float outlineFadeInTime = .5f;
	const float invertColorsDelay = 0.4f;
	const float invertColorsFadeOutTime = .5f;

	public void OnPickup() {
		if (corporealCubeDimensionObj.visibilityState != VisibilityState.visible) {
			Materialize();
		}
	}

	protected override void Awake() {
		base.Awake();
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

		DynamicObjectManager.OnDynamicObjectCreated += SetUniqueIdsUponCreation;
		corporealCubeDimensionObj.uniqueId.uniqueId = $"CorporealCube_{ID}";
		invertedCubeDimensionObj.uniqueId.uniqueId = $"InvertedCube_{ID}";
	}

	protected override void OnDestroy() {
		base.OnDestroy();
		DynamicObjectManager.OnDynamicObjectCreated -= SetUniqueIdsUponCreation;
	}

	void SetUniqueIdsUponCreation(string id) {
		if (id == this.id.uniqueId) {
			corporealCubeDimensionObj.uniqueId.uniqueId = $"CorporealCube_{ID}";
			invertedCubeDimensionObj.uniqueId.uniqueId = $"InvertedCube_{ID}";
		}
	}

	protected override void Start() {
		corporealCubeDimensionObj.uniqueId.uniqueId = $"CorporealCube_{ID}";
		invertedCubeDimensionObj.uniqueId.uniqueId = $"InvertedCube_{ID}";
		base.Start();
		
		pickupCube.OnPickupSimple += OnPickup;

		cubeTransforms = corporealCubeDimensionObj.transform.GetComponentsInChildrenRecursively<Transform>();

		corporealCubeDimensionObj.OnStateChangeSimple += HandleStateChange;
	}

	void FixedUpdate() {
		kinematicCollider.enabled = thisCollider.enabled;
		kinematicCollider.transform.position = transform.position;
		kinematicCollider.transform.rotation = transform.rotation;
		kinematicCollider.size = thisCollider.size * 1.01f;

		kinematicCollider.enabled = detectWhenPlayerIsNearCollider.enabled = corporealCubeDimensionObj.visibilityState == VisibilityState.invisible;

		thisCollider.material = corporealCubeDimensionObj.thisRigidbody.isKinematic ? kinematicCollider.material : defaultPhysicsMaterial;
	}

    void Update() {
		timeSinceStateChange += Time.deltaTime;
		switch (state) {
			case State.Materialized:
				break;
			case State.Materializing:
				kinematicCollider.enabled = false;

				float maxTime = Mathf.Max(outlineFadeInTime, invertColorsDelay) + invertColorsFadeOutTime;

				if (timeSinceStateChange < maxTime) {
					if (timeSinceStateChange < outlineFadeInTime) {
						float t1 = timeSinceStateChange / outlineFadeInTime;
						corporealGlassRenderer.SetFloat(propName, 1 - t1);
						cubeFrameRenderer.SetFloat(propName, 1 - t1);
					}
					else {
						corporealGlassRenderer.SetFloat(propName, 0);
						cubeFrameRenderer.SetFloat(propName, 0);
					}

					if (timeSinceStateChange > invertColorsDelay) {
						float t2 = (timeSinceStateChange - invertColorsDelay) / invertColorsFadeOutTime;
						invertedCubeRenderer.SetFloat(propName, t2);
						raymarchRenderer.SetFloat(propName, t2);
					}
				}
				else {
					invertedCubeDimensionObj.Dimension = corporealCubeDimensionObj.Dimension;
					state = State.Materialized;
				}
				break;
			default:
				break;
		}
	}

	void HandleStateChange(VisibilityState newState) {
		if (state != State.Materializing) {
			switch (newState) {
				case VisibilityState.invisible:
					// Corporeal cube should be invisible, inverted cube visible
					SetDissolveValuesForVisibility(corporealCubeVisible: false, invertedCubeVisible: true);

					// Don't allow the players to hold inverted cubes
					pickupCube.Drop();
					break;
				case VisibilityState.partiallyVisible:
				case VisibilityState.partiallyInvisible:
					// Both should be visible, visibility now controlled by dimension shaders
					SetDissolveValuesForVisibility(corporealCubeVisible: true, invertedCubeVisible: true);
					break;
				case VisibilityState.visible:
					// Corporeal cube should be visible, inverted cube invisible
					SetDissolveValuesForVisibility(corporealCubeVisible: true, invertedCubeVisible: false);
					break;
				default:
					break;
			}
		}
	}

	void SetDissolveValuesForVisibility(bool corporealCubeVisible, bool invertedCubeVisible) {
		cubeFrameRenderer.SetFloat(propName, corporealCubeVisible ? 0 : 1);
		corporealGlassRenderer.SetFloat(propName, corporealCubeVisible ? 0 : 1);
		invertedCubeRenderer.SetFloat(propName, invertedCubeVisible ? 0 : 1);
		raymarchRenderer.SetFloat(propName, invertedCubeVisible ? 0 : 1);
	}

	void Materialize() {
		VisibilityState desiredState = (VisibilityState)(((int)corporealCubeDimensionObj.visibilityState + 2) % 4);
		int desiredDimension = corporealCubeDimensionObj.GetDimensionWhereThisObjectWouldBeInVisibilityState(v => v == desiredState);
		if (desiredDimension == -1) {
			Debug.LogError($"Could not find a dimension where {corporealCubeDimensionObj.visibilityState} becomes {desiredState}");
		}
		else {
			corporealCubeDimensionObj.Dimension = desiredDimension;
		}

		ResetMaterialsOtherDimensions();
		state = State.Materializing;
	}

	void ResetMaterialsThisDimension() {
		kinematicCollider.enabled = false;
		if (pickupCube.thisRigidbody != null) {
			pickupCube.thisRigidbody.isKinematic = false;
		}

		// Always keep these materials visible
		foreach (var t in cubeTransforms) {
			t.gameObject.layer = LayerMask.NameToLayer(defaultLayerName);
		}

		cubeFrameRenderer.SetFloat(propName, 0);
		corporealGlassRenderer.SetFloat(propName, 0);
		invertedCubeRenderer.SetFloat(propName, 1);
		raymarchRenderer.SetFloat(propName, 1);
	}

	void ResetMaterialsOtherDimensions() {
		kinematicCollider.enabled = true;

		// Always keep these materials visible
		foreach (var t in cubeTransforms) {
			t.gameObject.layer = LayerMask.NameToLayer(defaultLayerName);
		}

		cubeFrameRenderer.SetFloat(propName, 1);
		corporealGlassRenderer.SetFloat(propName, 1);
		invertedCubeRenderer.SetFloat(propName, 0);
		raymarchRenderer.SetFloat(propName, 0);
	}

	#region Saving

	[Serializable]
	public class MultiDimensionCubeSave : SerializableSaveObject<MultiDimensionCube> {
		int state;
		float timeSinceStateChange;

		public MultiDimensionCubeSave(MultiDimensionCube multiDimensionCube) : base(multiDimensionCube) {
			this.state = (int)multiDimensionCube.state;
			this.timeSinceStateChange = multiDimensionCube.timeSinceStateChange;
		}

		public override void LoadSave(MultiDimensionCube multiDimensionCube) {
			multiDimensionCube._state = (State)this.state;
			multiDimensionCube.timeSinceStateChange = this.timeSinceStateChange;
		}
	}
	#endregion
}
