using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class MultiDimensionCube : MonoBehaviour {
	public enum State {
		Materialized,
		Materializing
	}
	private State _state;
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
	// TODO: Continue implementing to get rid of Coroutine
	float timeSinceStateChange = 0f;
	InteractableObject interactableObject;
	public PickupObject pickupCube;
	public PillarDimensionObject2 corporealCubeDimensionObj, invertedCubeDimensionObj;
	public EpitaphRenderer cubeFrameRenderer, corporealGlassRenderer, invertedCubeRenderer, raymarchRenderer;

	Transform[] cubeTransforms;

	public BoxCollider thisCollider;

	BoxCollider kinematicCollider;
	Rigidbody kinematicRigidbody;

	BoxCollider detectWhenPlayerIsNearCollider;

	const string defaultLayerName = "Default";
	const string propName = "_DissolveValue";
	float outlineFadeInTime = .5f;
	float invertColorsDelay = 0.4f;
	float invertColorsFadeOutTime = .5f;

	public void OnLeftMouseButtonDown() {
		if (!pickupCube.isHeld && corporealCubeDimensionObj.visibilityState != VisibilityState.visible) {
			Materialize();
		}
	}

	void SetupKinematicCollider() {
		GameObject kinematicGameObject = new GameObject("KinematicCollider");
		kinematicGameObject.transform.SetParent(transform);
		kinematicGameObject.transform.localScale = Vector3.one;
		kinematicGameObject.layer = LayerMask.NameToLayer("CollideWithPlayerOnly");

		kinematicRigidbody = kinematicGameObject.AddComponent<Rigidbody>();
		kinematicRigidbody.isKinematic = true;

		kinematicCollider = kinematicGameObject.AddComponent<BoxCollider>();
		kinematicCollider.size = thisCollider.size * 1.01f;
		kinematicCollider.enabled = !thisCollider.enabled;
	}

	void SetupDetectPlayerIsNearCollider() {
		GameObject triggerGameObject = new GameObject("DetectPlayerIsNearCollider");
		triggerGameObject.transform.SetParent(transform);
		triggerGameObject.transform.localScale = Vector3.one;
		triggerGameObject.transform.localPosition = Vector3.zero;
		triggerGameObject.transform.localRotation = new Quaternion();
		triggerGameObject.layer = LayerMask.NameToLayer("CollideWithPlayerOnly");

		detectWhenPlayerIsNearCollider = triggerGameObject.AddComponent<BoxCollider>();
		detectWhenPlayerIsNearCollider.size = thisCollider.size * 1.02f;
		detectWhenPlayerIsNearCollider.isTrigger = true;

		triggerGameObject.AddComponent<FreezeRigidbodyWhenPlayerIsNear>().pickupCubeDimensionShift = this;
	}

	void Awake() {
		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
			interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
		}
	}

	void Start() {
		cubeTransforms = corporealCubeDimensionObj.transform.GetComponentsInChildrenRecursively<Transform>();
		thisCollider = GetComponent<BoxCollider>();

		SetupKinematicCollider();
		SetupDetectPlayerIsNearCollider();

		corporealCubeDimensionObj.OnStateChange += HandleStateChange;
	}

	void FixedUpdate() {
		kinematicCollider.enabled = thisCollider.enabled;
		kinematicCollider.transform.position = transform.position;
		kinematicCollider.transform.rotation = transform.rotation;
		kinematicCollider.size = thisCollider.size * 1.01f;
	}

    void Update() {
        if (Input.GetKeyDown("o")) {
			Materialize();
		}

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
					state = State.Materialized;
				}
				break;
			default:
				break;
		}
	}

	void HandleStateChange(VisibilityState newState) {
		if (newState == VisibilityState.visible) {
			ResetMaterialsThisDimension();
		}
		else if (newState == VisibilityState.invisible) {
			ResetMaterialsOtherDimensions();
		}
	}

	void Materialize() {
		corporealCubeDimensionObj.baseDimension = DimensionPillar.activePillar.curDimension;
		invertedCubeDimensionObj.baseDimension = DimensionPillar.activePillar.curDimension;
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
}
