using System;
using System.Collections.Generic;
using SuperspectiveUtils;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(BetterTrigger))]
public class StaircaseRotate : MonoBehaviour, BetterTriggers {
    public static bool playerIsInAnyStaircaseRotateZone = false;
    public bool DEBUG;

    [ShowIf("DEBUG")]
    [ReadOnly]
    [SerializeField]
    bool treatedAsADownStairForPlayer;

    [ShowIf("DEBUG")]
    [ReadOnly]
    public float t;

    [ShowIf("DEBUG")]
    [ReadOnly]
    public Vector3 startPosition;
    public Vector3 effectiveStartPosition => startPosition + (endPosition - startPosition).normalized * startEndGap * Player.instance.Scale;

    [ShowIf("DEBUG")]
    [ReadOnly]
    public Vector3 endPosition;
    public Vector3 effectiveEndPosition => endPosition - (endPosition - startPosition).normalized * startEndGap * Player.instance.Scale;

    public Vector3 startGravityDirection = Vector3.zero;
    public Vector3 endGravityDirection = Vector3.zero;

    // Solves a problem where the t value never hit 0 or 1 (because bottom of player was marginally too high to be exactly floor level)
    readonly float startEndGap = 0.25f;
    readonly float gravAmplificationMagnitude = 8f;
    readonly float maxDistanceForGravAmplification = 8f;

    // Gravity amplification is use to stop players from "flying" over down-stairs
    readonly float minDistanceForGravAmplification = 4f;

    PlayerMovement playerMovement;

    [ShowNativeProperty]
    public Vector3 currentGravity => Physics.gravity;

    private float baseGravMagnitude;

    public Vector3 pivotPoint => transform.parent.position;
    Vector3 pivot => Vector3.Cross(startGravityDirection, endGravityDirection);

    private readonly Dictionary<GravityObject, bool> objectsInStaircase = new Dictionary<GravityObject, bool>();

    void Start() {
        playerMovement = PlayerMovement.instance;
        baseGravMagnitude = Physics.gravity.magnitude;

        if (startGravityDirection == Vector3.zero) startGravityDirection = -transform.parent.up;
        if (endGravityDirection == Vector3.zero) endGravityDirection = transform.parent.forward;

        Bounds initialBounds = transform.parent.GetComponent<Renderer>().bounds;

        startPosition = pivotPoint -
                        endGravityDirection * Mathf.Abs(Vector3.Dot(initialBounds.size, endGravityDirection));
        endPosition = pivotPoint -
                      startGravityDirection * Mathf.Abs(Vector3.Dot(initialBounds.size, startGravityDirection));

        gameObject.layer = SuperspectivePhysics.TriggerZoneLayer;
    }

    public void OnBetterTriggerEnter(Collider other) {
        if (other.TaggedAsPlayer()) {
            Player.instance.movement.pauseSnapToGround = true;
            baseGravMagnitude = currentGravity.magnitude;
            float testLerpValue = GetLerpPositionOfPoint(playerMovement.BottomOfPlayer);
            treatedAsADownStairForPlayer = TreatedAsDownStair(testLerpValue, Physics.gravity.normalized);
        }
        else if (other.TryGetComponent(out GravityObject gravity)) {
            float testLerpValue = GetLerpPositionOfPoint(other.transform.position);
            bool treatedAsDownStair = TreatedAsDownStair(testLerpValue, gravity.GravityDirection);
            
            objectsInStaircase.Add(gravity, treatedAsDownStair);
        }
        
        bool TreatedAsDownStair(float testLerpValue, Vector3 gravityDirection) {
            Vector3 testGravityDirection1 =
                Vector3.Lerp(startGravityDirection, endGravityDirection, testLerpValue).normalized;
            Vector3 testGravityDirection2 =
                Vector3.Lerp(startGravityDirection, endGravityDirection, 1 - testLerpValue).normalized;

            return Vector3.Angle(testGravityDirection1, gravityDirection) >
                   Vector3.Angle(testGravityDirection2, gravityDirection);
        }
    }

    public void OnBetterTriggerExit(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerIsInAnyStaircaseRotateZone = false;
            float angleToStartDirection = Vector3.Angle(startGravityDirection, Physics.gravity.normalized);
            float angleToEndDirection = Vector3.Angle(endGravityDirection, Physics.gravity.normalized);

            Vector3 exitGravity = angleToStartDirection < angleToEndDirection ? startGravityDirection : endGravityDirection;
            Player.instance.movement.pauseSnapToGround = false;
            Physics.gravity = baseGravMagnitude * exitGravity;

            float angleBetween = Vector3.Angle(playerMovement.transform.up, -Physics.gravity.normalized);
            if (treatedAsADownStairForPlayer) angleBetween = -angleBetween;
            playerMovement.transform.rotation =
                Quaternion.FromToRotation(playerMovement.transform.up, -Physics.gravity.normalized) *
                playerMovement.transform.rotation;

            PlayerLook playerLook = PlayerLook.instance;
            playerLook.RotationY -= angleBetween * Vector3.Dot(
                playerMovement.transform.forward,
                playerMovement.ProjectedHorizontalVelocity().normalized
            );
            playerLook.RotationY = Mathf.Clamp(playerLook.RotationY, -playerLook.yClamp, playerLook.yClamp);
        }
        else if (other.TryGetComponent(out GravityObject gravityObj)) {
            float angleToStartDirection = Vector3.Angle(startGravityDirection, gravityObj.GravityDirection.normalized);
            float angleToEndDirection = Vector3.Angle(endGravityDirection, gravityObj.GravityDirection.normalized);

            Vector3 exitGravity = angleToStartDirection < angleToEndDirection ? startGravityDirection : endGravityDirection;
            
            gravityObj.GravityDirection = exitGravity;
            if (objectsInStaircase.ContainsKey(gravityObj)) {
                objectsInStaircase.Remove(gravityObj);
            }
        }
    }

    public void OnBetterTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerIsInAnyStaircaseRotateZone = true;
            Player.instance.movement.pauseSnapToGround = true;
            t = GetLerpPositionOfPoint(playerMovement.BottomOfPlayer);
            if (treatedAsADownStairForPlayer) t = 1 - t;

            float gravAmplificationFactor = 1;
            if (treatedAsADownStairForPlayer) {
                Vector3 projectedPlayerPos = Vector3.ProjectOnPlane(playerMovement.BottomOfPlayer.GetClosestPointOnFiniteLine(
                    effectiveStartPosition,
                    effectiveEndPosition
                ), pivot);
                // TODO: Fix so that displacement perpendicular to the axis isn't counted
                Vector3 playerPositionOnPlane = Vector3.ProjectOnPlane(playerMovement.BottomOfPlayer, pivot);
                float distanceFromPlayerToStairs = Vector3.Distance(playerPositionOnPlane, projectedPlayerPos);
                gravAmplificationFactor = 1 + gravAmplificationMagnitude * Mathf.InverseLerp(
                    minDistanceForGravAmplification,
                    maxDistanceForGravAmplification,
                    distanceFromPlayerToStairs
                );
            }

            Physics.gravity = baseGravMagnitude * gravAmplificationFactor *
                              Vector3.Lerp(startGravityDirection, endGravityDirection, t).normalized;

            float angleBetween = Vector3.Angle(playerMovement.transform.up, -Physics.gravity.normalized);
            if (treatedAsADownStairForPlayer) angleBetween = -angleBetween;
            playerMovement.transform.rotation =
                Quaternion.FromToRotation(playerMovement.transform.up, -Physics.gravity.normalized) *
                playerMovement.transform.rotation;

            PlayerLook playerLook = PlayerLook.instance;
            playerLook.RotationY -= angleBetween * Vector3.Dot(
                playerMovement.transform.forward,
                playerMovement.ProjectedHorizontalVelocity().normalized
            );
            playerLook.RotationY = Mathf.Clamp(playerLook.RotationY, -playerLook.yClamp, playerLook.yClamp);
        }
        else if (other.gameObject.TryGetComponent(out GravityObject gravityObj)) {
            float objT = GetLerpPositionOfPoint(other.transform.position);
            if (objectsInStaircase.ContainsKey(gravityObj)) {
                if (objectsInStaircase[gravityObj]) objT = 1 - objT;
            }
            gravityObj.GravityDirection = Vector3.Lerp(startGravityDirection, endGravityDirection, objT).normalized;
        }
    }

    float GetLerpPositionOfPoint(Vector3 point) {
        Vector3 closestPointOnLine = point.GetClosestPointOnFiniteLine(effectiveStartPosition, effectiveEndPosition);
        float t = Utils.Vector3InverseLerp(effectiveStartPosition, effectiveEndPosition, closestPointOnLine);

        return t;
    }

    private void OnDrawGizmos() {
        float sphereSize = .25f * Player.instance.Scale;
        Color originalColor = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(effectiveStartPosition, sphereSize);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(effectiveEndPosition, sphereSize);
        Gizmos.color = (Color.black * 0.9f).WithAlpha(1f);
        Gizmos.DrawLine(effectiveStartPosition, effectiveEndPosition);
        Gizmos.color = originalColor;
    }
}