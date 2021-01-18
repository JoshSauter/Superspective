using EpitaphUtils;
using EpitaphUtils.PortalUtils;
using UnityEngine;
using UnityEngine.UI;

public class Interact : Singleton<Interact> {
    public const float defaultInteractionDistance = 6.5f;
    public bool DEBUG;
    public LayerMask layerMask;
    public Image reticle;
    public Image reticleOutside;
    public float interactionDistance = defaultInteractionDistance;

    public InteractableObject objectHovered;
    Camera cam;
    DebugLogger debug;
    readonly Color reticleOutsideSelectColor = new Color(0.1f, 0.75f, 0.075f, 0.75f);
    Color reticleOutsideUnselectColor;
    readonly Color reticleSelectColor = new Color(0.15f, 1, 0.15f, 0.9f);

    Color reticleUnselectColor;
    //int layerMask;

    // Use this for initialization
    void Start() {
        debug = new DebugLogger(this, () => DEBUG);
        if (reticle == null) {
            Debug.LogError("Reticle not set in Interact script, disabling script");
            enabled = false;
            return;
        }

        cam = EpitaphScreen.instance.playerCamera;
        reticleUnselectColor = reticle.color;
        reticleOutsideUnselectColor = reticleOutside.color;
    }

    // Update is called once per frame
    void Update() {
        InteractableObject newObjectHovered = FindInteractableObjectHovered();
        if (newObjectHovered != null && !newObjectHovered.interactable) newObjectHovered = null;

        // If we were previously hovering over a different object, send a MouseHoverExit event to that object
        if (objectHovered != null && newObjectHovered != objectHovered) {
            //Debug.LogWarning(objectHovered.name + ".OnMouseHoverExit()");
            objectHovered.OnMouseHoverExit?.Invoke();
        }

        // If we are hovering a new object that's not already hovered, send a MouseHover event to that object
        if (newObjectHovered != null && newObjectHovered != objectHovered) {
            //Debug.LogWarning(newObjectHovered.name + ".OnMouseHover()");
            newObjectHovered.OnMouseHoverEnter?.Invoke();
        }

        // Update which object is now selected
        objectHovered = newObjectHovered;
        objectHovered?.OnMouseHover?.Invoke();

        if (objectHovered != null) {
            reticle.color = reticleSelectColor;
            reticleOutside.color = reticleOutsideSelectColor;
            // If the left mouse button is being held down, interact with the object selected
            if (Input.GetMouseButton(0)) {
                // If left mouse button was clicked this frame, call OnLeftMouseButtonDown
                if (Input.GetMouseButtonDown(0)) objectHovered.OnLeftMouseButtonDown?.Invoke();
                objectHovered.OnLeftMouseButton?.Invoke();
            }
            // If we released the left mouse button this frame, call OnLeftMouseButtonUp
            else if (Input.GetMouseButtonUp(0)) objectHovered.OnLeftMouseButtonUp?.Invoke();
        }
        else {
            reticle.color = reticleUnselectColor;
            reticleOutside.color = reticleOutsideUnselectColor;
        }
    }

    public RaycastHits GetRaycastHits() {
        Vector2 reticlePos = Reticle.instance.thisTransformPos;
        Vector2 screenPos = Vector2.Scale(
            reticlePos,
            new Vector2(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight)
        );

        Ray ray = cam.ScreenPointToRay(screenPos);
        return PortalUtils.RaycastThroughPortals(ray.origin, ray.direction, interactionDistance, layerMask);
    }

    public RaycastHits GetAnyDistanceRaycastHits() {
        Vector2 reticlePos = Reticle.instance.thisTransformPos;
        Vector2 screenPos = Vector2.Scale(
            reticlePos,
            new Vector2(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight)
        );

        Ray ray = cam.ScreenPointToRay(screenPos);
        return PortalUtils.RaycastThroughPortals(ray.origin, ray.direction, float.MaxValue, layerMask);
    }

    InteractableObject FindInteractableObjectHovered() {
        RaycastHits hitObject = GetRaycastHits();

        if (hitObject.raycastWasAHit && hitObject.lastRaycast.hitInfo.collider != null) {
            debug.Log("Hovering over " + hitObject.lastRaycast.hitInfo.collider.gameObject.name);
            return hitObject.lastRaycast.hitInfo.collider.GetComponent<InteractableObject>();
        }

        return null;
    }
}