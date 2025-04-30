using System;
using PortalMechanics;
using UnityEngine;

/// <summary>
/// Enum to represent the different cameras in the game. Single place of maintenance for getting camera positions.
/// </summary>
public enum Cam {
    Player,
    Portal
}

public static class CamExt {
    public static Vector3 CamPos(this Cam cam) {
        switch (cam) {
            case Cam.Player:
                return Player.instance.AdjustedCamPos;
            case Cam.Portal:
                return VirtualPortalCamera.instance.transform.position;
            default:
                throw new ArgumentOutOfRangeException(nameof(cam), cam, null);
        }
    }
    
    public static Vector3 CamDirection(this Cam cam) {
        switch (cam) {
            case Cam.Player:
                return Player.instance.PlayerCam.transform.forward;
            case Cam.Portal:
                return VirtualPortalCamera.instance.transform.forward;
            default:
                throw new ArgumentOutOfRangeException(nameof(cam), cam, null);
        }
    }
}
