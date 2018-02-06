using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour {
	public bool useLocalCoordinates = true;

	public float rotationsPerSecondX;
	public float rotationsPerSecondY;
	public float rotationsPerSecondZ;

	// Update is called once per frame
	void Update () {
		float rotX = Time.deltaTime * rotationsPerSecondX * 360;
		float rotY = Time.deltaTime * rotationsPerSecondY * 360;
		float rotZ = Time.deltaTime * rotationsPerSecondZ * 360;

		transform.Rotate(rotX, rotY, rotZ, useLocalCoordinates ? Space.Self : Space.World);
	}
}
