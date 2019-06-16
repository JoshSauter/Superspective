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

	InteractableObject objectSelected;

	// Use this for initialization
	void Start () {
		debug = new DebugLogger(gameObject, DEBUG);
		if (reticle == null) {
			Debug.LogError("Reticle not set in Interact script, disabling script");
			enabled = false;
			return;
		}

		cam = EpitaphScreen.instance.playerCamera;
		reticleUnselectColor = reticle.color;
		reticleOutsideUnselectColor = reticleOutside.color;
	}
	
	// Update is called once per frame
	void Update () {
		InteractableObject newObjectSelected = FindInteractableObjectSelected();
		// If we lose focus from previous object selected, send an event to that object
		if (objectSelected != null && newObjectSelected != objectSelected) {
			objectSelected.OnLeftMouseButtonFocusLost();
		}

		// Update which object is now selected
		objectSelected = newObjectSelected;

		if (objectSelected != null) {
			reticle.color = reticleSelectColor;
			reticleOutside.color = reticleOutsideSelectColor;
			// If the left mouse button is being held down, interact with the object selected
			if (Input.GetMouseButton(0)) {
				// If left mouse button was clicked this frame, call OnLeftMouseButtonDown
				if (Input.GetMouseButtonDown(0)) {
					objectSelected.OnLeftMouseButtonDown();
				}
				objectSelected.OnLeftMouseButton();
			}
			// If we released the left mouse button this frame, call OnLeftMouseButtonUp
			else if (Input.GetMouseButtonUp(0)) {
				objectSelected.OnLeftMouseButtonUp();
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
		Physics.Raycast(ray.origin, ray.direction, out hitObject, interactionDistance, ~(1 << LayerMask.NameToLayer("Player")), QueryTriggerInteraction.Collide);
		return hitObject;
	}

	InteractableObject FindInteractableObjectSelected() {
		RaycastHit hitObject = GetRaycastHit();
		if (hitObject.collider != null) {
			debug.Log("Hovering over " + hitObject.collider.gameObject.name);
			return hitObject.collider.GetComponent<InteractableObject>();
		}
		else return null;
	}
}
