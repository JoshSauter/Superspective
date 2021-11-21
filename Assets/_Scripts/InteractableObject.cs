using SuperspectiveUtils;
using NaughtyAttributes;
using UnityEngine;

public class InteractableObject : MonoBehaviour {
    public InteractableGlow glow;
    public delegate void InteractAction();

    public bool interactable = true;
    public bool useLargerPrepassMaterial;
    public bool overrideGlowColor;

    [ShowIf("overrideGlowColor")]
    public Color glowColor = new Color(.6f, .35f, .25f, 1f);

    public GameObject thisRendererParent;
    public bool recursiveChildRenderers = true;
    public InteractAction OnLeftMouseButton;
    public InteractAction OnLeftMouseButtonDown;
    public InteractAction OnLeftMouseButtonUp;
    public InteractAction OnMouseHover;
    public InteractAction OnMouseHoverEnter;
    public InteractAction OnMouseHoverExit;

    public void Start() {
        if (thisRendererParent == null) {
            Renderer[] childRenderers = transform.GetComponentsInChildrenRecursively<Renderer>();
            if (childRenderers.Length > 0) thisRendererParent = childRenderers[0].gameObject;
        }

        if (thisRendererParent != null) {
            glow = thisRendererParent.gameObject.GetOrAddComponent<InteractableGlow>();
            glow.recursiveChildRenderers = recursiveChildRenderers;
            glow.useLargerPrepassMaterial = useLargerPrepassMaterial;
            glow.overrideGlowColor = overrideGlowColor;
            glow.glowColor = glowColor;
            glow.interactableObject = this;
        }
    }
}