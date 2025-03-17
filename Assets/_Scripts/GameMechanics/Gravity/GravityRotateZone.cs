using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId), typeof(BetterTrigger))]
public class GravityRotateZone : SuperspectiveObject<GravityRotateZone, GravityRotateZone.GravityRotateZoneSave>, BetterTriggers {
    public static bool playerIsInAnyGravityRotateZone = false;

    public enum RotateMode {
        Convex, // e.g. walking from a floor down some stairs to a wall
        Concave // e.g. walking from a floor up some stairs to a wall
    }
    
    [BoxGroup("Primary Settings")]
    [LabelText("Invisible Walls Enabled")]
    [GUIColor(.456f, .567f, .678f)]
    [Tooltip("Whether the invisible walls are enabled when the player is actively changing gravity directions.")]
    public bool invisibleWallsSetting = false;
    private const float INVISIBLE_WALL_TOLERANCE = 0.05f; // Amount of tolerance in the lerp value to enable the invisible wall
    private bool InvisibleWallShouldBeEnabled => invisibleWallsSetting && playerIsInThisGravityRotateZone && t is > INVISIBLE_WALL_TOLERANCE and < 1 - INVISIBLE_WALL_TOLERANCE;
    
    [NaughtyAttributes.HorizontalLine]
    
    [BoxGroup("Primary Settings")]
    [LabelText("Rotate Mode")]
    [GUIColor(1, 1, 0.5f)]
    [Tooltip("Whether the player rotates in a convex or concave manner.\n\nConvex: e.g. walking from a floor down some stairs to a wall.\nConcave: e.g. walking from a floor up some stairs to a wall.")]
    [OnValueChanged(nameof(RecalculateDerivedValues))]
    public RotateMode rotateMode = RotateMode.Convex;
    private bool IsConvex => rotateMode == RotateMode.Convex;
    private bool IsConcave => rotateMode == RotateMode.Concave;
    
    [BoxGroup("Primary Settings")]
    [LabelText("Pivot Point")]
    [GUIColor(1, 1, 0.5f)]
    [Tooltip("The center point around which gravity rotation occurs. Specified in local space.")]
    [OnValueChanged(nameof(RecalculateDerivedValues))]
    public Vector3 pivotPoint;
    public Vector3 WorldPivotPoint => transform.TransformPoint(pivotPoint);

    [NaughtyAttributes.HorizontalLine]
    
    [BoxGroup("Primary Settings")]
    [GUIColor(0.5f, 1, 0.5f)]
    [LabelText("Start Point")]
    [Tooltip("Defines the starting position of the gravity rotation. Specified in local space.")]
    [OnValueChanged(nameof(RecalculateDerivedValues))]
    public Vector3 start = Vector3.right * 5;
    public Vector3 WorldStart => transform.TransformPoint(start);
    
    [BoxGroup("Primary Settings")]
    [GUIColor(0.5f, 1, 0.5f)]
    [LabelText("Start Hitbox Size")]
    [Tooltip("Size of the hitbox at the start of the rotation. Specified in world space.")]
    public Vector2 startHitboxSize = new Vector2(10, 10);

    [BoxGroup("Gravity Settings")]
    [GUIColor(0.5f, 1, 0.5f)]
    [LabelText("Start Gravity Direction")]
    [Tooltip("Gravity direction at the start of the rotation. Should be orthogonal to the axis of rotation. Specified in local space.")]
    [OnValueChanged(nameof(RecalculateDerivedValues))]
    public Vector3 startGravity = Vector3.down;
    public Vector3 WorldStartGravity => transform.TransformDirection(startGravity);
    
    [NaughtyAttributes.HorizontalLine]

    [BoxGroup("Primary Settings")]
    [GUIColor(1, 0.5f, 0.5f)]
    [LabelText("End Point")]
    [Tooltip("Defines the ending position of the gravity rotation. Specified in local space.")]
    [OnValueChanged(nameof(RecalculateDerivedValues))]
    public Vector3 end = Vector3.up * 5;
    public Vector3 WorldEnd => transform.TransformPoint(end);
    
    [BoxGroup("Primary Settings")]
    [GUIColor(1, 0.5f, 0.5f)]
    [LabelText("End Hitbox Size")]
    [Tooltip("Size of the hitbox at the end of the rotation. Specified in world space.")]
    public Vector2 endHitboxSize = new Vector2(10, 10);

    [BoxGroup("Gravity Settings")]
    [GUIColor(1, 0.5f, 0.5f)]
    [LabelText("End Gravity Direction")]
    [Tooltip("Gravity direction at the end of the rotation. Should be orthogonal to the axis of rotation. Specified in local space.")]
    [OnValueChanged(nameof(RecalculateDerivedValues))]
    public Vector3 endGravity = Vector3.left;
    public Vector3 WorldEndGravity => transform.TransformDirection(endGravity);
    
    [NaughtyAttributes.HorizontalLine]

    [BoxGroup("Gravity Settings")]
    [GUIColor(0.875f, .65f, 1f)]
    [LabelText("Artificially Increase Player Gravity Magnitude")]
    [ShowIf(nameof(IsConvex))]
    [Tooltip("If true, the player's gravity magnitude will be increased by an amount varying by their distance from the line segment connecting the start and end planes (shown by the purple line in scene view).")]
    public bool artificialGravityAmplification = false;
    private bool ShowArtificialGravityAmplificationSettings => IsConvex && artificialGravityAmplification;
    
    [BoxGroup("Gravity Settings")]
    [GUIColor(0.875f, .65f, 1f)]
    [ShowIf(nameof(ShowArtificialGravityAmplificationSettings))]
    [LabelText("Gravity Amplification Magnitude")]
    [Tooltip("The maximum amount by which to amplify the player's gravity magnitude.")]
    public float gravAmplificationMagnitude = 8f;
    
    [BoxGroup("Gravity Settings")]
    [GUIColor(0.875f, .65f, 1f)]
    [ShowIf(nameof(ShowArtificialGravityAmplificationSettings))]
    [LabelText("Max Distance for Gravity Amplification")]
    [Tooltip("The distance from the player to the line segment connecting the start and end planes at which gravity amplification is at its maximum.")]
    public float maxDistanceForGravAmplification = 8f;
    
    [BoxGroup("Gravity Settings")]
    [GUIColor(0.875f, .65f, 1f)]
    [ShowIf(nameof(ShowArtificialGravityAmplificationSettings))]
    [LabelText("Min Distance for Gravity Amplification")]
    [Tooltip("The distance from the player to the line segment connecting the start and end planes at which gravity amplification begins.")]
    public float minDistanceForGravAmplification = 4f;

    private bool ShowGravityLine => ShowArtificialGravityAmplificationSettings || IsConcave;
    
    [BoxGroup("Derived Values")]
    [ReadOnly]
    [GUIColor(0.75f, 0.75f, 1f)]
    [LabelText("Gravity Line Start")]
    [Tooltip("The start point of the line segment connecting the start and end planes. Specified in local space.")]
    [ShowIf(nameof(ShowGravityLine))]
    [SerializeField]
    private Vector3 gravityLineStart;
    private Vector3 WorldGravityLineStart => transform.TransformPoint(gravityLineStart);
    
    [BoxGroup("Derived Values")]
    [ReadOnly]
    [GUIColor(0.75f, 0.75f, 1f)]
    [LabelText("Gravity Line End")]
    [Tooltip("The end point of the line segment connecting the start and end planes. Specified in local space.")]
    [ShowIf(nameof(ShowGravityLine))]
    [SerializeField]
    private Vector3 gravityLineEnd;
    private Vector3 WorldGravityLineEnd => transform.TransformPoint(gravityLineEnd);
    
    [BoxGroup("Derived Values")]
    [ReadOnly]
    [GUIColor(0.75f, 0.75f, 1f)]
    [LabelText("Rotation Axis")]
    [Tooltip("Automatically calculated based on start and end positions. Specified in local space.")]
    [SerializeField]
    private Vector3 rotationAxis;
    private Vector3 WorldRotationAxis => transform.TransformDirection(rotationAxis);

    [BoxGroup("Derived Values")]
    [ReadOnly]
    [LabelText("Total Rotation Angle")]
    [GUIColor(0.75f, 0.75f, 1f)]
    [Tooltip("The total rotation in degrees between start and end points. Should be 90\u00b0.")]
    [SerializeField]
    private float totalAngle;
    
    [BoxGroup("State")]
    [ReadOnly]
    [GUIColor(0.875f, .65f, 1f)]
    [LabelText("Player Is In This Gravity Rotate Zone")]
    [Tooltip("Whether the player is currently in this gravity rotate zone.")]
    [SerializeField]
    public bool playerIsInThisGravityRotateZone = false;

    [BoxGroup("State")]
    [ReadOnly]
    [GUIColor(0.875f, .65f, 1f)]
    [LabelText("All GravityObjects In Zone")]
    [Tooltip("A collection of all GravityObjects in this gravity rotate zone.")]
    [ShowInInspector]
    private List<GravityObject> allGravityObjectsInZoneList => allGravityObjectsInZone.ToList();
    private readonly HashSet<GravityObject> allGravityObjectsInZone = new HashSet<GravityObject>();
    
    [BoxGroup("State")]
    [ReadOnly]
    [GUIColor(0.875f, .65f, 1f)]
    [LabelText("Last Player Lerp Value")]
    [Tooltip("Normalized value between 0 and 1 representing the player's position between start and end.")]
    [SerializeField]
    private float t;
    
    [BoxGroup("State")]
    [ReadOnly]
    [GUIColor(0.875f, .65f, 1f)]
    [LabelText("Gravity Amplification Factor")]
    [Tooltip("The factor by which the player's gravity magnitude is amplified.")]
    [ShowIf(nameof(artificialGravityAmplification))]
    [SerializeField]
    private float gravAmplificationFactor = 1;
    
    [BoxGroup("State")]
    [ReadOnly]
    [GUIColor(.456f, .567f, .678f)]
    [LabelText("Invisible wall currently active")]
    [ShowIf(nameof(invisibleWallsSetting))]
    [Tooltip("Whether the invisible wall is enabled. Should only be true while the player is actively rotating the gravity and is between the start and end gravity directions.")]
    public bool invisibleWallEnabled = false;
    
    [BoxGroup("References")]
    [LabelText("Trigger Zone")]
    [Tooltip("The trigger zone that defines the area in which gravity rotation occurs. Use RegenerateTriggerZone to dynamically create the trigger zone mesh.")]
    public MeshCollider triggerZone;
    
    [BoxGroup("References")]
    [LabelText("Invisible Wall")]
    [GUIColor(.456f, .567f, .678f)]
    [Tooltip("The invisible wall on either side of the trigger zone, to keep the player from exiting the zone mid-rotation.")]
    public MeshCollider invisibleWall;
    
    private Vector3 StartDirection => (start - pivotPoint).normalized;
    private Vector3 WorldStartDirection => transform.TransformDirection(StartDirection);
    private Vector3 EndDirection => (end - pivotPoint).normalized;
    private Vector3 WorldEndDirection => transform.TransformDirection(EndDirection);

    private const float LERP_TOLERANCE_FOR_PLAYER_ENTRY = 0.05f;

    [ContextMenu("Swap Start and End")]
    public void SwapStartAndEnd() {
        Vector3 temp = start;
        start = end;
        end = temp;
        
        temp = startGravity;
        startGravity = endGravity;
        endGravity = temp;

        Vector2 temp2 = startHitboxSize;
        startHitboxSize = endHitboxSize;
        endHitboxSize = temp2;
        
        RecalculateDerivedValues();
    }

    protected override void Awake() {
        base.Awake();
        RecalculateDerivedValues();
    }
    
    protected override void Start() {
        base.Start();
        UpdateInvisibleWallsEnabled();
    }

    private void RecalculateDerivedValues() {
        if (start == end) return; // Prevent division by zero or undefined axis

        // Compute rotation axis as the normal of the plane defined by pivot, start, and end
        rotationAxis = Vector3.Cross(StartDirection, EndDirection).normalized;

        // Compute total rotation angle in degrees, validate it's 90 degrees
        totalAngle = Vector3.SignedAngle(StartDirection, EndDirection, rotationAxis);
        if (!Mathf.Approximately(totalAngle, 90)) {
            Debug.LogError($"Start and End points are not 90 degrees apart. Total angle is {totalAngle} degrees.");
        }

        gravityLineStart = pivotPoint + start.normalized * (start.magnitude - startHitboxSize.y / 2f);
        gravityLineEnd = pivotPoint + end.normalized * (end.magnitude - endHitboxSize.y / 2f);

        if (IsConcave) {
            artificialGravityAmplification = false;
        }
        gravAmplificationFactor = 1;
    }

    public float GetLerpValue(Vector3 position) {
        Vector3 projectedPosition = Vector3.ProjectOnPlane(position, rotationAxis);
        if (IsConvex) {
            Vector3 playerVector = projectedPosition - pivotPoint;

            // Compute signed angle between start direction and player position
            float angle = Vector3.SignedAngle(StartDirection, playerVector, rotationAxis);

            debug.Log($"Player vector: {playerVector}, Angle: {angle}");

            // Normalize t to be between 0 and 1
            return Mathf.Clamp01(angle / totalAngle);
        }
        else {
            Vector3 closestPointOnLine = projectedPosition.GetClosestPointOnFiniteLine(gravityLineStart, gravityLineEnd);
            float t = Utils.Vector3InverseLerp(gravityLineStart, gravityLineEnd, closestPointOnLine);

            return Easing.InverseSmoothStep(t);
        }
    }

    public Vector3 GetInterpolatedGravityDirection(Vector3 worldPosition) {
        Vector3 position = transform.InverseTransformPoint(worldPosition);
        float nextT = Mathf.Clamp01(GetLerpValue(position));
        Vector3 desiredGravity = Vector3.Lerp(startGravity, endGravity, nextT).normalized;
        
        debug.Log($"Position: {position}, prev t: {t}, next t: {nextT}, t speed: {(nextT - t) / Time.fixedDeltaTime} Gravity: {desiredGravity}");
        t = nextT;
        return desiredGravity;
    }

    public Vector3 GetStartOrEndGravityDirection(Vector3 worldPosition) {
        Vector3 position = transform.InverseTransformPoint(worldPosition);
        float t = Mathf.Clamp01(GetLerpValue(position));
        
        return t < 0.5f ? startGravity : endGravity;
    }
    
    bool ValidateGravityDirection(Vector3 testGravityDirectionLocal, out Vector3 matchingGravityDirectionLocal) {
        // Check if the test gravity direction is close to either start or end gravity direction
        if (Vector3.Dot(testGravityDirectionLocal.normalized, startGravity.normalized) > 0.95f) {
            matchingGravityDirectionLocal = startGravity;
            return true;
        }
        else if (Vector3.Dot(testGravityDirectionLocal.normalized, endGravity.normalized) > 0.95f) {
            matchingGravityDirectionLocal = endGravity;
            return true;
        }

        matchingGravityDirectionLocal = Vector3.zero;
        return false;
    }

    bool ValidateEntry(Vector3 entryPositionWorld, Vector3 enteringObjectGravityWorld) {
        Vector3 entryPositionLocal = transform.InverseTransformPoint(entryPositionWorld);
        Vector3 entryGravityLocal = transform.InverseTransformDirection(enteringObjectGravityWorld);

        // First test that the gravity currently affecting the object matches either the start or end gravity
        if (ValidateGravityDirection(entryGravityLocal, out Vector3 matchingGravityDirectionLocal)) {
            if (IsConvex) return true; // Convex rotation mode doesn't require any additional checks
            // TODO: Check the above ^
            
            // Then make sure the object is entering the trigger zone close to that gravity direction
            float lerpValue = GetLerpValue(entryPositionLocal);
            bool start = matchingGravityDirectionLocal == startGravity;
            bool end = matchingGravityDirectionLocal == endGravity;
            
            return (start && lerpValue < LERP_TOLERANCE_FOR_PLAYER_ENTRY) || (end && lerpValue > 1 - LERP_TOLERANCE_FOR_PLAYER_ENTRY);
        }
        return false;
    }

    bool ValidateEntryConditionsForPlayer() {
        Vector3 gravityDirectionWorld = Physics.gravity.normalized;
        Vector3 playerPosWorld = PlayerMovement.instance.BottomOfPlayer;
        
        return ValidateEntry(playerPosWorld, gravityDirectionWorld);
    }
    
    bool ValidateEntryConditionsForGravityObject(GravityObject gravityObj) {
        return ValidateEntry(gravityObj.transform.position, gravityObj.GravityDirection);
    }

    private void PlayerEnterRotateZone() {
        playerIsInAnyGravityRotateZone = true;
        playerIsInThisGravityRotateZone = true;
            
        PlayerMovement playerMovement = PlayerMovement.instance;
        Vector3 playerWorldPos = playerMovement.BottomOfPlayer;

        Vector3 desiredGravityLocal = GetInterpolatedGravityDirection(playerWorldPos).normalized;
        SetGravityForPlayer(desiredGravityLocal, 1, false);
    }

    private void GravityObjectEnterRotateZone(GravityObject gravityObj) {
        allGravityObjectsInZone.Add(gravityObj);
            
        Vector3 desiredGravityDirection = GetInterpolatedGravityDirection(gravityObj.transform.position).normalized;
        SetGravityForGravityObject(gravityObj, desiredGravityDirection);
    }

    public void OnBetterTriggerEnter(Collider other) {
        if (other.TaggedAsPlayer() && ValidateEntryConditionsForPlayer()) {
            PlayerEnterRotateZone();
        }
        else if (other.gameObject.TryGetComponent(out GravityObject gravityObj) && ValidateEntryConditionsForGravityObject(gravityObj)) {
            GravityObjectEnterRotateZone(gravityObj);
        }
    }

    public void OnBetterTriggerStay(Collider other) {
        if (other.TaggedAsPlayer() && (playerIsInThisGravityRotateZone || ValidateEntryConditionsForPlayer())) {
            if (!playerIsInThisGravityRotateZone) {
                PlayerEnterRotateZone();
            }
            playerIsInAnyGravityRotateZone = true;
            playerIsInThisGravityRotateZone = true;
            
            PlayerMovement playerMovement = PlayerMovement.instance;
            Vector3 playerWorldPos = playerMovement.BottomOfPlayer;

            Vector3 desiredGravityLocal = GetInterpolatedGravityDirection(playerWorldPos).normalized;
            
            // Artificially amp up the gravity magnitude as the player is further from the line connecting the start and end planes (purple line in scene view)
            float nextGravAmplificationFactor;
            if (artificialGravityAmplification) {
                nextGravAmplificationFactor = GetGravityAmplificationFactor(playerWorldPos);
            }
            else {
                nextGravAmplificationFactor = 1;
            }
            SetGravityForPlayer(desiredGravityLocal, nextGravAmplificationFactor);
        }
        else if (other.gameObject.TryGetComponent(out GravityObject gravityObj) && (allGravityObjectsInZone.Contains(gravityObj) || ValidateEntryConditionsForGravityObject(gravityObj))) {
            GravityObjectEnterRotateZone(gravityObj);
        }
    }

    public void OnBetterTriggerExit(Collider other) {
        if (other.TaggedAsPlayer() && playerIsInThisGravityRotateZone) {
            playerIsInAnyGravityRotateZone = false;
            playerIsInThisGravityRotateZone = false;
            
            PlayerMovement playerMovement = PlayerMovement.instance;
            Vector3 playerWorldPos = playerMovement.BottomOfPlayer;
            Vector3 desiredGravityLocal = GetStartOrEndGravityDirection(playerWorldPos).normalized;
            
            SetGravityForPlayer(desiredGravityLocal, 1, false);
        }
        else if (other.gameObject.TryGetComponent(out GravityObject gravityObj) && allGravityObjectsInZone.Contains(gravityObj)) {
            Vector3 desiredGravityDirection = GetStartOrEndGravityDirection(other.transform.position).normalized;
            SetGravityForGravityObject(gravityObj, desiredGravityDirection);
            
            allGravityObjectsInZone.Remove(gravityObj);
        }
    }

    private void SetGravityForPlayer(Vector3 desiredGravityLocal, float nextGravAmplificationFactor = 1, bool movePlayerVision = true) {
        debug.Log($"Setting gravity for player to {desiredGravityLocal} x {nextGravAmplificationFactor:F3}");
        Vector3 desiredGravityWorld = transform.TransformDirection(desiredGravityLocal);
        
        PlayerMovement playerMovement = PlayerMovement.instance;
        
        Vector3 unscaledGravity = Physics.gravity / gravAmplificationFactor;
        Vector3 nextScaledGravity = unscaledGravity * nextGravAmplificationFactor;
        Physics.gravity = nextScaledGravity.magnitude * desiredGravityWorld.normalized;
        gravAmplificationFactor = nextGravAmplificationFactor;

        float angleBetween = -Vector3.Angle(playerMovement.transform.up, -Physics.gravity.normalized);
        if (IsConcave) angleBetween *= -1;
        PlayerMovement.instance.transform.rotation =
            Quaternion.FromToRotation(playerMovement.transform.up, -Physics.gravity.normalized) *
            playerMovement.transform.rotation;

        UpdateInvisibleWallsEnabled();

        // Rotate the player's camera
        if (!movePlayerVision) return;
        PlayerLook playerLook = PlayerLook.instance;
        playerLook.RotationY -= angleBetween * Vector3.Dot(
            playerMovement.transform.forward,
            playerMovement.ProjectedHorizontalVelocity().normalized
        );
        debug.Log($"AngleBetween: {angleBetween:F3}\nUnclamped RotationY: {playerLook.RotationY}");
        playerLook.RotationY = Mathf.Clamp(playerLook.RotationY, -playerLook.yClamp, playerLook.yClamp);
    }
    
    private void SetGravityForGravityObject(GravityObject gravityObj, Vector3 desiredGravityLocal) {
        Vector3 desiredGravityWorld = transform.TransformDirection(desiredGravityLocal);
        debug.Log($"Setting gravity direction for {gravityObj.name} to {desiredGravityWorld}");
        gravityObj.GravityDirection = desiredGravityWorld;
    }

    private float GetGravityAmplificationFactor(Vector3 playerWorldPos) {
        Vector3 position = transform.InverseTransformPoint(playerWorldPos);
        Vector3 playerPositionOnPlane = Vector3.ProjectOnPlane(position, rotationAxis);
        float distanceFromPlayerToStairs = Vector3.Distance(playerPositionOnPlane, playerPositionOnPlane.GetClosestPointOnFiniteLine(gravityLineStart, gravityLineEnd));
        return 1 + gravAmplificationMagnitude * Mathf.InverseLerp(
            minDistanceForGravAmplification,
            maxDistanceForGravAmplification,
            distanceFromPlayerToStairs
        );
    }

    private void UpdateInvisibleWallsEnabled() {
        invisibleWallEnabled = InvisibleWallShouldBeEnabled;
        if (invisibleWallEnabled && invisibleWall == null) {
            // This will log a warning if the invisible wall doesn't exist yet because we really should always bake it into the scene data
            RegenerateInvisibleWall();
        }

        if (invisibleWall != null) {
            invisibleWall.gameObject.SetActive(invisibleWallEnabled);
        }
    }

    private void OnDrawGizmosSelected() {
        Vector3 worldPivot = WorldPivotPoint;
        Vector3 worldStart = WorldStart;
        Vector3 worldEnd = WorldEnd;
        Vector3 worldAxis = WorldRotationAxis;

        float sphereRadiusStart = startHitboxSize.magnitude / 100f;
        float sphereRadiusEnd = endHitboxSize.magnitude / 100f;
        float sphereRadiusMiddle = sphereRadiusEnd + sphereRadiusStart / 2f;
        
        // Draw spheres at the start, end, and pivot points
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(worldPivot, sphereRadiusMiddle);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(worldStart, sphereRadiusStart);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(worldEnd, sphereRadiusEnd);
        
        // Draw planes at the hitboxes
        float height = startHitboxSize.y;
        float width = startHitboxSize.x;
        if (IsConvex) {
            Vector3 xDelta = WorldRotationAxis * startHitboxSize.x / 2f;
            Vector3 yDirection = IsConvex ? WorldStartDirection : WorldEndDirection;
            Vector3 yDelta = yDirection * startHitboxSize.y / 2f;
            ExtDebug.DrawPlane(worldStart, -WorldEndDirection, xDelta.normalized, yDelta.normalized, height, width, Color.green);
        
            height = endHitboxSize.y;
            width = endHitboxSize.x;
            
            xDelta = WorldRotationAxis * endHitboxSize.x / 2f;
            yDirection = IsConvex ? WorldEndDirection : WorldStartDirection;
            yDelta = yDirection * endHitboxSize.y / 2f;
            ExtDebug.DrawPlane(worldEnd, -WorldStartDirection, xDelta.normalized, yDelta.normalized, height, width, Color.red);
        }
        else {
            Vector3 planeCenterPoint = start + (EndDirection * height / 2f);
            Vector3 worldPlaneCenterPoint = transform.TransformPoint(planeCenterPoint);
            
            Vector3 xDelta = WorldRotationAxis * startHitboxSize.x / 2f;
            Vector3 yDirection = IsConvex ? WorldStartDirection : WorldEndDirection;
            Vector3 yDelta = yDirection * startHitboxSize.y / 2f;
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(worldPlaneCenterPoint, sphereRadiusStart);
            ExtDebug.DrawPlane(worldPlaneCenterPoint, -WorldStartDirection, xDelta.normalized, yDelta.normalized, height, width, Color.green);
            
            planeCenterPoint = end + (StartDirection * height / 2f);
            worldPlaneCenterPoint = transform.TransformPoint(planeCenterPoint);
            
            xDelta = WorldRotationAxis * endHitboxSize.x / 2f;
            yDirection = IsConvex ? WorldEndDirection : WorldStartDirection;
            yDelta = yDirection * endHitboxSize.y / 2f;
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldPlaneCenterPoint, sphereRadiusEnd);
            ExtDebug.DrawPlane(worldPlaneCenterPoint, -WorldEndDirection, xDelta.normalized, yDelta.normalized, height, width, Color.red);
        }
        
        Gizmos.color = Color.white;
        Gizmos.DrawLine(worldPivot, worldStart);
        Gizmos.DrawLine(worldPivot, worldEnd);

        Gizmos.color = Color.blue;
        Vector3 endOfNormalVector = worldAxis * (start.magnitude + end.magnitude) / 2f;
        Gizmos.DrawSphere(worldPivot + endOfNormalVector, sphereRadiusMiddle);
        Gizmos.DrawRay(worldPivot, endOfNormalVector);

        Vector3 playerLocalPos = transform.InverseTransformPoint(PlayerMovement.instance.BottomOfPlayer);
        Vector3 projectedPlayerLocalPos = Vector3.ProjectOnPlane(playerLocalPos, rotationAxis);
        Vector3 projectedPlayerWorldPos = transform.TransformPoint(projectedPlayerLocalPos);
        if (playerIsInThisGravityRotateZone) {
            // Draw the line from the player to the pivot point
            Gizmos.color = new Color(1f, 1f, .5f);
            Gizmos.DrawSphere(projectedPlayerWorldPos, sphereRadiusMiddle);
            Gizmos.DrawLine(worldPivot, projectedPlayerWorldPos);

            // Draw the line from the player to the gravity amplification point
            // The same point is also used as the lerp value for concave rotation mode, so we'll draw it for that too
            if (artificialGravityAmplification || IsConcave) {
                float sphereSize = sphereRadiusMiddle / 2f;
                Vector3 gravAmplifyPoint = projectedPlayerLocalPos.GetClosestPointOnFiniteLine(gravityLineStart, gravityLineEnd);
                Vector3 worldGravAmplifyPoint = transform.TransformPoint(gravAmplifyPoint);
                Gizmos.DrawLine(worldGravAmplifyPoint, projectedPlayerWorldPos);
                Gizmos.DrawSphere(worldGravAmplifyPoint, sphereSize);
            }
        }

        // Draw the line between gravity start and end points
        // This same line segment is also used for determining concave rotations, so we'll draw it for that as well
        if (artificialGravityAmplification || IsConcave) {
            Gizmos.color = new Color(0.65f, .25f, 1f);
            Vector3 worldGravityLineStart = transform.TransformPoint(gravityLineStart);
            Vector3 worldGravityLineEnd = transform.TransformPoint(gravityLineEnd);
            Gizmos.DrawLine(worldGravityLineStart, worldGravityLineEnd);
            Gizmos.DrawSphere(worldGravityLineStart, sphereRadiusMiddle);
            Gizmos.DrawSphere(worldGravityLineEnd, sphereRadiusMiddle);
        }

        // Draw spheres at selected vertex for debug mode
        if (!DEBUG) return;
        if (_vertices == null || _vertices.Length == 0) return;
        Gizmos.color = Color.cyan;
        int clampedIndex = Mathf.Clamp(selectedVertex, 0, _vertices.Length - 1);
        Gizmos.DrawSphere(transform.TransformPoint(_vertices[clampedIndex]), sphereRadiusMiddle);
        
        if (_invisibleWallVertices == null) return;
        Gizmos.color = Color.black;
        foreach (Vector3 invisibleWallVertex in _invisibleWallVertices) {
            Gizmos.DrawSphere(transform.TransformPoint(invisibleWallVertex), 0.085f);
        }
    }

#region Dynamic Hitbox Generation

    private Mesh _triggerZoneMesh;
    [SerializeField]
    [HideInInspector]
    private Vector3[] _vertices;
    [ShowIf(nameof(DEBUG))]
    [GUIColor(0, 1f, 1f)]
    [Range(0, NUM_VERTICES-1)]
    public int selectedVertex = 0;
    private const int NUM_VERTICES = 10;
    
    private Mesh _invisibleWallMesh;
    [SerializeField]
    [HideInInspector]
    private Vector3[] _invisibleWallVertices;
    private const int NUM_VERTICES_CONCAVE_INVIS_WALL = 12;

    [ContextMenu("Regenerate Trigger Zone")]
    [Button("Regenerate Trigger Zone")]
    private void RegenerateTriggerZone() {
        RecalculateDerivedValues();
        
        if (_triggerZoneMesh == null) {
            _triggerZoneMesh = new Mesh() {
                name = "GravityRotateZone Mesh"
            };
        }
        else {
            _triggerZoneMesh.Clear();
        }
        
        SetVertices();
        
        _triggerZoneMesh.vertices = _vertices;
        _triggerZoneMesh.RecalculateBounds();

        if (triggerZone == null) {
            triggerZone = this.GetOrAddComponent<MeshCollider>();
        }

        triggerZone.convex = true;
        triggerZone.isTrigger = true;
        triggerZone.sharedMesh = _triggerZoneMesh;
        gameObject.layer = SuperspectivePhysics.TriggerZoneLayer;
        this.GetOrAddComponent<BetterTrigger>().trigger = triggerZone;

        if (invisibleWallsSetting) {
            RegenerateInvisibleWall();
        }
    }

    private void RegenerateInvisibleWall() {
        if (Application.isPlaying) {
            debug.LogWarning("Regenerating invisible wall in play mode. You should really bake this into the scene.", true);
        }
        
        SetInvisWallVertices();
        int[] triangles = GetInvisWallTriangles();
        
        if (_invisibleWallMesh == null) {
            _invisibleWallMesh = new Mesh() {
                name = "GravityRotateZone Player Invisible Wall Mesh"
            };
        }
        else {
            _invisibleWallMesh.Clear();
        }
        
        _invisibleWallMesh.vertices = _invisibleWallVertices;
        _invisibleWallMesh.triangles = triangles;
        _invisibleWallMesh.RecalculateBounds();
        _invisibleWallMesh.RecalculateNormals();

        if (invisibleWall == null) {
            GameObject invisibleWallGO = new GameObject("Invisible Wall");
            invisibleWallGO.transform.SetParent(transform, false);
            invisibleWallGO.layer = SuperspectivePhysics.CollideWithPlayerOnlyLayer;
            invisibleWall = invisibleWallGO.AddComponent<MeshCollider>();
        }
        invisibleWall.sharedMesh = _invisibleWallMesh;
    }
    
    [ContextMenu("Clear Trigger Zone")]
    [Button("Clear Trigger Zone")]
    private void ClearTriggerZone() {
        if (triggerZone != null) {
            triggerZone.sharedMesh = null;
        }
        
        if (_triggerZoneMesh != null) {
            _triggerZoneMesh.Clear();
        }

        _triggerZoneMesh = null;
        _vertices = null;

        ClearInvisibleWall();
    }

    private void ClearInvisibleWall() {
        if (invisibleWall != null) {
            DestroyImmediate(invisibleWall.gameObject);
            invisibleWall = null;
        }
        
        if (_invisibleWallMesh != null) {
            _invisibleWallMesh.Clear();
        }
        
        _invisibleWallMesh = null;
        _invisibleWallVertices = null;
    }

    private void SetVertices() {
        _vertices = new Vector3[NUM_VERTICES];
        
        // First 4 vertices are the corners of the start hitbox
        Vector3 startPoint = start;
        Vector3 endPoint = end;
        if (IsConcave) {
            startPoint += EndDirection * startHitboxSize.y / 2f;
            endPoint += StartDirection * endHitboxSize.y / 2f;
        }
        
        Vector3 xDelta = rotationAxis * startHitboxSize.x / 2f;
        Vector3 yDirection = IsConvex ? StartDirection : EndDirection;
        Vector3 yDelta = yDirection * startHitboxSize.y / 2f;
        _vertices[0] = startPoint + xDelta + yDelta;
        _vertices[1] = startPoint + xDelta - yDelta;
        _vertices[2] = startPoint - xDelta + yDelta;
        _vertices[3] = startPoint - xDelta - yDelta;
        
        // Next 4 vertices are the corners of the end hitbox
        xDelta = rotationAxis * endHitboxSize.x / 2f;
        yDirection = IsConvex ? EndDirection : StartDirection;
        yDelta = yDirection * endHitboxSize.y / 2f;
        _vertices[4] = endPoint + xDelta + yDelta;
        _vertices[5] = endPoint + xDelta - yDelta;
        _vertices[6] = endPoint - xDelta + yDelta;
        _vertices[7] = endPoint - xDelta - yDelta;
        
        // Last 2 vertices are where the outer edges of the hitboxes intersect
        Vector3 xOffset = rotationAxis * Mathf.Max(startHitboxSize.x, endHitboxSize.x) / 2f;
        if (IsConvex) {
            Vector3 projected0 = Vector3.ProjectOnPlane(_vertices[0], rotationAxis);
            Vector3 projected4 = Vector3.ProjectOnPlane(_vertices[4], rotationAxis);
            _vertices[8] = projected0 + projected4 + xOffset;
            _vertices[9] = projected0 + projected4 - xOffset;
        }
        else {
            _vertices[8] = pivotPoint + xOffset;
            _vertices[9] = pivotPoint - xOffset;
        }
    }

    private void SetInvisWallVertices() {
        if (IsConcave) {
            _invisibleWallVertices = new Vector3[NUM_VERTICES_CONCAVE_INVIS_WALL];
            
            Vector3 sideOffsetStart = rotationAxis * startHitboxSize.x / 2f;
            Vector3 sideOffsetEnd = rotationAxis * endHitboxSize.x / 2f;

            // Flipped directions for concave plane height
            Vector3 heightStart = EndDirection * startHitboxSize.y;
            Vector3 heightEnd = StartDirection * endHitboxSize.y;
            
            _invisibleWallVertices[0] = start + sideOffsetStart;
            _invisibleWallVertices[1] = gravityLineStart + sideOffsetStart;
            _invisibleWallVertices[2] = _invisibleWallVertices[0] + heightStart;
            _invisibleWallVertices[3] = gravityLineEnd + sideOffsetEnd;
            _invisibleWallVertices[4] = end + sideOffsetEnd;
            _invisibleWallVertices[5] = _invisibleWallVertices[4] + heightEnd;
            
            int half = NUM_VERTICES_CONCAVE_INVIS_WALL / 2;
            for (int i = half; i < NUM_VERTICES_CONCAVE_INVIS_WALL; i++) {
                int referenceIndex = i - half;
                Vector3 offset = -2f * (referenceIndex < half ? sideOffsetStart : sideOffsetEnd);
                _invisibleWallVertices[i] = _invisibleWallVertices[referenceIndex] + offset;
            }
        }
        else {
            _invisibleWallVertices = new Vector3[NUM_VERTICES];

            // Left wall
            _invisibleWallVertices[0] = _vertices[0];
            _invisibleWallVertices[1] = _vertices[1];
            _invisibleWallVertices[2] = _vertices[8];
            _invisibleWallVertices[3] = _vertices[5];
            _invisibleWallVertices[4] = _vertices[4];
            
            // Right wall
            _invisibleWallVertices[5] = _vertices[2];
            _invisibleWallVertices[6] = _vertices[3];
            _invisibleWallVertices[7] = _vertices[7];
            _invisibleWallVertices[8] = _vertices[6];
            _invisibleWallVertices[9] = _vertices[9];
        }
    }

    // The reason this is a getter and not setting a field on this object is because we don't use triangles for debug display,
    // unlike the vertices. I'd do them all as pure function getters if not for OnDrawGizmosSelected needing easy access to this data.
    private int[] GetInvisWallTriangles() {
        if (IsConcave) {
            return new int[] {
                // Left wall
                0, 1, 2,
                1, 3, 2,
                2, 3, 5,
                3, 4, 5,
                // Right wall
                8, 7, 6,
                8, 9, 7,
                11, 9, 8,
                11, 10, 9
            };
        }
        else {
            return new int[] {
                0, 1, 2,
                1, 3, 2,
                2, 3, 4,

                9, 6, 5,
                9, 7, 6,
                9, 8, 7
            };
        }
    }
#endregion

#region Saving
    [Serializable]
    public class GravityRotateZoneSave : SaveObject<GravityRotateZone> {
        public RotateMode rotateMode;
        public SerializableVector3 pivotPoint;
        public SerializableVector3 start;
        public SerializableVector2 startHitboxSize;
        public SerializableVector3 startGravity;
        public SerializableVector3 end;
        public SerializableVector2 endHitboxSize;
        public SerializableVector3 endGravity;
        public bool artificialGravityAmplification;
        public float gravAmplificationMagnitude;
        public float maxDistanceForGravAmplification;
        public float minDistanceForGravAmplification;
        public SerializableVector3 gravityLineStart;
        public SerializableVector3 gravityLineEnd;
        public SerializableVector3 rotationAxis;
        public float totalAngle;
        public bool playerIsInThisGravityRotateZone;
        public float t;
        public float gravAmplificationFactor;

        public GravityRotateZoneSave(GravityRotateZone script) : base(script) {
            this.rotateMode = script.rotateMode;
            this.pivotPoint = script.pivotPoint;
            this.start = script.start;
            this.startHitboxSize = script.startHitboxSize;
            this.startGravity = script.startGravity;
            this.end = script.end;
            this.endHitboxSize = script.endHitboxSize;
            this.endGravity = script.endGravity;
            this.artificialGravityAmplification = script.artificialGravityAmplification;
            this.gravAmplificationMagnitude = script.gravAmplificationMagnitude;
            this.maxDistanceForGravAmplification = script.maxDistanceForGravAmplification;
            this.minDistanceForGravAmplification = script.minDistanceForGravAmplification;
            this.gravityLineStart = script.gravityLineStart;
            this.gravityLineEnd = script.gravityLineEnd;
            this.rotationAxis = script.rotationAxis;
            this.totalAngle = script.totalAngle;
            this.playerIsInThisGravityRotateZone = script.playerIsInThisGravityRotateZone;
            this.t = script.t;
            this.gravAmplificationFactor = script.gravAmplificationFactor;
        }
    }

    public override void LoadSave(GravityRotateZoneSave save) {
        rotateMode = save.rotateMode;
        pivotPoint = save.pivotPoint;
        start = save.start;
        startHitboxSize = save.startHitboxSize;
        startGravity = save.startGravity;
        end = save.end;
        endHitboxSize = save.endHitboxSize;
        endGravity = save.endGravity;
        artificialGravityAmplification = save.artificialGravityAmplification;
        gravAmplificationMagnitude = save.gravAmplificationMagnitude;
        maxDistanceForGravAmplification = save.maxDistanceForGravAmplification;
        minDistanceForGravAmplification = save.minDistanceForGravAmplification;
        gravityLineStart = save.gravityLineStart;
        gravityLineEnd = save.gravityLineEnd;
        rotationAxis = save.rotationAxis;
        totalAngle = save.totalAngle;
        playerIsInThisGravityRotateZone = save.playerIsInThisGravityRotateZone;
        t = save.t;
        gravAmplificationFactor = save.gravAmplificationFactor;

        RegenerateTriggerZone();
    }
    #endregion
}

