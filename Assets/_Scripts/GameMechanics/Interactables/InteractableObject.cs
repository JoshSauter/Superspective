using System;
using System.ComponentModel.Design.Serialization;
using SuperspectiveUtils;
using NaughtyAttributes;
using PortalMechanics;
using Saving;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class InteractableObject : SuperspectiveObject<InteractableObject, InteractableObject.InteractableObjectSave> {

    public InteractableGlow glow;
    public delegate void InteractAction();

    public enum InteractableState : byte {
        Interactable,
        Disabled,
        Hidden
    }

    [SerializeField]
    private InteractableState _state;
    public InteractableState state {
        get => _state;
        private set => _state = value;
    }

    public string enabledHelpText = "";
    public string disabledHelpText = "";
    
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
    
    // Used to determine whether two interactable objects should be treated as the same (a portal copy for instance)
    PortalableObject portalableObject;
    PortalCopy portalCopy;

    protected override void Start() {
        base.Start();
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

        portalableObject = transform.GetComponentInChildren<PortalableObject>();
        portalCopy = transform.GetComponentInChildren<PortalCopy>();
    }

    public void SetAsInteractable(string helpMsg = "") {
        debug.Log($"Set as interactable with help message: {helpMsg}");
        if (helpMsg != "") {
            enabledHelpText = helpMsg;
        }

        if (state == InteractableState.Interactable) return;
        
        state = InteractableState.Interactable;
    }

    public void SetAsDisabled(string helpMsg = "") {
        debug.Log($"Set as disabled with help message: {helpMsg}");
        if (helpMsg != "") {
            disabledHelpText = helpMsg;
        }
        if (state == InteractableState.Disabled) return;
        
        state = InteractableState.Disabled;
    }

    public void SetAsHidden() {
        if (state == InteractableState.Hidden) return;
        
        state = InteractableState.Hidden;
    }

    public bool IsSameAs(InteractableObject other) {
        if (other == null) return false;

        if (portalableObject != null && other.portalCopy != null) {
            return portalableObject == other.portalCopy.originalPortalableObj;
        }
        else if (portalCopy != null && other.portalableObject != null) {
            return portalCopy.originalPortalableObj == other.portalableObject;
        }

        return this == other;
    }

    public override void LoadSave(InteractableObjectSave save) {
        state = save.state;
    }

    [Serializable]
    public class InteractableObjectSave : SaveObject<InteractableObject> {
        public InteractableState state;
        
        public InteractableObjectSave(InteractableObject script) : base(script) {
            this.state = script.state;
        }
    }
}
