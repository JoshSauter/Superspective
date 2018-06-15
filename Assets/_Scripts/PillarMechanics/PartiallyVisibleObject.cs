using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VisibilityState {
	invisible,
	partiallyVisible,
	visible
};

public class PartiallyVisibleObject : MonoBehaviour {
	public Color materialColor = Color.black;
	private VisibilityState _visibilityState;
	public VisibilityState visibilityState {
		get {
			return _visibilityState;
		}
	}

	public VisibilityState startingVisibilityState;
	VisibilityState oppositeStartingVisibilityState;
	bool negativeRenderer;
	int initialLayer;
	Material visibleMaterial;
	Material initialMaterial;
	Renderer renderer;

	void Awake() {
		initialLayer = gameObject.layer;
		renderer = GetComponent<Renderer>();
		visibleMaterial = Resources.Load<Material>("Materials/Unlit/Unlit");
		initialMaterial = renderer.material;
		negativeRenderer = initialMaterial.name.Contains("Neg");
		oppositeStartingVisibilityState = startingVisibilityState == VisibilityState.visible ? VisibilityState.invisible : VisibilityState.visible;
	}

	private void Start() {
		SetVisibilityState(startingVisibilityState);
	}

	/// <summary>
	/// Handles the VisibilityState switching logic when a sweeping collider hits this object
	/// </summary>
	/// <param name="direction">Direction the sweeping collider is moving</param>
	/// <returns>true if VisibilityState changes, false otherwise</returns>
	public bool HitBySweepingCollider(MovementDirection direction) {
		switch (direction) {
			case MovementDirection.clockwise:
				if (visibilityState == startingVisibilityState) {
					print("Enter Clockwise, setting visibility state to PartiallyVisible from " + startingVisibilityState);
					SetVisibilityState(VisibilityState.partiallyVisible);
					return true;
				}
				break;
			case MovementDirection.counterclockwise:
				if (visibilityState == oppositeStartingVisibilityState) {
					print("Enter Counterclockwise, setting visibility state to PartiallyVisible from " + oppositeStartingVisibilityState);
					SetVisibilityState(VisibilityState.partiallyVisible);
					return true;
				}
				break;
		}
		return false;
	}

	/// <summary>
	/// Handles the VisibilityState switching logic when a sweeping collider exits this object
	/// </summary>
	/// <param name="direction">Direction the sweeping collider is moving</param>
	/// <returns>true if VisibilityState changes, false otherwise</returns>
	public bool SweepingColliderExit(MovementDirection direction) {
		if (visibilityState == VisibilityState.partiallyVisible) {
			switch (direction) {
				case MovementDirection.clockwise:
					print("Exit Clockwise, setting visibility state to " + oppositeStartingVisibilityState + " from PartiallyVisible");
					SetVisibilityState(oppositeStartingVisibilityState);
					return true;
				case MovementDirection.counterclockwise:
					print("Exit Counterclockwise, setting visibility state to " + startingVisibilityState + " from PartiallyVisible");
					SetVisibilityState(startingVisibilityState);
					return true;
			}
		}
		return false;
	}

	public void ResetVisibilityState() {
		SetVisibilityState(startingVisibilityState);
	}

	public void SetVisibilityState(VisibilityState newState) {
		_visibilityState = newState;
		UpdateVisibilitySettings();
	}

	private void UpdateVisibilitySettings() {
		switch (visibilityState) {
			case VisibilityState.invisible:
				gameObject.layer = LayerMask.NameToLayer("Invisible");
				break;
			case VisibilityState.partiallyVisible:
				gameObject.layer = initialLayer;
				UpdateMaterial(initialMaterial);
				break;
			case VisibilityState.visible:
				gameObject.layer = initialLayer;
				UpdateMaterial(visibleMaterial);
				break;
		}
	}

	private void UpdateMaterial(Material newMaterial) {
		renderer.material = newMaterial;
		MaterialPropertyBlock pb = new MaterialPropertyBlock();
		renderer.GetPropertyBlock(pb);
		pb.SetColor("_Color", materialColor);
		renderer.SetPropertyBlock(pb);
	}
}
