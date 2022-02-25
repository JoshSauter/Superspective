using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveFasterZone : MonoBehaviour {
    private PlayerMovement playerMovement;

    public float maxMultiplier = 1.3f;
    public float multiplierLerpSpeed = 0.001f;
    public float multiplierDeLerpSpeed = 0.1f;

    private bool playerIsInZone = false;
    private bool resetPlayerIsInZone = false;
    
    // Start is called before the first frame update
    void Start() {
        playerMovement = PlayerMovement.instance;
    }

    private void OnTriggerStay(Collider other) {
        playerIsInZone = true;
        resetPlayerIsInZone = false;
    }

    private void FixedUpdate() {
        if (resetPlayerIsInZone) {
            playerIsInZone = false;
        }
        else {
            resetPlayerIsInZone = true;
        }

        float target = playerIsInZone ? maxMultiplier : 1f;
        float lerpSpeed = playerIsInZone ? multiplierLerpSpeed : multiplierDeLerpSpeed;
        playerMovement.movespeedMultiplier = Mathf.Lerp(playerMovement.movespeedMultiplier, target, lerpSpeed);
    }
}
