using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour {
    Transform playerTransform;
    Transform cameraTransform;
	[Range(0.01f,1)]
	public float generalSensitivity = 0.5f;
	[Range(0.01f,1)]
	public float sensitivityX = 0.5f;
	[Range(0.01f,1)]
	public float sensitivityY = 0.5f;
    public float rotationY = 0F;

    // Use this for initialization
    void Start () {
        playerTransform = gameObject.transform;
        cameraTransform = playerTransform.GetChild(0);

        Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
	
	// Update is called once per frame
	void Update () {
		LookHorizontal(Input.GetAxis("Mouse X") * 4 * generalSensitivity * sensitivityX);
		rotationY += Input.GetAxis("Mouse Y") * 4 * generalSensitivity * sensitivityY;
        rotationY = Mathf.Clamp(rotationY, -85f, 85f);
        LookVertical(rotationY);
	}

    private void LookVertical(float rotation) {
        cameraTransform.localEulerAngles = new Vector3(-rotation, cameraTransform.localEulerAngles.y, cameraTransform.localEulerAngles.z);
    }

    private void LookHorizontal(float rotation) {
        playerTransform.Rotate(new Vector3(0, rotation, 0));
    }
}
