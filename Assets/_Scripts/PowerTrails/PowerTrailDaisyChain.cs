using System.Collections;
using System.Collections.Generic;
using PowerTrailMechanics;
using UnityEngine;

public class PowerTrailDaisyChain : MonoBehaviour {
    public PowerTrail source;
    
    void Start() {
        PowerTrail thisPowerTrail = GetComponent<PowerTrail>();
        thisPowerTrail.skipStartupShutdownSounds = true;
        source.OnPowerFinish += () => thisPowerTrail.powerIsOn = true;
        // Not technically correct if you want it to act as one continuous PowerTrail, but good enough for now
        source.OnDepowerBegin += () => thisPowerTrail.powerIsOn = false;
    }
}
