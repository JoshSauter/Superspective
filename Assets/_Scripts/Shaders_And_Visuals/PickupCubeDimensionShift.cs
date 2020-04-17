using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class PickupCubeDimensionShift : MonoBehaviour {
	InteractableObject interactableObject;
	public PickupObject pickupCube;
	public PillarDimensionObject pickupCubeDimensionObject, invertColorsDimensionObject;
	public EpitaphRenderer cubeOutlineRenderer, invertColorsRenderer, raymarchRenderer;

	UnityEngine.Transform[] cubeTransforms;

	public BoxCollider thisCollider;

	BoxCollider kinematicCollider;
	Rigidbody kinematicRigidbody;

	BoxCollider detectWhenPlayerIsNearCollider;

	const string defaultLayerName = "Default";
	const string propName = "_DissolveValue";
	float outlineFadeInTime = 5f;
	float invertColorsDelay = 0.4f;
	float invertColorsFadeOutTime = 5f;

	bool inOtherDimension;

	public void OnLeftMouseButtonDown() {
		Materialize();
		pickupCube.Pickup();
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
		kinematicCollider.enabled = thisCollider.enabled;
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
		cubeTransforms = pickupCubeDimensionObject.transform.GetComponentsInChildrenRecursively<UnityEngine.Transform>();
		thisCollider = GetComponent<BoxCollider>();

		ResetMaterialsOtherDimensions();
		SetupKinematicCollider();
		SetupDetectPlayerIsNearCollider();

		pickupCubeDimensionObject.OnStateChange += HandleStateChange;
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
		pickupCubeDimensionObject.SetBaseDimension(DimensionPillar.activePillar.curDimension);
		invertColorsDimensionObject.SetBaseDimension(DimensionPillar.activePillar.curDimension);
		ResetMaterialsOtherDimensions();
		StopAllCoroutines();
		StartCoroutine(MaterializeCoroutine());
	}

	void ResetMaterialsThisDimension() {
		thisCollider.enabled = false;
		if (pickupCube.thisRigidbody != null) {
			pickupCube.thisRigidbody.isKinematic = false;
		}

		// Always keep these materials visible
		foreach (var t in cubeTransforms) {
			t.gameObject.layer = LayerMask.NameToLayer(defaultLayerName);
		}

		cubeOutlineRenderer.SetFloat(propName, 0);
		invertColorsRenderer.SetFloat(propName, 1);
		raymarchRenderer.SetFloat(propName, 1);
	}

	void ResetMaterialsOtherDimensions() {
		thisCollider.enabled = true;

		// Always keep these materials visible
		foreach (var t in cubeTransforms) {
			t.gameObject.layer = LayerMask.NameToLayer(defaultLayerName);
		}

		cubeOutlineRenderer.SetFloat(propName, 1);
		invertColorsRenderer.SetFloat(propName, 0);
		raymarchRenderer.SetFloat(propName, 0);
	}

	IEnumerator MaterializeCoroutine() {
		thisCollider.enabled = false;

		float maxTime = Mathf.Max(outlineFadeInTime, invertColorsDelay) + invertColorsFadeOutTime;
		float timeElapsed = 0;

		while (timeElapsed < maxTime) {
			timeElapsed += Time.deltaTime;

			if (timeElapsed < outlineFadeInTime) {
				float t1 = timeElapsed / outlineFadeInTime;
				cubeOutlineRenderer.SetFloat(propName, 1-t1);
			}
			else {
				cubeOutlineRenderer.SetFloat(propName, 0);
			}

			if (timeElapsed > invertColorsDelay) {
				float t2 = (timeElapsed - invertColorsDelay) / invertColorsFadeOutTime;
				invertColorsRenderer.SetFloat(propName, t2);
				raymarchRenderer.SetFloat(propName, t2);
			}

			yield return null;
		}

		ResetMaterialsThisDimension();

		yield return new WaitForSeconds(0.5f);
		//ResetMaterialsOtherDimensions();
	}
}
