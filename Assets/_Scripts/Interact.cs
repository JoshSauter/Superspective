using System;
using Audio;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.UI;

// Only handles the system governing interactable objects, all the juice for this is in InteractJuice.cs
public class Interact : Singleton<Interact> {
    public const float defaultInteractionDistance = 6.5f;
    public bool DEBUG;
    // For debug GUI
    string nameOfFirstObjectHit = "";
    readonly GUIStyle style = new GUIStyle();
    public LayerMask layerMask;
    public Image reticle;
    public Image reticleOutside;
    public float interactionDistance = defaultInteractionDistance;

    public InteractableObject interactableObjectHovered;
    Camera cam;
    DebugLogger debug;

    private SuperspectiveRaycast debugRaycast;

    private void OnDrawGizmos() {
        if (!DEBUG || debugRaycast == null || !Debug.isDebugBuild) return;

        Color defaultColor = Gizmos.color;
        for (int i = 0; i < debugRaycast.raycastParts.Count; i++) {
            var part = debugRaycast.raycastParts[i];
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, (float)i / RaycastUtils.MAX_RAYCASTS);
            Gizmos.DrawSphere(part.ray.origin, 0.05f);
            Gizmos.DrawSphere(part.ray.origin + part.ray.direction * part.distance, 0.025f);
        }

        Gizmos.color = defaultColor;
    }

    // Use this for initialization
    void Start() {
        debug = new DebugLogger(this, () => DEBUG);
        if (reticle == null) {
            Debug.LogError("Reticle not set in Interact script, disabling script");
            enabled = false;
            return;
        }

        cam = SuperspectiveScreen.instance.playerCamera;
    }

    // Update is called once per frame
    void Update() {
        if (NovaPauseMenu.instance.PauseMenuIsOpen) {
            reticle.enabled = false;
            reticleOutside.enabled = false;
            return;
        }
        else {
            reticle.enabled = true;
            reticleOutside.enabled = true;
        }

        if (Input.GetMouseButtonDown(0)) {
            MaskBufferRenderTextures.instance.RequestVisibilityMask();
        }
        
        InteractableObject newObjectHovered = FindInteractableObjectHovered();

        if (newObjectHovered != null && newObjectHovered.state == InteractableObject.InteractableState.Hidden) {
            newObjectHovered = null;
        }

        // If we were previously hovering over a different object, send a MouseHoverExit event to that object
        if (interactableObjectHovered != null && newObjectHovered != interactableObjectHovered) {
            //Debug.LogWarning(objectHovered.name + ".OnMouseHoverExit()");
            interactableObjectHovered.OnMouseHoverExit?.Invoke();
        }

        HandleMouseHoverNewObject(newObjectHovered);

        if (interactableObjectHovered != null) {
            bool disabled = interactableObjectHovered.state == InteractableObject.InteractableState.Disabled;
            if (disabled) {

                if (Input.GetMouseButtonDown(0)) {
                    // Play disabled sound
                    AudioManager.instance.Play(AudioName.DisabledSound, "DisabledInteraction", true);
                }
                return;
            }
            
            // If the left mouse button is being held down, interact with the object selected
            if (Input.GetMouseButton(0)) {
                // If left mouse button was clicked this frame, call OnLeftMouseButtonDown
                if (Input.GetMouseButtonDown(0)) interactableObjectHovered.OnLeftMouseButtonDown?.Invoke();
                interactableObjectHovered.OnLeftMouseButton?.Invoke();
            }
            // If we released the left mouse button this frame, call OnLeftMouseButtonUp
            else if (Input.GetMouseButtonUp(0)) interactableObjectHovered.OnLeftMouseButtonUp?.Invoke();
        }
    }

    void HandleMouseHoverNewObject(InteractableObject newObjectHovered) {
        // If we are hovering a new object that's not already hovered, send a MouseHover event to that object
        if (newObjectHovered != null && !newObjectHovered.IsSameAs(interactableObjectHovered)) {

            //Debug.LogWarning(newObjectHovered.name + ".OnMouseHover()");
            newObjectHovered.OnMouseHoverEnter?.Invoke();
        }

        // Update which object is now selected
        interactableObjectHovered = newObjectHovered;
        interactableObjectHovered?.OnMouseHover?.Invoke();
    }

    public Vector2 PixelPositionOfReticle() {
        Vector2 reticlePos = Reticle.instance.thisTransformPos;
        Vector2 unclampedPos = Vector2.Scale(
            reticlePos,
            new Vector2(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight)
        );

        return new Vector2(
            Mathf.Clamp(unclampedPos.x, 0, SuperspectiveScreen.currentWidth - 1),
            Mathf.Clamp(unclampedPos.y, 0, SuperspectiveScreen.currentHeight - 1)
        );
    }

    public SuperspectiveRaycast GetRaycastHits(bool disableDebug = true) {
        // Vector2 screenPos = PixelPositionOfReticle();

        Ray ray = AdjustedRay(Reticle.instance.thisTransformPos);
        if (!disableDebug && DEBUG) {
            debugRaycast = RaycastUtils.Raycast(ray.origin, ray.direction, interactionDistance, layerMask);
            return debugRaycast;
        }
        return RaycastUtils.Raycast(ray.origin, ray.direction, interactionDistance, layerMask);
    }

    public SuperspectiveRaycast GetAnyDistanceRaycastHits() {
        Vector2 reticlePos = Reticle.instance.thisTransformPos;
        // Vector2 screenPos = Vector2.Scale(
        //     reticlePos,
        //     new Vector2(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight)
        // );

        Ray ray = AdjustedRay(reticlePos);
        return RaycastUtils.Raycast(ray.origin, ray.direction, float.MaxValue, layerMask);
    }

    Ray AdjustedRay(Vector2 viewportPos) {
        Ray ray = cam.ViewportPointToRay(viewportPos);
        // The following line was causing a bug WRT interacting with objects through portals
        // Not sure what its purpose was but keeping it here in case it needs to be restored
        //ray.origin -= ray.direction.normalized * 0.55f;
        return ray;
    }

    InteractableObject FindInteractableObjectHovered() {
        SuperspectiveRaycast raycastResult = GetRaycastHits(false);

        InteractableObject interactable = null;
        if (raycastResult.hitObject) {
            nameOfFirstObjectHit = raycastResult.firstObjectHit.collider.name;
            GameObject firstObjHit = raycastResult.firstObjectHit.collider.gameObject;
            interactable = firstObjHit.FindInParentsRecursively<InteractableObject>();
        }
        else {
            nameOfFirstObjectHit = "";
        }

        return interactable;
    }

    void OnGUI() {
        if (!DEBUG || !Debug.isDebugBuild) return;
        
        GUI.depth = 2;
        style.normal.textColor = nameOfFirstObjectHit == "" ? Color.red : (interactableObjectHovered == null) ? Color.green : Color.blue;
        GUI.Label(new Rect(5, 80, 200, 25), $"Object Hovered: {nameOfFirstObjectHit}", style);

        String binaryMask = Convert.ToString(MaskBufferRenderTextures.instance.visibilityMaskValue, 2);
        String maskPrintStatement = "";
        for (int i = 0; i < binaryMask.Length; i++) {
            char digit = binaryMask[binaryMask.Length - 1 - i];
            maskPrintStatement += $"\n{i}:\t{digit}";
        }
        style.normal.textColor = Color.blue;
        GUI.Label(
            new Rect(5, 105, 200, 25),
            $"Mask Values: {maskPrintStatement}",
            style
        );
    }
}