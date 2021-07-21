using UnityEngine;
using System.Collections.Generic;
using SuperspectiveUtils;
using SuperspectiveUtils.ShaderUtils;
using System.Linq;
using NaughtyAttributes;
using Saving;
using System;
using SerializableClasses;

namespace PortalMechanics {
	[RequireComponent(typeof(UniqueId))]
	public class PortalableObject : SaveableObject<PortalableObject, PortalableObject.PortalableObjectSave> {
		Portal _sittingInPortal;
		Portal _hoveredThroughPortal;
		Portal _grabbedThroughPortal;
		public Portal sittingInPortal {
			get {
				return _sittingInPortal;
			}
			set {
				_sittingInPortal = value;
				if (copyShouldBeEnabled && portalInteractingWith != null) {
					if (fakeCopyInstance == null) {
						CreateFakeCopyInstance();
					}
					SetMaterials(true);
					UpdateMaterialProperties(portalInteractingWith);
				}
			}
		}
		public Portal hoveredThroughPortal {
			get {
				return _hoveredThroughPortal;
			}
			set {
				_hoveredThroughPortal = value;
				if (copyShouldBeEnabled && portalInteractingWith != null) {
					if (fakeCopyInstance == null) {
						CreateFakeCopyInstance();
					}
					SetMaterials(true);
					UpdateMaterialProperties(portalInteractingWith);
				}
			}
		}
		public Portal grabbedThroughPortal {
			get {
				return _grabbedThroughPortal;
			}
			private set {
				_grabbedThroughPortal = value;
				if (copyShouldBeEnabled && portalInteractingWith != null) {
					if (fakeCopyInstance == null) {
						CreateFakeCopyInstance();
					}
					SetMaterials(true);
					UpdateMaterialProperties(portalInteractingWith);
				}
			}
		}

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

				if (portal == null) {
					return null;
				}
				
				return useOtherPortal ? portal.otherPortal : portal;
			}
		}
		[ShowNativeProperty]
		public bool copyShouldBeEnabled => sittingInPortal != null || hoveredThroughPortal != null || grabbedThroughPortal != null;
		[ShowNativeProperty]
		public bool copyIsEnabled => fakeCopyInstance != null && fakeCopyInstance.copyEnabled;
		Dictionary<Renderer, Material[]> originalMaterials;
		Dictionary<Renderer, Material[]> portalCopyMaterials;

		public PortalCopy fakeCopyPrefab;
		public PortalCopy fakeCopyInstance;

		public delegate void PortalObjectAction(Portal inPortal);
		public PortalObjectAction BeforeObjectTeleported;
		public PortalObjectAction OnObjectTeleported;

		SuperspectiveRaycast thisFrameRaycastHits;

		protected override void Awake() {
			base.Awake();
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

		protected override void Start() {
			base.Start();
			OnObjectTeleported += UpdateGrabbedThroughPortalAfterObjectTeleports;
			Portal.BeforeAnyPortalTeleport += (Portal inPortal, Collider objBeingTeleported) => UpdateGrabbedThroughPortalAfterPlayerTeleports(inPortal);
		}

		void Update() {
			thisFrameRaycastHits = Interact.instance.GetRaycastHits();

			RecalculateHoveredThroughPortal();

			if (copyShouldBeEnabled && fakeCopyInstance == null) {
				CreateFakeCopyInstance();
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

			if (!thisFrameRaycastHits.hitPortal && grabbedThroughPortal != null) {
				//pickupObject.Drop();
			}
		}

		void CreateFakeCopyInstance() {
			if (copyShouldBeEnabled && fakeCopyInstance == null) {
				fakeCopyInstance = Instantiate(fakeCopyPrefab);
				fakeCopyInstance.original = gameObject;
				fakeCopyInstance.originalPortalableObj = this;
				fakeCopyInstance.transform.localScale = transform.localScale;

				fakeCopyInstance.OnPortalCopyEnabled += () => SetMaterials(true);
				fakeCopyInstance.OnPortalCopyDisabled += () => SetMaterials(false);
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
			bool hoveredOnPickupObj = thisFrameRaycastHits.hitObject && thisFrameRaycastHits.firstObjectHit.collider.gameObject == this.gameObject;
			bool hoveredOnPortalCopy = thisFrameRaycastHits.hitObject && copyIsEnabled && thisFrameRaycastHits.firstObjectHit.collider.gameObject == fakeCopyInstance.gameObject;
			hoveredThroughPortal = grabbedThroughPortal != null ? grabbedThroughPortal : (hoveredOnPickupObj || hoveredOnPortalCopy) ? InteractedThroughPortal() : null;
		}

		void HandleDrop() {
			grabbedThroughPortal = null;
		}

		Portal InteractedThroughPortal() {
			bool hoveredOnPortalCopy = copyIsEnabled && thisFrameRaycastHits.hitObject && thisFrameRaycastHits.firstObjectHit.collider.gameObject == fakeCopyInstance.gameObject;
			Portal hoveredThroughPortal = thisFrameRaycastHits.firstPortalHit;
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
					m.SetVector("_PortalPos", inPortal.transform.position + inPortal.transform.forward * 0.00001f);
					m.SetVector("_PortalNormal", inPortal.transform.forward);
				}
			}
		}

		void SetMaterials(bool usePortalCopyMaterials) {
			SetMaterialsForRenderers(renderers, usePortalCopyMaterials);
			if (usePortalCopyMaterials) {
				UpdateMaterialProperties(portalInteractingWith);
				SetMaterialsForRenderers(fakeCopyInstance.renderers, usePortalCopyMaterials);
			}
		}

		void SetMaterialsForRenderers(Renderer[] renderersToChangeMaterialsOf, bool usePortalCopyMaterials) {
			for (int i = 0; i < renderers.Length; i++) {
				Renderer rendererToUseAsKey = renderers[i];
				Renderer rendererToModify = renderersToChangeMaterialsOf[i];

				rendererToModify.materials = usePortalCopyMaterials ? portalCopyMaterials[rendererToUseAsKey] : originalMaterials[rendererToUseAsKey];
				if (usePortalCopyMaterials) {

					for (int j = 0; j < rendererToModify.materials.Length; j++) {
						rendererToModify.materials[j].CopyMatchingPropertiesFromMaterial(originalMaterials[rendererToUseAsKey][j]);
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
					debug.LogWarning("No matching portalCopyShader for shader " + material.shader.name);
					return null;
			}
		}


		#region Saving

		[Serializable]
		public class PortalableObjectSave : SerializableSaveObject<PortalableObject> {
			SerializableReference<Portal, Portal.PortalSave> sittingInPortal;
			SerializableReference<Portal, Portal.PortalSave> hoveredThroughPortal;
			SerializableReference<Portal, Portal.PortalSave> grabbedThroughPortal;

			public PortalableObjectSave(PortalableObject obj) : base(obj) {
				sittingInPortal = null;
				hoveredThroughPortal = null;
				grabbedThroughPortal = null;
				if (obj.sittingInPortal != null) {
					this.sittingInPortal = obj.sittingInPortal;
				}
				if (obj.hoveredThroughPortal != null) {
					this.hoveredThroughPortal = obj.hoveredThroughPortal;
				}
				if (obj.grabbedThroughPortal != null) {
					this.grabbedThroughPortal = obj.grabbedThroughPortal;
				}
			}

			public override void LoadSave(PortalableObject obj) {
				// GetOrNull valid here because if this saved value is set, it should have to be loaded
				if (this.sittingInPortal != null) {
					obj.sittingInPortal = this.sittingInPortal.GetOrNull();
				}
				if (this.hoveredThroughPortal != null) {
					obj.hoveredThroughPortal = this.hoveredThroughPortal.GetOrNull();
				}
				if (this.grabbedThroughPortal != null) {
					obj.grabbedThroughPortal = this.grabbedThroughPortal.GetOrNull();
				}
			}
		}
		#endregion
	}
}
