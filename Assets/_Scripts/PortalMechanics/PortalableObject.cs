using UnityEngine;
using System.Collections.Generic;
using SuperspectiveUtils;
using SuperspectiveUtils.ShaderUtils;
using System.Linq;
using NaughtyAttributes;
using Saving;
using System;
using SerializableClasses;
using StateUtils;
using UnityEngine.Rendering;

namespace PortalMechanics {
	[RequireComponent(typeof(UniqueId))]
	public class PortalableObject : SaveableObject<PortalableObject, PortalableObject.PortalableObjectSave> {
		Portal _sittingInPortal;
		Portal _hoveredThroughPortal;
		Portal _grabbedThroughPortal;
		private bool resetSittingInPortal = false;
		public Portal sittingInPortal {
			get => _sittingInPortal;
			set {
				_sittingInPortal = value;
				if (value != null) {
					resetSittingInPortal = false;
				}
				
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
			get => _hoveredThroughPortal;
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
			get => _grabbedThroughPortal;
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

		// This state machine only tracks when the player should be forced to drop the cube because it's being moved
		// on the other side of a portal without a portal between the player and cube
		public enum HoldState {
			NoPortalBetweenCubeAndPlayer,
			PortalBetweenCubeAndPlayer
		}
		private StateMachine<HoldState> holdState = new StateMachine<HoldState>(HoldState.NoPortalBetweenCubeAndPlayer);

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

			renderers = transform.GetComponentsInChildrenRecursively<Renderer>();
			colliders = transform.GetComponentsInChildrenRecursively<Collider>();

			pickupObject.OnPickupSimple += RecalculateGrabbedThroughPortal;
			pickupObject.OnDropSimple += HandleDrop;
		}

		protected override void Start() {
			base.Start();
			OnObjectTeleported += UpdateGrabbedThroughPortalAfterObjectTeleports;
			Portal.BeforeAnyPortalTeleport += (Portal inPortal, Collider objBeingTeleported) => UpdateGrabbedThroughPortalAfterPlayerTeleports(inPortal);
			InitializeHoldStateMachine();
		}

		private void FixedUpdate() {
			if (resetSittingInPortal) {
				_sittingInPortal = null;
			}
			else {
				resetSittingInPortal = true;
			}
		}

		void Update() {

			thisFrameRaycastHits = Interact.instance.GetRaycastHits();

			RecalculateHoveredThroughPortal();
			PreventCubeFromBeingDroppedIfHeldLegallyThroughPortal();

			if (copyShouldBeEnabled && fakeCopyInstance == null) {
				CreateFakeCopyInstance();
			}

			if (copyShouldBeEnabled) {
				UpdateMaterialProperties(portalInteractingWith);
			}
			if (sittingInPortal != null) {
				foreach (var r in renderers) {
					r.enabled = sittingInPortal.portalRenderingIsEnabled;
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
			if (grabbedThroughPortal != null) {
				hoveredThroughPortal = grabbedThroughPortal;
				return;
			}
			bool hoveredOnPickupObj = thisFrameRaycastHits.hitObject && thisFrameRaycastHits.firstObjectHit.collider.gameObject == this.gameObject;
			bool hoveredOnPortalCopy = thisFrameRaycastHits.hitObject && copyIsEnabled && thisFrameRaycastHits.firstObjectHit.collider.gameObject == fakeCopyInstance.gameObject;
			hoveredThroughPortal = (hoveredOnPickupObj || hoveredOnPortalCopy) ? InteractedThroughPortal() : null;
		}

		// Being held illegally means the cube is being carried on the other side of the portal without the last frame raycast hitting that portal
		// (or the player standing inside of that portal)
		void PreventCubeFromBeingDroppedIfHeldLegallyThroughPortal() {
			// The actual dropping happens through a StateMachine trigger, we just reset the timer here if the cube is being held legally
			if (hoveredThroughPortal != null &&
			    (thisFrameRaycastHits.hitPortal == hoveredThroughPortal || hoveredThroughPortal.playerRemainsInPortal)) {
				
				holdState.Set(HoldState.PortalBetweenCubeAndPlayer);
				holdState.timeSinceStateChanged = 0f;
			}
		}

		void HandleDrop() {
			grabbedThroughPortal = null;
		}

		Portal InteractedThroughPortal() {
			bool hoveredOnPortalCopy = copyIsEnabled && thisFrameRaycastHits.hitObject && thisFrameRaycastHits.firstObjectHit.collider.gameObject == fakeCopyInstance.gameObject;
			Portal hoveredThroughPortal = thisFrameRaycastHits.firstValidPortalHit;
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
				SetMaterialsForRenderers(fakeCopyInstance.renderers, usePortalCopyMaterials);
				SetRenderQueueToJustAfterGeometry(fakeCopyInstance.renderers);
				UpdateMaterialProperties(portalInteractingWith);
			}
		}

		void SetRenderQueueToJustAfterGeometry(Renderer[] renderersToChangeMaterialsOf) {
			foreach (Renderer renderer in renderersToChangeMaterialsOf) {
				bool isTransparent = renderer.material.GetInt("__SuberspectiveBlendMode") ==
				                     (int)ShaderUtils.SuberspectiveBlendMode.Transparent;
				if (isTransparent) {
					renderer.material.renderQueue = (int)RenderQueue.Geometry + 1;
				}
			}
		}

		void SetMaterialsForRenderers(Renderer[] renderersToChangeMaterialsOf, bool usePortalCopyMaterials) {
			const string portalCopyKeyword = "PORTAL_COPY_OBJECT";
			for (int i = 0; i < renderers.Length; i++) {
				Renderer rendererToUseAsKey = renderers[i];
				Renderer rendererToModify = renderersToChangeMaterialsOf[i];

				rendererToModify.material = rendererToUseAsKey.material;
			}
			foreach (Renderer renderer in renderersToChangeMaterialsOf) {
				if (usePortalCopyMaterials) {
					renderer.material.EnableKeyword(portalCopyKeyword);
				}
				else {
					renderer.material.DisableKeyword(portalCopyKeyword);
				}
			}
		}

		void InitializeHoldStateMachine() {
			const float timeBeforeForcedToDropCube = 0.15f;
			holdState.AddStateTransition(HoldState.PortalBetweenCubeAndPlayer, HoldState.NoPortalBetweenCubeAndPlayer, timeBeforeForcedToDropCube);
			holdState.AddTrigger(HoldState.NoPortalBetweenCubeAndPlayer, 0f, () => {
				if (pickupObject != null && hoveredThroughPortal != null) {
					pickupObject.Drop();
				}
			});
		}

		#region Saving

		[Serializable]
		public class PortalableObjectSave : SerializableSaveObject<PortalableObject> {
			SerializableReference<Portal, Portal.PortalSave> sittingInPortal;
			SerializableReference<Portal, Portal.PortalSave> hoveredThroughPortal;
			SerializableReference<Portal, Portal.PortalSave> grabbedThroughPortal;
			StateMachine<HoldState>.StateMachineSave holdStateSave;

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

				holdStateSave = obj.holdState.ToSave();
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

				obj.holdState.FromSave(holdStateSave);
			}
		}
		#endregion
	}
}
