using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Interact : MonoBehaviour {
	public Image reticle;
	public Image reticleOutside;

	Color reticleUnselectColor;
	Color reticleSelectColor = new Color(0.15f,1,0.15f,0.9f);
	Color reticleOutsideUnselectColor;
	Color reticleOutsideSelectColor = new Color(0.1f, 0.75f, 0.075f, 0.75f);
	public float interactionDistance = 5f;
	Transform cam;

	InteractableObject objectSelected;

	// Use this for initialization
	void Start () {
		if (reticle == null) {
			Debug.LogError("Reticle not set in Interact script, disabling script");
			this.enabled = false;
			return;
		}

		cam = EpitaphScreen.instance.playerCamera.transform;
		reticleUnselectColor = reticle.color;
		reticleOutsideUnselectColor = reticleOutside.color;
		if (cam.GetComponent<Camera>() == null) {
			Debug.LogError("\"Camera\" object does not have an actual camera component attached, make sure this is the object you want.");
		}
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
				objectSelected.OnLeftMouseButton();
				// If left mouse button was clicked this frame, call OnLeftMouseButtonDown
				if (Input.GetMouseButtonDown(0)) {
					objectSelected.OnLeftMouseButtonDown();
				}
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

	InteractableObject FindInteractableObjectSelected() {
		RaycastHit hitObject;
		Physics.Raycast(cam.position, cam.transform.forward, out hitObject, interactionDistance);
		if (hitObject.collider != null) {
			return hitObject.collider.GetComponent<InteractableObject>();
		}
		else return null;
	}
}
