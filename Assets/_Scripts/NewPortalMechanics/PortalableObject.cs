using UnityEngine;
using System.Collections.Generic;
using EpitaphUtils;
using EpitaphUtils.ShaderUtils;
using System.Linq;

public class PortalableObject : MonoBehaviour {
	public Portal sittingInPortal;

	Renderer[] renderers;
	Renderer[] portalCopyObjRenderers;
	public bool copyIsEnabled { get { return fakeCopyInstance != null && fakeCopyInstance.activeSelf; } }
	Dictionary<Renderer, Material[]> originalMaterials;
	Dictionary<Renderer, Material[]> portalCopyMaterials;

	public GameObject fakeCopyPrefab;
	public GameObject fakeCopyInstance;

	public delegate void PortalObjectAction(Portal inPortal);
	public PortalObjectAction OnObjectTeleported;
	public PortalObjectAction BeforeObjectTeleported;

	private void Awake() {
		originalMaterials = new Dictionary<Renderer, Material[]>();
		portalCopyMaterials = new Dictionary<Renderer, Material[]>();

		renderers = transform.GetComponentsInChildrenRecursively<Renderer>();
		foreach (var r in renderers) {
			originalMaterials[r] = r.materials;
			portalCopyMaterials[r] = GetPortalCopyMaterials(r.materials);
		}
	}

	public void EnableAndUpdatePortalCopy(Portal inPortal) {
		bool copyWasDisabled = !copyIsEnabled;

		if (fakeCopyInstance == null) {
			fakeCopyInstance = Instantiate(fakeCopyPrefab);
			portalCopyObjRenderers = fakeCopyInstance.transform.GetComponentsInChildrenRecursively<Renderer>();

			InteractableObject maybeInteractableObj = GetComponent<InteractableObject>();
			if (maybeInteractableObj != null) {
				InteractableObject fakeCopyInteractableObj = fakeCopyInstance.GetComponent<InteractableObject>();
				if (fakeCopyInteractableObj == null) fakeCopyInteractableObj = fakeCopyInstance.AddComponent<InteractableObject>();

				fakeCopyInteractableObj.OnLeftMouseButton += maybeInteractableObj.OnLeftMouseButton;
				fakeCopyInteractableObj.OnLeftMouseButtonDown += maybeInteractableObj.OnLeftMouseButtonDown;
				fakeCopyInteractableObj.OnLeftMouseButtonUp += maybeInteractableObj.OnLeftMouseButtonUp;
				fakeCopyInteractableObj.OnMouseHover += maybeInteractableObj.OnMouseHover;
				fakeCopyInteractableObj.OnMouseHoverEnter += maybeInteractableObj.OnMouseHoverEnter;
				fakeCopyInteractableObj.OnMouseHoverExit += maybeInteractableObj.OnMouseHoverExit;
			}

		}

		if (copyWasDisabled) {
			SetMaterials(true);
		}

		UpdatePortalCopyMaterialShaderValues(inPortal);

		TransformCopy(inPortal);
		fakeCopyInstance.SetActive(true);
	}

	public void DisablePortalCopy() {
		if (copyIsEnabled) {
			SetMaterials(false);
		}
		fakeCopyInstance?.SetActive(false);
	}

	void UpdatePortalCopyMaterialShaderValues(Portal inPortal) {
		foreach (var r in renderers) {
			foreach (var m in r.materials) {
				m.SetVector("_PortalPos", inPortal.transform.position - inPortal.transform.forward * 0.00001f);
				m.SetVector("_PortalNormal", inPortal.transform.forward);
			}
		}

		foreach (var r in portalCopyObjRenderers) {
			foreach (var m in r.materials) {
				m.SetVector("_PortalPos", inPortal.otherPortal.transform.position - inPortal.otherPortal.transform.forward * 0.00001f);
				m.SetVector("_PortalNormal", inPortal.otherPortal.transform.forward);
			}
		}
	}

	void SetMaterials(bool usePortalCopyMaterials) {
		for (int i = 0; i < renderers.Length; i++) {
			Renderer r = renderers[i];

			r.materials = usePortalCopyMaterials ? portalCopyMaterials[r] : originalMaterials[r];
			if (usePortalCopyMaterials) {
				Renderer portalCopyObjRenderer = portalCopyObjRenderers[i];
				portalCopyObjRenderer.materials = r.materials;

				for (int j = 0; j < r.materials.Length; j++) {
					r.materials[j].CopyMatchingPropertiesFromMaterial(originalMaterials[r][j]);
				}
			}
		}
	}

	Material[] GetPortalCopyMaterials(Material[] materials) {
		return materials.Select(m => GetPortalCopyMaterial(m)).ToArray();
	}

	Material GetPortalCopyMaterial(Material material) {
		switch (material.shader.name) {
			case "Custom/Unlit":
				return new Material(Shader.Find("Custom/UnlitPortalCopy"));
			case "Custom/UnlitTransparent":
				return new Material(Shader.Find("Custom/UnlitTransparentPortalCopy"));
			default:
				Debug.LogWarning("No matching portalCopyShader for shader " + material.shader.name);
				return null;
		}
	}

	private void TransformCopy(Portal inPortal) {
		Transform obj = fakeCopyInstance.transform;
		// Position
		Vector3 relativeObjPos = inPortal.transform.InverseTransformPoint(transform.position);
		relativeObjPos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeObjPos;
		obj.position = inPortal.otherPortal.transform.TransformPoint(relativeObjPos);

		// Rotation
		Quaternion relativeRot = Quaternion.Inverse(inPortal.transform.rotation) * transform.rotation;
		relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
		obj.rotation = inPortal.otherPortal.transform.rotation * relativeRot;
	}
}
