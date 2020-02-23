using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EpitaphUtils;

public class Interact : Singleton<Interact> {
	public bool DEBUG = false;
	DebugLogger debug;
	public Image reticle;
	public Image reticleOutside;

	Color reticleUnselectColor;
	Color reticleSelectColor = new Color(0.15f,1,0.15f,0.9f);
	Color reticleOutsideUnselectColor;
	Color reticleOutsideSelectColor = new Color(0.1f, 0.75f, 0.075f, 0.75f);
	public float interactionDistance = 5f;
	Camera cam;

	InteractableObject objectHovered;
	int layerMask;

	// Use this for initialization
	void Start () {
		debug = new DebugLogger(this, () => DEBUG);
		if (reticle == null) {
			Debug.LogError("Reticle not set in Interact script, disabling script");
			enabled = false;
			return;
		}

		cam = EpitaphScreen.instance.playerCamera;
		reticleUnselectColor = reticle.color;
		reticleOutsideUnselectColor = reticleOutside.color;

		layerMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Invisible") | 1 << LayerMask.NameToLayer("Ignore Raycast") | 1 << LayerMask.NameToLayer("CollideWithPlayerOnly"));
	}
	
	// Update is called once per frame
	void Update () {
		InteractableObject newObjectHovered = FindInteractableObjectHovered();
		// If we lose focus from previous object selected, send an event to that object
		if (newObjectHovered != null && newObjectHovered != objectHovered) {
			newObjectHovered.OnMouseHover?.Invoke();
		}
		if (objectHovered != null && newObjectHovered != objectHovered) {
			objectHovered.OnMouseHoverExit?.Invoke();
		}

		// Update which object is now selected
		objectHovered = newObjectHovered;

		if (objectHovered != null) {
			reticle.color = reticleSelectColor;
			reticleOutside.color = reticleOutsideSelectColor;
			// If the left mouse button is being held down, interact with the object selected
			if (Input.GetMouseButton(0)) {
				// If left mouse button was clicked this frame, call OnLeftMouseButtonDown
				if (Input.GetMouseButtonDown(0)) {
					objectHovered.OnLeftMouseButtonDown?.Invoke();
				}
				objectHovered.OnLeftMouseButton?.Invoke();
			}
			// If we released the left mouse button this frame, call OnLeftMouseButtonUp
			else if (Input.GetMouseButtonUp(0)) {
				objectHovered.OnLeftMouseButtonUp?.Invoke();
			}
		}
		else {
			reticle.color = reticleUnselectColor;
			reticleOutside.color = reticleOutsideUnselectColor;
		}

	}

	public RaycastHit GetRaycastHit() {
		Vector2 reticlePos = Reticle.instance.thisTransformPos;
		Vector2 screenPos = Vector2.Scale(reticlePos, new Vector2(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight));

		Ray ray = cam.ScreenPointToRay(screenPos);
		RaycastHit hitObject;
		Physics.Raycast(ray.origin, ray.direction, out hitObject, interactionDistance, layerMask, QueryTriggerInteraction.Collide);
		return hitObject;
	}

	InteractableObject FindInteractableObjectHovered() {
		RaycastHit hitObject = GetRaycastHit();
		if (hitObject.collider != null) {
			debug.Log("Hovering over " + hitObject.collider.gameObject.name);
			return hitObject.collider.GetComponent<InteractableObject>();
		}
		else return null;
	}
}
