using System;
using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;

public class CubeKiller : MonoBehaviour {
    private void OnTriggerStay(Collider other) {
        if (other.TryGetComponent(out PickupObject cube)) {
            cube.Dematerialize();
        }
    }
}
