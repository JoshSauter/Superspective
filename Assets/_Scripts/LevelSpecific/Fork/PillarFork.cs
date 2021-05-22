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
        float diff = Player.instance.transform.position.x - transform.position.x;
        bool playerToTheLeft = diff < 0;
        if (Mathf.Abs(diff) < 0.4f) return;
        if (playerToTheLeft && pillar.curDimension == 1) {
            pillar.ShiftDimensionDown();
        }
        else if (!playerToTheLeft && pillar.curDimension == 0) {
            pillar.ShiftDimensionUp();
        }
    }
}
