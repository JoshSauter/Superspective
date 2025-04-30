using System.Collections.Generic;
using System.Linq;
using PortalMechanics;
using NaughtyAttributes;
using UnityEngine;
using SuperspectiveUtils;
using UnityEngine.Assertions;

// Creates a copy of an object that is partially through a portal on the other side of the portal
public class PortalCopy : MonoBehaviour {
    public float fudgeDistance = 0.001f;
    public bool DEBUG = false;
    DebugLogger debug;
    public PortalableObject originalPortalableObj;

    [ShowNativeProperty]
    private bool CopyEnabled { get; set; } = true;
    [ShowNativeProperty]
    private Portal RelevantPortal => originalPortalableObj == null ? null : originalPortalableObj.IsInPortal ?
        originalPortalableObj.Portal :
        originalPortalableObj.IsHeldThroughPortal ?
            originalPortalableObj.PortalHeldThrough.otherPortal :
            null;

    public Renderer[] renderers;
    public Collider[] colliders;

    void Awake() {
        renderers = transform.GetComponentsInChildrenRecursively<Renderer>();
        
        colliders = transform.GetComponentsInChildrenRecursively<Collider>();
    }

	void Start() {
        debug = new DebugLogger(gameObject, originalPortalableObj.ID, () => DEBUG);

        // copyToOriginalMaterialMap = renderers.SelectMany(r => {
        //     List<Material> originalMaterials = originalPortalableObj.renderers.ToList().Find(r2 => r.name == r2.name).sharedMaterials.ToList();
        //     Assert.IsTrue(originalMaterials.Count == r.sharedMaterials.Length, "Original renderer not found for copy renderer: " + r.name);
        //
        //     return originalMaterials.Select(m => {
        //         Material copyMaterial = r.sharedMaterials.ToList().Find(m2 => m2.name == m.name);
        //         Assert.IsNotNull(copyMaterial, "Copy material not found for original material: " + m.name);
        //         return new { original = m, copy = copyMaterial };
        //     });
        // }).ToDictionary(pair => pair.copy, pair => pair.original);

        // Disallow collisions between a portal object and its copy
        foreach (var c1 in colliders) {
            foreach (var c2 in originalPortalableObj.colliders) {
                SuperspectivePhysics.IgnoreCollision(c1, c2, $"{originalPortalableObj.ID}_PortalCopy");
            }
        }
        
        SetPortalCopyEnabled(false);
    }

    public void SetPortalCopyEnabled(bool enabled) {
        if (CopyEnabled == enabled) return;
        
        foreach (var r in renderers) {
            r.enabled = enabled;
        }
        foreach (var c in colliders) {
            c.enabled = enabled;
        }

        CopyEnabled = enabled;
        if (CopyEnabled) {
            TransformCopy();
        }
    }

    void LateUpdate() {
        if (!originalPortalableObj) {
            Destroy(gameObject);
            return;
        }
        
        UpdateOriginalAndPortalCopyMaterials(CopyEnabled);
        if (CopyEnabled || originalPortalableObj.IsPortaled) {
            TransformCopy();
        }
    }

    void UpdateOriginalAndPortalCopyMaterials(bool portalCopyEnabled) {
        UpdateMaterialsForRenderers(portalCopyEnabled, RelevantPortal?.otherPortal, renderers);
        UpdateMaterialsForRenderers(portalCopyEnabled, RelevantPortal, originalPortalableObj.renderers);
    }

    void UpdateMaterialsForRenderers(bool portalCopyEnabled, Portal portal, Renderer[] renderers) {
        foreach (var r in renderers) {
            foreach (var m in r.materials) {
                if (portalCopyEnabled && portal) {
                    m.EnableKeyword("PORTAL_COPY_OBJECT");
                
                    m.SetVector("_PortalPos", portal.transform.position - portal.transform.forward * 0.00001f);
                    m.SetVector("_PortalNormal", portal.transform.forward);
                    m.SetFloat("_FudgeDistance", fudgeDistance);
                }
                else {
                    m.DisableKeyword("PORTAL_COPY_OBJECT");
                }
            }
        }
    }

    void TransformCopy() {
        debug.Log("Portal: " + RelevantPortal + "\nBefore position: " + transform.position);
        TransformCopy(RelevantPortal);
        debug.Log("After position: " + transform.position);
        UpdateOriginalAndPortalCopyMaterials(true);
    }

    void TransformCopy(Portal inPortal) {
        if (inPortal == null) {
            return;
        }
        
        // Position
        transform.position = inPortal.TransformPoint(originalPortalableObj.transform.position);

        // Rotation
        transform.rotation = inPortal.TransformRotation(originalPortalableObj.transform.rotation);
        
        // Scale
        transform.localScale = originalPortalableObj.transform.localScale * inPortal.ScaleFactor;
    }
}
