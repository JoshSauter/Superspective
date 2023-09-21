using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using PowerTrailMechanics;
using UnityEngine;

// Deprecated, functionality moved into PowerTrails directly
public class PowerTrailDaisyChain : MonoBehaviour {
    public MultiMode mode = MultiMode.Single;
    [HideIf("IsMulti")]
    public PowerTrail source;
    [ShowIf("IsMulti")]
    public PowerTrail[] sources;
    
    bool IsMulti() {
        return mode != MultiMode.Single;
    }
    
    void Start() {
        PowerTrail thisPowerTrail = GetComponent<PowerTrail>();
        thisPowerTrail.skipStartupShutdownSounds = true;

        switch (mode) {
            case MultiMode.Single:
                source.OnPowerFinish += () => thisPowerTrail.powerIsOn = PowerIsOn();
                // Not technically correct if you want it to act as one continuous PowerTrail, but good enough for now
                source.OnDepowerBegin += () => thisPowerTrail.powerIsOn = PowerIsOn();
                break;
            case MultiMode.Any:
            case MultiMode.All:
                foreach (var pt in sources) {
                    pt.OnPowerFinish += () => thisPowerTrail.powerIsOn = PowerIsOn();
                    pt.OnDepowerFinish += () => thisPowerTrail.powerIsOn = PowerIsOn();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    bool PowerIsOn() {
        switch (mode) {
            case MultiMode.Single:
                return source.powerIsOn;
            case MultiMode.Any:
                return sources.ToList().Exists(s => s.powerIsOn);
            case MultiMode.All:
                return sources.ToList().TrueForAll(s => s.powerIsOn);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
