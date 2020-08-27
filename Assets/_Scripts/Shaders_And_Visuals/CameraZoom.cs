using PortalMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour {
    public float defaultFOV = 90f;
    public float zoomFOV = 30f;
    public float currentFOV => mainCamera.fieldOfView;
    public bool zoomed = false;

    private float zoomLerpSpeed = 4f;
    PlayerLook playerLook;
    Camera mainCamera;
    public List<Camera> otherCameras = new List<Camera>();
    PlayerButtonInput input;

    IEnumerator Start() {
        mainCamera = GetComponent<Camera>();
        defaultFOV = mainCamera.fieldOfView;
        playerLook = PlayerLook.instance;
        input = PlayerButtonInput.instance;

        yield return new WaitForSeconds(1f);
        otherCameras.Add(VirtualPortalCamera.instance.portalCamera);
    }

	private void Update() {
        zoomed = input.Action2Held && playerLook.viewLockedObject == null;

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, zoomed ? zoomFOV : defaultFOV, Time.deltaTime * zoomLerpSpeed);
        foreach (var cam in otherCameras) {
            cam.fieldOfView = mainCamera.fieldOfView;
		}
	}
}
