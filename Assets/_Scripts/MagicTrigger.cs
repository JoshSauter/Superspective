using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicTrigger : MonoBehaviour {
    static bool DEBUG = false;
    public bool destroyOnTrigger = false;

    public Vector3 playerFaceDirection;
    public float playerFaceAmount;

    public delegate void MagicAction(Collider other);
    public event MagicAction OnMagicTrigger;

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.tag == "Player") {
            if (DEBUG) {
                print("Amount facing: " + Vector3.Dot(other.transform.forward, playerFaceDirection) + "\nThreshold: " + playerFaceAmount +
                "\nPass?: " + (Vector3.Dot(other.transform.forward, playerFaceDirection) > playerFaceAmount));
            }
            if (Vector3.Dot(other.transform.forward, playerFaceDirection) > playerFaceAmount && OnMagicTrigger != null) {
                OnMagicTrigger(other);
                if (destroyOnTrigger) {
                    Destroy(gameObject);
                }
            }
        }
    }
}
