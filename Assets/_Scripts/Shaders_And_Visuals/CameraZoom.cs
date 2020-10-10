using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour, SaveableObject {
    public float defaultFOV = 90f;
    public const float zoomFOV = 30f;
    public float currentFOV => mainCamera.fieldOfView;
    public bool zoomed = false;

    private const float zoomLerpSpeed = 4f;
    PlayerLook playerLook;
    Camera mainCamera;
    public List<Camera> otherCameras = new List<Camera>();
    PlayerButtonInput input;

	void Awake() {
		mainCamera = GetComponent<Camera>();
		defaultFOV = mainCamera.fieldOfView;
	}

    IEnumerator Start() {
        playerLook = PlayerLook.instance;
        input = PlayerButtonInput.instance;

        yield return new WaitForSeconds(1f);
        otherCameras.Add(VirtualPortalCamera.instance.portalCamera);
    }

	private void Update() {
        zoomed = input.Action2Held && playerLook.state == PlayerLook.State.ViewUnlocked;

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, zoomed ? zoomFOV : defaultFOV, Time.deltaTime * zoomLerpSpeed);
        foreach (var cam in otherCameras) {
            cam.fieldOfView = mainCamera.fieldOfView;
		}
	}

	#region Saving
	// There's only one player so we don't need a UniqueId here
	public string ID => "CameraZoom";

	[Serializable]
	class CameraZoomSave {
		float defaultFOV;
		bool zoomed;
		float currentFOV;

		public CameraZoomSave(CameraZoom zoom) {
			this.defaultFOV = zoom.defaultFOV;
			this.zoomed = zoom.zoomed;
			this.currentFOV = zoom.currentFOV;
		}

		public void LoadSave(CameraZoom zoom) {
			zoom.defaultFOV = this.defaultFOV;
			zoom.zoomed = this.zoomed;
			zoom.mainCamera.fieldOfView = this.currentFOV;
		}
	}

	public object GetSaveObject() {
		return new CameraZoomSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		CameraZoomSave save = savedObject as CameraZoomSave;

		save.LoadSave(this);
	}
	#endregion
}
