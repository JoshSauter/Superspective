using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillarFork : MonoBehaviour {
    DimensionPillar pillar;
    
    void Start() {
        pillar = GetComponent<DimensionPillar>();
    }

    void Update() {
        Vector3 diff = Player.instance.transform.position - transform.position;
        if (Mathf.Abs(diff.x) < 0.4f || diff.z < 0) return;
        
        bool playerToTheLeft = diff.x < 0;
        if (playerToTheLeft && pillar.curDimension == 1) {
            pillar.ShiftDimensionDown();
        }
        else if (!playerToTheLeft && pillar.curDimension == 0) {
            pillar.ShiftDimensionUp();
        }
    }
}
