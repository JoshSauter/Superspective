using System;
using SuperspectiveUtils;
using NaughtyAttributes;
using UnityEngine;

public class StaircaseRotate : MonoBehaviour {
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
    public Vector3 effectiveStartPosition => startPosition + (endPosition - startPosition).normalized * startEndGap * Player.instance.scale;

    [ShowIf("DEBUG")]
    [ReadOnly]
    public Vector3 endPosition;
    public Vector3 effectiveEndPosition => endPosition - (endPosition - startPosition).normalized * startEndGap * Player.instance.scale;

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
    }

    void OnTriggerEnter(Collider other) {
        GravityObject gravityObj = other.gameObject.GetComponent<GravityObject>();
        float testLerpValue = 0;
        if (other.TaggedAsPlayer()) {
            baseGravMagnitude = currentGravity.magnitude;
            testLerpValue = GetLerpPositionOfPoint(playerMovement.bottomOfPlayer);
        }
        else if (gravityObj != null) testLerpValue = GetLerpPositionOfPoint(other.transform.position);
        Vector3 testGravityDirection1 =
            Vector3.Lerp(startGravityDirection, endGravityDirection, testLerpValue).normalized;
        Vector3 testGravityDirection2 =
            Vector3.Lerp(startGravityDirection, endGravityDirection, 1 - testLerpValue).normalized;

        if (other.TaggedAsPlayer())
            treatedAsADownStairForPlayer = Vector3.Angle(testGravityDirection1, Physics.gravity.normalized) >
                                           Vector3.Angle(testGravityDirection2, Physics.gravity.normalized);
    }

    void OnTriggerExit(Collider other) {
        float angleToStartDirection = Vector3.Angle(startGravityDirection, Physics.gravity.normalized);
        float angleToEndDirection = Vector3.Angle(endGravityDirection, Physics.gravity.normalized);

        Vector3 exitGravity = angleToStartDirection < angleToEndDirection ? startGravityDirection : endGravityDirection;

        GravityObject gravityObj = other.gameObject.GetComponent<GravityObject>();
        if (other.TaggedAsPlayer()) {
            Physics.gravity = baseGravMagnitude * exitGravity;

            float angleBetween = Vector3.Angle(playerMovement.transform.up, -Physics.gravity.normalized);
            if (treatedAsADownStairForPlayer) angleBetween = -angleBetween;
            playerMovement.transform.rotation =
                Quaternion.FromToRotation(playerMovement.transform.up, -Physics.gravity.normalized) *
                playerMovement.transform.rotation;

            PlayerLook playerLook = PlayerLook.instance;
            playerLook.rotationY -= angleBetween * Vector3.Dot(
                playerMovement.transform.forward,
                playerMovement.ProjectedHorizontalVelocity().normalized
            );
            playerLook.rotationY = Mathf.Clamp(playerLook.rotationY, -playerLook.yClamp, playerLook.yClamp);
        }
        else if (gravityObj != null) gravityObj.gravityDirection = exitGravity;
    }

    void OnTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) {
            t = GetLerpPositionOfPoint(playerMovement.bottomOfPlayer);
            if (treatedAsADownStairForPlayer) t = 1 - t;

            float gravAmplificationFactor = 1;
            if (treatedAsADownStairForPlayer) {
                Vector3 projectedPlayerPos = ClosestPointOnLine(
                    effectiveStartPosition,
                    effectiveEndPosition,
                    playerMovement.bottomOfPlayer
                );
                float distanceFromPlayerToStairs = Vector3.Distance(playerMovement.bottomOfPlayer, projectedPlayerPos);
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
            playerLook.rotationY -= angleBetween * Vector3.Dot(
                playerMovement.transform.forward,
                playerMovement.ProjectedHorizontalVelocity().normalized
            );
            playerLook.rotationY = Mathf.Clamp(playerLook.rotationY, -playerLook.yClamp, playerLook.yClamp);
        }
        else if (other.gameObject.TryGetComponent(out GravityObject gravityObj)) {
            float objT = GetLerpPositionOfPoint(other.transform.position);
            gravityObj.gravityDirection = Vector3.Lerp(startGravityDirection, endGravityDirection, objT).normalized;
        }
    }

    float GetLerpPositionOfPoint(Vector3 point) {
        Vector3 projectedPlayerPos = ClosestPointOnLine(effectiveStartPosition, effectiveEndPosition, point);
        float t = Utils.Vector3InverseLerp(effectiveStartPosition, effectiveEndPosition, projectedPlayerPos);

        //debug.Log("StartPos: " + stairStartPos + ", EndPos: " + stairEndPos + ", PlayerPos: " + playerPos + ", t=" + t);
        return t;
    }

    Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint) {
        Vector3 vVector1 = vPoint - vA;
        Vector3 vVector2 = (vB - vA).normalized;

        float d = Vector3.Distance(vA, vB);
        float t = Vector3.Dot(vVector2, vVector1);

        if (t <= 0)
            return vA;

        if (t >= d)
            return vB;

        Vector3 vVector3 = vVector2 * t;

        Vector3 vClosestPoint = vA + vVector3;

        return vClosestPoint;
    }

    private void OnDrawGizmos() {
        float sphereSize = .25f * Player.instance.scale;
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