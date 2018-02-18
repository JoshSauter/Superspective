using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour {
	public Transform[] otherObjectsToTeleport;

    public Collider enter;
    public Collider exit;

	// Use this for initialization
	void Awake () {
		if (otherObjectsToTeleport == null) otherObjectsToTeleport = new Transform[0];

		if (enter == null) {
			enter = transform.Find("Enter").GetComponent<Collider>();
		}
		if (exit == null) {
			exit = transform.Find("Exit").GetComponent<Collider>();
		}
	}
}
