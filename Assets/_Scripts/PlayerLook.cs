using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour {
    Transform playerTransform;
    Transform cameraTransform;
    public float rotationY = 0F;

    // Use this for initialization
    void Start () {
        playerTransform = gameObject.transform;
        cameraTransform = playerTransform.GetChild(0);

        Cursor.lockState = CursorLockMode.Locked;
	}
	
	// Update is called once per frame
	void Update () {
        LookHorizontal(Input.GetAxis("Mouse X"));
        rotationY += Input.GetAxis("Mouse Y");
        rotationY = Mathf.Clamp(rotationY, -75f, 75f);
        LookVertical(rotationY);
	}

    private void LookVertical(float rotation) {
        cameraTransform.localEulerAngles = new Vector3(-rotation, cameraTransform.localEulerAngles.y, cameraTransform.localEulerAngles.z);
    }

    private void LookHorizontal(float rotation) {
        playerTransform.Rotate(new Vector3(0, rotation, 0));
    }
}
