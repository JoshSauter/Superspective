using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour {
    public Collider enter;
    public Collider exit;

	// Use this for initialization
	void Awake () {
        enter = transform.Find("Enter").GetComponent<Collider>();
        exit = transform.Find("Exit").GetComponent<Collider>();
	}
}
