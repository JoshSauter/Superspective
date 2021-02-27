using SuperspectiveUtils;
using MagicTriggerMechanics;
using UnityEngine;

[RequireComponent(typeof(MagicTrigger))]
public class TeleportEnter : MonoBehaviour {
    public bool DEBUG;
    public bool teleportPlayer = true;
    public MagicTrigger trigger;
    public Collider teleportEnter;
    public Collider teleportExit;
    public Vector3 teleportOffset = Vector3.zero;
    public Transform[] otherObjectsToTeleport;
    DebugLogger debug;

    // Use this for initialization
    void Awake() {
        debug = new DebugLogger(this, () => DEBUG);
        teleportEnter = GetComponent<Collider>();
        trigger = GetComponent<MagicTrigger>();

        if (otherObjectsToTeleport == null) otherObjectsToTeleport = new Transform[0];
    }

    void OnEnable() {
        trigger.OnMagicTriggerStayOneTime += TeleportTriggered;
    }

    void OnDisable() {
        trigger.OnMagicTriggerStayOneTime -= TeleportTriggered;
    }

    void TeleportTriggered(GameObject player) {
        Debug.Assert(
            teleportExit != null,
            "Please specify the teleport exit for " + gameObject.scene.name + ": " + gameObject.name
        );

        TriggerEventsBeforeTeleport(player);

        Vector3 teleportDisplacement = TeleporterDisplacement() + teleportOffset;
        Vector3 displacementToCenter = teleportEnter.transform.position - player.transform.position;

        TeleportPlayer(player);

        // Handle position and rotation
        Quaternion rotationTransformation =
            teleportExit.transform.rotation * Quaternion.Inverse(teleportEnter.transform.rotation);
        debug.Log(
            "Displacement: " + teleportDisplacement + "\nDisplacementToCenter: " + displacementToCenter +
            "\nRotationTransformation: " + rotationTransformation.eulerAngles
        );

        foreach (Transform otherObject in otherObjectsToTeleport) {
            otherObject.transform.position += displacementToCenter;
            otherObject.transform.rotation = rotationTransformation * otherObject.transform.rotation;
            otherObject.transform.position += teleportDisplacement;
            otherObject.transform.position -=
                teleportExit.transform.TransformVector(teleportEnter.transform.TransformVector(displacementToCenter));
        }

        TriggerEventsAfterTeleport(player);
    }

    float GetRotationAngleBetweenTeleporters() {
        Vector3 forwardEnter = teleportEnter.transform.rotation * Vector3.forward;
        Vector3 forwardExit = teleportExit.transform.rotation * Vector3.forward;
        float angleEnter = Mathf.Rad2Deg * Mathf.Atan2(forwardEnter.x, forwardEnter.z);
        float angleExit = Mathf.Rad2Deg * Mathf.Atan2(forwardExit.x, forwardExit.z);
        return Mathf.DeltaAngle(angleEnter, angleExit);
    }

    Vector3 TeleporterDisplacement() {
        return teleportExit.transform.position - teleportEnter.transform.position;
    }

    void TeleportPlayer(GameObject player) {
        // Handle velocity
        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        Vector3 playerWorldVelocity = playerRigidbody.velocity;
        Vector3 playerRelativeVelocity = teleportEnter.transform.InverseTransformDirection(playerWorldVelocity);
        Vector3 playerTransformedWorldVelocity = teleportExit.transform.TransformDirection(playerRelativeVelocity);
        if (DEBUG) print("Velocity was " + playerWorldVelocity + " but is now " + playerTransformedWorldVelocity);
        playerRigidbody.velocity = playerTransformedWorldVelocity;

        Vector3 playerWorldPos = player.transform.position;
        Vector3 playerLocalPos = teleportEnter.transform.InverseTransformPoint(playerWorldPos);
        Vector3 playerTransformedWorldPos = teleportExit.transform.TransformPoint(playerLocalPos) + teleportOffset;
        player.transform.position = playerTransformedWorldPos;

        Quaternion rotationTransformation =
            teleportExit.transform.rotation * Quaternion.Inverse(teleportEnter.transform.rotation);
        player.transform.rotation = rotationTransformation * player.transform.rotation;
    }

    void TriggerEventsBeforeTeleport(GameObject player) {
        BeforeTeleport?.Invoke(teleportEnter, teleportExit, player);
        BeforeAnyTeleport?.Invoke(teleportEnter, teleportExit, player);
        BeforeTeleportSimple?.Invoke();
        BeforeAnyTeleportSimple?.Invoke();
    }

    void TriggerEventsAfterTeleport(GameObject player) {
        OnTeleport?.Invoke(teleportEnter, teleportExit, player);
        OnAnyTeleport?.Invoke(teleportEnter, teleportExit, player);
        OnTeleportSimple?.Invoke();
        OnAnyTeleportSimple?.Invoke();
    }

#region events
    public delegate void TeleportAction(Collider teleportEnter, Collider teleportExit, GameObject player);

    public delegate void SimpleTeleportAction();

    public event TeleportAction BeforeTeleport;
    public event TeleportAction OnTeleport;
    public event SimpleTeleportAction BeforeTeleportSimple;
    public event SimpleTeleportAction OnTeleportSimple;

    public static event TeleportAction BeforeAnyTeleport;
    public static event TeleportAction OnAnyTeleport;
    public static event SimpleTeleportAction BeforeAnyTeleportSimple;
    public static event SimpleTeleportAction OnAnyTeleportSimple;
#endregion
}