using System.Collections;
using System.Collections.Generic;
using LevelSpecific.WhiteRoom.CathedralTutorial;
using SuperspectiveUtils;
using UnityEngine;

public class RotateAroundPillar : MonoBehaviour {
    public FloorManager.Floor floor;
    public Transform portalCube;
    private Camera playerCam;

    public float followLerpSpeed = 10f;
    // effectivePlayerCamPos will never go within this distance of the pivot point (stops very fast rotations from happening)
    private float minDistance = 12;

    private Vector3 effectivePlayerCamPos;
    
    // Start is called before the first frame update
    void Start() {
        playerCam = SuperspectiveScreen.instance.playerCamera;
    }

    // Update is called once per frame
    void Update() {
        if (FloorManager.instance.floor == floor) {
            effectivePlayerCamPos = Vector3.Lerp(effectivePlayerCamPos, playerCam.transform.position, followLerpSpeed * Time.deltaTime);
        }
        else {
            effectivePlayerCamPos = Vector3.Lerp(effectivePlayerCamPos, portalCube.position, followLerpSpeed * Time.deltaTime);
        }
        UpdateWallRotation(effectivePlayerCamPos);
    }

    Angle AngleOfCamera(Vector3 camPos) {
        
        Vector3 pillarToPoint = camPos - transform.position;
        if (pillarToPoint.magnitude < minDistance) {
            pillarToPoint = pillarToPoint.normalized * minDistance;
        }
        PolarCoordinate polar = PolarCoordinate.CartesianToPolar(pillarToPoint);
        return Angle.D180 - polar.angle;
    }

    void UpdateWallRotation(Vector3 camPos) {
        transform.localEulerAngles = new Vector3(0, AngleOfCamera(camPos).degrees, 0);
    }
}
