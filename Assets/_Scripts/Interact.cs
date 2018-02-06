using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Interact : MonoBehaviour {
	public Image reticle;

	Color reticleUnselectColor;
	Color reticleSelectColor = new Color(0.15f,1,0.15f,0.9f);
	public float interactionDistance = 5f;
	Transform cam;

	// Use this for initialization
	void Start () {
		if (reticle == null) {
			Debug.LogError("Reticle not set in Interact script, disabling script");
			this.enabled = false;
			return;
		}

		cam = transform.GetChild(0);
		reticleUnselectColor = reticle.color;
		if (cam.GetComponent<Camera>() == null) {
			Debug.LogError("\"Camera\" object does not have an actual camera component attached, make sure this is the object you want.");
		}
	}
	
	// Update is called once per frame
	void Update () {
		InteractableObject obj = FindInteractableObjectSelected();
		if (obj != null) {
			reticle.color = reticleSelectColor;
			// If the left mouse button is being held down, interact with the object selected
			if (Input.GetMouseButton(0)) {
				obj.OnLeftMouseButton();
				// If left mouse button was clicked this frame, call OnLeftMouseButtonDown
				if (Input.GetMouseButtonDown(0)) {
					obj.OnLeftMouseButtonDown();
				}
			}
			// If we released the left mouse button this frame, call OnLeftMouseButtonUp
			else if (Input.GetMouseButtonUp(0)) {
				obj.OnLeftMouseButtonUp();
			}
		}
		else {
			reticle.color = reticleUnselectColor;
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
