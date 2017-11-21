using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Staircase : MonoBehaviour {
    Vector3 startRot;
    Vector3 endRot = new Vector3(0, 0, -90);

    Collider collider;
    float playerStartPos;
    float playerEndPos;

	// Use this for initialization
	void Start () {
        startRot = transform.parent.rotation.eulerAngles;
        collider = GetComponent<Collider>();
	}
	
	// Update is called once per frame
	void Update () {
	}

    private void OnTriggerStay(Collider other) {
        if (other.tag == "Player") {
            playerStartPos = collider.bounds.min.x;
            playerEndPos = collider.bounds.max.x;

            float t = Mathf.InverseLerp(playerStartPos, playerEndPos, other.transform.position.x);
            transform.parent.rotation = Quaternion.Euler(Vector3.Lerp(startRot, endRot, t));
        }
    }
}
