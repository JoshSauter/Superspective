using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : SaveableObject<CameraZoom, CameraZoom.CameraZoomSave> {
    public float defaultFOV = 90f;
    public const float zoomFOV = 30f;
    public float currentFOV => mainCamera.fieldOfView;
    public bool zoomed = false;

    const float zoomLerpSpeed = 4f;
    PlayerLook playerLook;
    Camera mainCamera;
    public List<Camera> otherCameras = new List<Camera>();
    PlayerButtonInput input;

    protected override void Awake() {
	    base.Awake();
		mainCamera = GetComponent<Camera>();
		defaultFOV = mainCamera.fieldOfView;
	}

    protected override void Start() {
	    base.Start();
        playerLook = PlayerLook.instance;
        input = PlayerButtonInput.instance;

        StartCoroutine(Initialize());
    }

    IEnumerator Initialize() {
	    yield return new WaitForSeconds(1f);
	    otherCameras.Add(VirtualPortalCamera.instance.portalCamera);
    }

    void Update() {
        zoomed = input.Action2Held && playerLook.state == PlayerLook.State.ViewUnlocked;

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, zoomed ? zoomFOV : defaultFOV, Time.deltaTime * zoomLerpSpeed * (zoomed ? 1f : 2f));
        foreach (var cam in otherCameras) {
            cam.fieldOfView = mainCamera.fieldOfView;
		}
	}

	#region Saving
	// There's only one player so we don't need a UniqueId here
	public override string ID => "CameraZoom";

	[Serializable]
	public class CameraZoomSave : SerializableSaveObject<CameraZoom> {
		float defaultFOV;
		bool zoomed;
		float currentFOV;

		public CameraZoomSave(CameraZoom zoom) : base(zoom) {
			this.defaultFOV = zoom.defaultFOV;
			this.zoomed = zoom.zoomed;
			this.currentFOV = zoom.currentFOV;
		}

		public override void LoadSave(CameraZoom zoom) {
			zoom.defaultFOV = this.defaultFOV;
			zoom.zoomed = this.zoomed;
			zoom.mainCamera.fieldOfView = this.currentFOV;
		}
	}
	#endregion
}
