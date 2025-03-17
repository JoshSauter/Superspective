using System;
using Audio;
using NovaMenuUI;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Only handles the system governing interactable objects, all the juice for this is in InteractJuice.cs
public class Interact : Singleton<Interact> {
    public const float defaultInteractionDistance = 6.5f;
    public bool DEBUG;
    // For debug GUI
    private string nameOfFirstObjectHit = "";
    private string nameOfPortalHit = "";
    
    readonly GUIStyle style = new GUIStyle();
    [NonSerialized]
    private LayerMask layerMask;
    public Image reticle;
    public Image reticleOutside;
    public float interactionDistance = defaultInteractionDistance;
    public float effectiveInteractionDistance => Player.instance.Scale * interactionDistance;

    public InteractableObject interactableObjectHovered;
    Camera cam;
    DebugLogger debug;

    // The raycast last used for interaction
    public SuperspectiveRaycast raycast;

    private void OnDrawGizmos() {
        if (!DEBUG || raycast == null || !Debug.isDebugBuild) return;

        Color defaultColor = Gizmos.color;
        for (int i = 0; i < raycast.raycastParts.Count; i++) {
            var part = raycast.raycastParts[i];
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, (float)i / RaycastUtils.MAX_RAYCASTS);
            Gizmos.DrawSphere(part.ray.origin, 0.05f);
            Gizmos.DrawSphere(part.ray.origin + part.ray.direction * part.distance, 0.025f);
        }

        Gizmos.color = defaultColor;
    }

    // Use this for initialization
    void Start() {
        layerMask = SuperspectivePhysics.PhysicsRaycastLayerMask;
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

        if (PlayerButtonInput.instance.InteractPressed) {
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

                if (PlayerButtonInput.instance.InteractPressed) {
                    // Play disabled sound
                    AudioManager.instance.Play(AudioName.DisabledSound, "DisabledInteraction", true);
                }
                return;
            }
            
            // If the left mouse button is being held down, interact with the object selected
            if (PlayerButtonInput.instance.InteractHeld) {
                // If left mouse button was clicked this frame, call OnLeftMouseButtonDown
                if (PlayerButtonInput.instance.InteractPressed) interactableObjectHovered.OnLeftMouseButtonDown?.Invoke();
                interactableObjectHovered.OnLeftMouseButton?.Invoke();
            }
            // If we released the left mouse button this frame, call OnLeftMouseButtonUp
            else if (PlayerButtonInput.instance.InteractReleased) interactableObjectHovered.OnLeftMouseButtonUp?.Invoke();
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

    public SuperspectiveRaycast GetRaycastHits() {
        Ray ray = AdjustedRay(Reticle.instance.thisTransformPos);
        return RaycastUtils.Raycast(ray.origin, ray.direction, effectiveInteractionDistance, layerMask, true);
    }

    public SuperspectiveRaycast GetAnyDistanceRaycastHits() {
        Vector2 reticlePos = Reticle.instance.thisTransformPos;
        // Vector2 screenPos = Vector2.Scale(
        //     reticlePos,
        //     new Vector2(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight)
        // );

        Ray ray = AdjustedRay(reticlePos);
        return RaycastUtils.Raycast(ray.origin, ray.direction, float.MaxValue, layerMask, true);
    }

    Ray AdjustedRay(Vector2 viewportPos) {
        Ray ray = cam.ViewportPointToRay(viewportPos);
        // The following line was causing a bug WRT interacting with objects through portals
        // Not sure what its purpose was but keeping it here in case it needs to be restored
        //ray.origin -= ray.direction.normalized * 0.55f;
        return ray;
    }

    InteractableObject FindInteractableObjectHovered() {
        SuperspectiveRaycast raycastResult = GetRaycastHits();
        raycast = raycastResult;

        InteractableObject interactable = null;
        if (raycastResult.DidHitObject) {
            nameOfFirstObjectHit = raycastResult.FirstObjectHit.collider.name;
            GameObject firstObjHit = raycastResult.FirstObjectHit.collider.gameObject;
            #if UNITY_EDITOR
            // In the editor, holding control and clicking on an object in the game world will select and focus it in the scene view
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0)) {
                Selection.objects = new UnityEngine.Object[] { firstObjHit };
                Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView != null) {
                    gameView.maximized = false;
                }
                SceneView.lastActiveSceneView.FrameSelected();
                SceneViewFX.instance.enabled = true;
                SceneViewFX.instance.cachedEnableState = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            } 
            #endif
            interactable = firstObjHit.FindInParentsRecursively<InteractableObject>();
            if (interactable == null) {
                interactable = firstObjHit.FindInParentsRecursively<PortalCopy>()?.originalPortalableObj.InteractObject;
            }
        }
        else {
            nameOfFirstObjectHit = "";
        }

        nameOfPortalHit = raycastResult.DidHitPortal ? raycastResult.FirstValidPortalHit.name : "";

        return interactable;
    }

    void OnGUI() {
        if (!DEBUG || !DebugInput.IsDebugBuild) return;
        
        GUI.depth = 2;
        Color textColor;
        string text;
        switch (nameOfFirstObjectHit != "", nameOfPortalHit != "") {
            // Didn't hit an object nor a portal
            case (false, false):
                textColor = Color.red;
                text = "Object Hovered: None";
                break;
            // Hit a portal but not an object
            case (false, true):
                textColor = Color.blue;
                text = "Object Hovered: None, Portal Hit: " + nameOfPortalHit;
                break;
            // Hit an object but not a portal
            case (true, false):
                textColor = Color.green;
                text = "Object Hovered: " + nameOfFirstObjectHit;
                break;
            // Hit both an object and a portal
            default:
                textColor = Color.cyan;
                text = "Object Hovered: " + nameOfFirstObjectHit + ", through Portal: " + nameOfPortalHit;
                break;
        }

        style.normal.textColor = textColor;
        
        GUI.Label(new Rect(5, 80, 200, 25), text, style);

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