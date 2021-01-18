using PortalMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

// Creates a copy of an object that is partially through a portal on the other side of the portal
public class PortalCopy : MonoBehaviour {
    public bool DEBUG = false;
    DebugLogger debug;
    public GameObject original;
    public PortalableObject originalPortalableObj;
    InteractableGlow maybeOriginalGlow;
    InteractableGlow glow;

    bool _copyEnabled = false;
    public bool copyEnabled {
        get { return _copyEnabled; }
        set {
            if (_copyEnabled && !value) {
                OnPortalCopyDisabled?.Invoke();
            }
            if (!_copyEnabled && value) {
                OnPortalCopyEnabled?.Invoke();
            }
            _copyEnabled = value;
        }
    }

    public Renderer[] renderers;
    Collider[] colliders;

    public delegate void PortalCopyAction();
    public PortalCopyAction OnPortalCopyEnabled;
    public PortalCopyAction OnPortalCopyDisabled;

    void Awake() {
        renderers = transform.GetComponentsInChildrenRecursively<Renderer>();
        colliders = transform.GetComponentsInChildrenRecursively<Collider>();
    }

	IEnumerator Start() {
        debug = new DebugLogger(gameObject, () => DEBUG);

        originalPortalableObj = original.GetComponent<PortalableObject>();

        // Disallow collisions between a portal object and its copy
        foreach (var c1 in colliders) {
            foreach (var c2 in originalPortalableObj.colliders) {
                Physics.IgnoreCollision(c1, c2);
            }
        }

        InteractableObject maybeInteract = original.GetComponent<InteractableObject>();
        maybeOriginalGlow = original.GetComponent<InteractableGlow>();

        if (maybeInteract != null) {
            InteractableObject interact = gameObject.AddComponent<InteractableObject>();
            interact.glowColor = maybeInteract.glowColor;
            interact.overrideGlowColor = maybeInteract.overrideGlowColor;

            interact.OnLeftMouseButton += maybeInteract.OnLeftMouseButton;
            interact.OnLeftMouseButtonDown += maybeInteract.OnLeftMouseButtonDown;
            interact.OnLeftMouseButtonUp += maybeInteract.OnLeftMouseButtonUp;

            interact.OnMouseHover += maybeInteract.OnMouseHover;
            interact.OnMouseHoverEnter += maybeInteract.OnMouseHoverEnter;
            interact.OnMouseHoverExit += maybeInteract.OnMouseHoverExit;
        }

        TransformCopy();

        yield return null;

        glow = GetComponent<InteractableGlow>();
    }

    public void SetPortalCopyEnabled(bool enabled) {
        foreach (var r in renderers) {
            r.enabled = enabled;
        }
        foreach (var c in colliders) {
            c.enabled = enabled;
        }

        copyEnabled = enabled;
        if (copyEnabled) {
            TransformCopy();
        }
    }

    void LateUpdate() {
        if (originalPortalableObj.copyShouldBeEnabled != copyEnabled) {
            SetPortalCopyEnabled(originalPortalableObj.copyShouldBeEnabled);
        }

        if (copyEnabled) {
            TransformCopy();
            if (maybeOriginalGlow != null && glow != null) {
                glow.enabled = maybeOriginalGlow.enabled;
                glow.glowAmount = maybeOriginalGlow.glowAmount;
            }
        }
    }

	void UpdateMaterials() {
        Portal portal = originalPortalableObj.portalInteractingWith.otherPortal;

        foreach (var r in renderers) {
            foreach (var m in r.materials) {
                m.SetVector("_PortalPos", portal.transform.position - portal.transform.forward * 0.00001f);
                m.SetVector("_PortalNormal", portal.transform.forward);
            }
        }
    }

    public void TransformCopy() {
        Portal relevantPortal = originalPortalableObj.portalInteractingWith;
        debug.Log("Portal: " + relevantPortal + "\nBefore position: " + transform.position);
        TransformCopy(relevantPortal);
        debug.Log("After position: " + transform.position);
        UpdateMaterials();
    }

    void TransformCopy(Portal inPortal) {
        // Position
        transform.position = inPortal.TransformPoint(original.transform.position);

        // Rotation
        transform.rotation = inPortal.TransformRotation(original.transform.rotation);
    }
}
