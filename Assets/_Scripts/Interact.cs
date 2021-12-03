using System;
using System.Collections;
using System.Linq;
using Audio;
using SuperspectiveUtils;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

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
    readonly Color reticleOutsideSelectColor = new Color(0.1f, 0.75f, 0.075f, 0.75f);
    Color reticleOutsideUnselectColor;
    readonly Color reticleSelectColor = new Color(0.15f, 1, 0.15f, 0.9f);
    private readonly Color reticleSelectDisabledColor = new Color(.8f, 0.15f, 0.15f, .9f);

    Color reticleUnselectColor;

    // Use this for initialization
    void Start() {
        debug = new DebugLogger(this, () => DEBUG);
        if (reticle == null) {
            Debug.LogError("Reticle not set in Interact script, disabling script");
            enabled = false;
            return;
        }

        cam = SuperspectiveScreen.instance.playerCamera;
        reticleUnselectColor = reticle.color;
        reticleOutsideUnselectColor = reticleOutside.color;
    }

    // Update is called once per frame
    void Update() {
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

        // If we are hovering a new object that's not already hovered, send a MouseHover event to that object
        if (newObjectHovered != null && newObjectHovered != interactableObjectHovered) {
            //Debug.LogWarning(newObjectHovered.name + ".OnMouseHover()");
            newObjectHovered.OnMouseHoverEnter?.Invoke();
        }

        // Update which object is now selected
        interactableObjectHovered = newObjectHovered;
        interactableObjectHovered?.OnMouseHover?.Invoke();

        if (interactableObjectHovered != null) {
            bool disabled = interactableObjectHovered.state == InteractableObject.InteractableState.Disabled;
            if (disabled) {
                reticle.color = reticleSelectDisabledColor;
                reticleOutside.color = reticleSelectDisabledColor;

                if (Input.GetMouseButtonDown(0)) {
                    // Play disabled sound
                    AudioManager.instance.Play(AudioName.DisabledSound, "DisabledInteraction");
                }
                return;
            }
            
            reticle.color = reticleSelectColor;
            reticleOutside.color = reticleOutsideSelectColor;
            // If the left mouse button is being held down, interact with the object selected
            if (Input.GetMouseButton(0)) {
                // If left mouse button was clicked this frame, call OnLeftMouseButtonDown
                if (Input.GetMouseButtonDown(0)) interactableObjectHovered.OnLeftMouseButtonDown?.Invoke();
                interactableObjectHovered.OnLeftMouseButton?.Invoke();
            }
            // If we released the left mouse button this frame, call OnLeftMouseButtonUp
            else if (Input.GetMouseButtonUp(0)) interactableObjectHovered.OnLeftMouseButtonUp?.Invoke();
        }
        else {
            reticle.color = reticleUnselectColor;
            reticleOutside.color = reticleOutsideUnselectColor;
        }
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

    public SuperspectiveRaycast GetRaycastHits() {
        Vector2 screenPos = PixelPositionOfReticle();

        Ray ray = cam.ScreenPointToRay(screenPos);
        return RaycastUtils.Raycast(ray.origin, ray.direction, interactionDistance, layerMask);
    }

    public SuperspectiveRaycast GetAnyDistanceRaycastHits() {
        Vector2 reticlePos = Reticle.instance.thisTransformPos;
        Vector2 screenPos = Vector2.Scale(
            reticlePos,
            new Vector2(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight)
        );

        Ray ray = cam.ScreenPointToRay(screenPos);
        return RaycastUtils.Raycast(ray.origin, ray.direction, float.MaxValue, layerMask);
    }

    InteractableObject FindInteractableObjectHovered() {
        SuperspectiveRaycast raycastResult = GetRaycastHits();

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
        if (DEBUG) {
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
}