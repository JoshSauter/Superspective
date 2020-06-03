using UnityEngine;
using System.Collections.Generic;
using EpitaphUtils;
using EpitaphUtils.ShaderUtils;
using EpitaphUtils.PortalUtils;
using System.Linq;
using NaughtyAttributes;

namespace PortalMechanics {
	public class PortalableObject : MonoBehaviour {
		public Portal sittingInPortal;
		public Portal hoveredThroughPortal;
		public Portal grabbedThroughPortal;

		InteractableObject interact;
		PickupObject pickupObject;

		[HideInInspector]
		public Collider[] colliders;
		Renderer[] renderers;

		public Portal portalInteractingWith {
			get {
				// This long-form code is necessary because C# null propagation does not work properly with Unity Objects
				bool useOtherPortal = false;
				Portal portal = sittingInPortal;
				if (portal == null) {
					portal = hoveredThroughPortal;
					useOtherPortal = true;
				}
				if (portal == null) {
					portal = grabbedThroughPortal;
					useOtherPortal = true;
				}

				return useOtherPortal ? portal.otherPortal : portal;
			}
		}
		[ShowNativeProperty]
		public bool copyShouldBeEnabled { get { return sittingInPortal != null || hoveredThroughPortal != null || grabbedThroughPortal != null; } }
		[ShowNativeProperty]
		public bool copyIsEnabled { get { return fakeCopyInstance != null && fakeCopyInstance.copyEnabled; } }
		Dictionary<Renderer, Material[]> originalMaterials;
		Dictionary<Renderer, Material[]> portalCopyMaterials;

		public PortalCopy fakeCopyPrefab;
		public PortalCopy fakeCopyInstance;

		public delegate void PortalObjectAction(Portal inPortal);
		public PortalObjectAction BeforeObjectTeleported;
		public PortalObjectAction OnObjectTeleported;

		RaycastHits thisFrameRaycastHits;

		private void Awake() {
			interact = GetComponent<InteractableObject>();
			pickupObject = GetComponent<PickupObject>();

			originalMaterials = new Dictionary<Renderer, Material[]>();
			portalCopyMaterials = new Dictionary<Renderer, Material[]>();

			renderers = transform.GetComponentsInChildrenRecursively<Renderer>();
			colliders = transform.GetComponentsInChildrenRecursively<Collider>();
			foreach (var r in renderers) {
				originalMaterials[r] = r.materials;
				portalCopyMaterials[r] = GetPortalCopyMaterials(r.materials);
			}

			pickupObject.OnPickupSimple += RecalculateGrabbedThroughPortal;
			pickupObject.OnDropSimple += HandleDrop;
		}

		private void Start() {
			OnObjectTeleported += UpdateGrabbedThroughPortalAfterObjectTeleports;
			Portal.BeforeAnyPortalTeleport += (Portal inPortal, Collider objBeingTeleported) => UpdateGrabbedThroughPortalAfterPlayerTeleports(inPortal);
		}

		private void Update() {
			thisFrameRaycastHits = Interact.instance.GetRaycastHits();

			RecalculateHoveredThroughPortal();

			if (copyShouldBeEnabled) {
				if (fakeCopyInstance == null) {
					fakeCopyInstance = Instantiate(fakeCopyPrefab);
					fakeCopyInstance.original = gameObject;
					fakeCopyInstance.originalPortalableObj = this;
					fakeCopyInstance.transform.localScale = transform.localScale;

					fakeCopyInstance.OnPortalCopyEnabled += () => SetMaterials(true);
					fakeCopyInstance.OnPortalCopyDisabled += () => SetMaterials(false);
				}
				//fakeCopyInstance.GetComponent<InteractableGlow>().CurrentColor = GetComponent<InteractableGlow>().CurrentColor;
			}

			if (copyShouldBeEnabled) {
				UpdateMaterialProperties(portalInteractingWith);
			}

			if (sittingInPortal != null) {
				foreach (var r in renderers) {
					r.enabled = sittingInPortal.portalIsEnabled;
				}
			}
			else {
				foreach (var r in renderers) {
					r.enabled = true;
				}
			}

			if (!thisFrameRaycastHits.raycastHitAnyPortal && grabbedThroughPortal != null) {
				//pickupObject.Drop();
			}
		}

		void UpdateGrabbedThroughPortalAfterObjectTeleports(Portal inPortal) {
			if (pickupObject.isHeld) {
				// Teleporting the object from the "out" side of the portal to the "in" side of the portal means we are no longer holding the object through a portal
				if (grabbedThroughPortal == inPortal.otherPortal) {
					grabbedThroughPortal = null;
				}
				else {
					grabbedThroughPortal = inPortal;
				}
			}
		}

		void UpdateGrabbedThroughPortalAfterPlayerTeleports(Portal inPortal) {
			if (pickupObject.isHeld) {
				if (grabbedThroughPortal == inPortal) {
					grabbedThroughPortal = null;
				}
				else {
					grabbedThroughPortal = inPortal.otherPortal;
				}
			}
		}

		void RecalculateGrabbedThroughPortal() {
			if (pickupObject.isHeld) {
				grabbedThroughPortal = InteractedThroughPortal();
			}
			else {
				grabbedThroughPortal = null;
			}
		}

		void RecalculateHoveredThroughPortal() {
			bool hoveredOnPickupObj = thisFrameRaycastHits.raycastWasAHit && thisFrameRaycastHits.objectHit == this.gameObject;
			bool hoveredOnPortalCopy = thisFrameRaycastHits.raycastWasAHit && copyIsEnabled && thisFrameRaycastHits.objectHit == fakeCopyInstance.gameObject;
			hoveredThroughPortal = grabbedThroughPortal != null ? grabbedThroughPortal : (hoveredOnPickupObj || hoveredOnPortalCopy) ? InteractedThroughPortal() : null;
		}

		void HandleDrop() {
			grabbedThroughPortal = null;
		}

		Portal InteractedThroughPortal() {
			bool hoveredOnPortalCopy = copyIsEnabled && thisFrameRaycastHits.raycastWasAHit && thisFrameRaycastHits.lastRaycast.hitInfo.collider.gameObject == fakeCopyInstance.gameObject;
			Portal hoveredThroughPortal = thisFrameRaycastHits.firstRaycast.portalHit;
			if (sittingInPortal == hoveredThroughPortal) {
				hoveredThroughPortal = null;
			}
			else if (hoveredOnPortalCopy) {
				hoveredThroughPortal = sittingInPortal?.otherPortal;
			}

			return hoveredThroughPortal;
		}

		void UpdateMaterialProperties(Portal inPortal) {
			foreach (var r in renderers) {
				foreach (var m in r.materials) {
					m.SetVector("_PortalPos", inPortal.transform.position - inPortal.transform.forward * 0.00001f);
					m.SetVector("_PortalNormal", inPortal.transform.forward);
				}
			}
		}

		void SetMaterials(bool usePortalCopyMaterials) {
			for (int i = 0; i < renderers.Length; i++) {
				Renderer r = renderers[i];

				r.materials = usePortalCopyMaterials ? portalCopyMaterials[r] : originalMaterials[r];
				if (usePortalCopyMaterials) {

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
}
