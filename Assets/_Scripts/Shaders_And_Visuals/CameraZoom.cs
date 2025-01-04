using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

public class CameraZoom : SuperspectiveObject<CameraZoom, CameraZoom.CameraZoomSave> {
    public float defaultFOV = 90f;
    public const float zoomFOV = 30f;
    public float currentFOV => mainCamera.fieldOfView;
    public bool zoomed = false;

    const float zoomLerpSpeed = 4f;
    PlayerLook playerLook;
    Camera mainCamera;
    public List<Camera> otherCameras = new List<Camera>();
    PlayerButtonInput input;

    private const float VIGNETTE_MULTIPLIER = 1.6f;
    private float defaultVignetteMagnitude;
    private VignetteAndChromaticAberration vignette;

    protected override void Awake() {
	    base.Awake();
		mainCamera = GetComponent<Camera>();
		defaultFOV = mainCamera.fieldOfView;
		vignette = mainCamera.GetComponent<VignetteAndChromaticAberration>();
		defaultVignetteMagnitude = vignette.intensity;
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
        zoomed = input.ZoomHeld && playerLook.state == PlayerLook.ViewLockState.ViewUnlocked;

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, zoomed ? zoomFOV : defaultFOV, Time.deltaTime * zoomLerpSpeed * (zoomed ? 1f : 2f));
        foreach (var cam in otherCameras) {
            cam.fieldOfView = mainCamera.fieldOfView;
		}

        vignette.intensity = Mathf.Lerp(vignette.intensity, zoomed ? defaultVignetteMagnitude * VIGNETTE_MULTIPLIER : defaultVignetteMagnitude, Time.deltaTime * zoomLerpSpeed * (zoomed ? 1f : 2f));
    }

#region Saving

	public override void LoadSave(CameraZoomSave save) {
		defaultFOV = save.defaultFOV;
		mainCamera.fieldOfView = currentFOV;
		zoomed = save.zoomed;
	}

	// There's only one player so we don't need a UniqueId here
	public override string ID => "CameraZoom";

	[Serializable]
	public class CameraZoomSave : SaveObject<CameraZoom> {
		public float defaultFOV;
		public float currentFOV;
		public bool zoomed;

		public CameraZoomSave(CameraZoom zoom) : base(zoom) {
			this.defaultFOV = zoom.defaultFOV;
			this.currentFOV = zoom.currentFOV;
			this.zoomed = zoom.zoomed;
		}
	}
#endregion
}
