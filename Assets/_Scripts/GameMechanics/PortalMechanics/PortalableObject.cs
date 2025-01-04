using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;

namespace PortalMechanics {
    [RequireComponent(typeof(UniqueId))]
    public class PortalableObject : SuperspectiveObject<PortalableObject, PortalableObject.PortalableObjectSave> {
        public delegate void PortalObjectAction(Portal inPortal);
        public event PortalObjectAction OnObjectTeleported;
        
        public PortalCopy portalCopyPrefab;
        [ShowNonSerializedField]
        private PortalCopy _portalCopy;
        public PortalCopy PortalCopy {
            get {
                if (!_portalCopy && portalCopyPrefab) {
                    _portalCopy = Instantiate(portalCopyPrefab, transform.position, transform.rotation);
                    _portalCopy.originalPortalableObj = this;
                }

                return _portalCopy;
            }
        }
        
        public bool IsPortaled => IsInPortal || IsHeldThroughPortal;

        // This is the portal the object is currently in the trigger zone of
        private Portal _portal;
        [ShowNativeProperty]
        public Portal Portal => _portal;
        public bool IsInPortal => _portal != null;
        
        // This is the portal the object is currently held through
        private Portal _portalHeldThrough = null;
        [ShowNativeProperty]
        public Portal PortalHeldThrough => _portalHeldThrough;
        public bool IsHeldThroughPortal => _portalHeldThrough != null;

        public Collider[] colliders;
        private HashSet<Collider> collidersHash = new HashSet<Collider>();
        public Renderer[] renderers;
        
        private InteractableObject _interactObject;
        public InteractableObject InteractObject {
            get {
                if (!_interactObject) {
                    _interactObject = GetComponent<InteractableObject>();
                }
                
                return _interactObject;
            }
        }

        private GravityObject _gravityObject;
        private GravityObject GravityObject {
            get {
                if (!_gravityObject) {
                    _gravityObject = GetComponent<GravityObject>();
                }

                return _gravityObject;
            }
        }
        
        private PickupObject _pickupObject;
        public PickupObject PickupObject {
            get {
                if (!_pickupObject) {
                    _pickupObject = GetComponent<PickupObject>();
                }

                return _pickupObject;
            }
        }
        
        private PillarDimensionObject[] _pillarDimensionObjects;
        private PillarDimensionObject[] PillarDimensionObjects {
            get {
                if (_pillarDimensionObjects == null || _pillarDimensionObjects.Length == 0) {
                    _pillarDimensionObjects = transform.GetComponentsInChildren<PillarDimensionObject>();
                }

                return _pillarDimensionObjects;
            }
        }

        // Prevents ExitPortal from being called when the object is teleported through a portal
        public bool teleportedThisFixedUpdate = false;

        public enum HeldThroughPortalResetState : byte {
            Idle,
            RaycastGoesThroughPortal,
            RaycastDoesNotGoThroughPortal
        }
        private StateMachine<HeldThroughPortalResetState> heldThroughPortalResetState;
        // Time that a cube should be held NOT through a portal before resetting the held through portal
        // As determined by the player's raycast hits
        private const float HELD_THROUGH_PORTAL_RESET_TIME = 0.5f;

        protected override void Awake() {
            base.Awake();

            InitializeHeldThroughPortalResetStateMachine();

            if (PickupObject) {
                PickupObject.OnDropSimple += () => _portalHeldThrough = null;
            }
            
            colliders = transform.GetComponentsInChildrenRecursively<Collider>();
            renderers = transform.GetComponentsInChildrenRecursively<Renderer>();
            collidersHash = new HashSet<Collider>(colliders);
        }

        private void InitializeHeldThroughPortalResetStateMachine() {
            heldThroughPortalResetState = this.StateMachine(HeldThroughPortalResetState.Idle);

            void StateUpdate(float _) {
                // Cube is held and IsHeldThroughPortal is true, check whether or not the player's raycast goes through a portal
                if (PickupObject && PickupObject.isHeld && IsHeldThroughPortal) {
                    bool raycastHitPortal = Interact.instance.GetRaycastHits().DidHitAnyPortal;
                    if (raycastHitPortal) {
                        heldThroughPortalResetState.Set(HeldThroughPortalResetState.RaycastGoesThroughPortal);
                    }
                    else {
                        heldThroughPortalResetState.Set(HeldThroughPortalResetState.RaycastDoesNotGoThroughPortal);
                    }
                }
                // Cube not held, don't do anything
                else {
                    heldThroughPortalResetState.Set(HeldThroughPortalResetState.Idle);
                }
            }
            
            // All states should operate the same (for simplicity)
            heldThroughPortalResetState.WithUpdate(StateUpdate);
            
            // If the cube has continuously been not held through a portal while portalHeldThrough is not null, reset the portalHeldThrough
            heldThroughPortalResetState.AddTrigger(HeldThroughPortalResetState.RaycastDoesNotGoThroughPortal, HELD_THROUGH_PORTAL_RESET_TIME, () => {
                _portalHeldThrough = null;
                heldThroughPortalResetState.Set(HeldThroughPortalResetState.Idle);
            });
        }

        private void FixedUpdate() {
            // Is executed after the portal teleportation event, so we can reset the flag here
            teleportedThisFixedUpdate = false;
        }

        protected override void Start() {
            base.Start();
            
            Portal.OnAnyPortalPlayerTeleport += OnPlayerTeleport;
        }

        protected override void OnDisable() {
            base.OnDisable();
            Portal.OnAnyPortalPlayerTeleport -= OnPlayerTeleport;
        }

        void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;

            PortalCopy.SetPortalCopyEnabled(IsPortaled);
            InteractObject.glow.renderers = IsPortaled ?
                (PortalCopy.renderers.Concat(renderers).ToList()) :
                renderers.ToList();
        }
        
        void LateUpdate() {
            if (GameManager.instance.IsCurrentlyLoading) return;

            if (IsInPortal) {
                // Consider the case where a cube moves through a portal very quickly. It may be in the trigger collider for one portal,
                // get teleported, then immediately be outside of the trigger collider for the other portal, without ever triggering the
                // exit condition for the other portal. This checks if the object is still in the trigger collider for the other portal.
                // If it is not, manually and immediately trigger the exit condition.
                // NOTE: Only using the first collider for this check, which may not be a correct assumption but is more performant
                if (!_portal.triggerColliders.Any(c => SuperspectivePhysics.CollidersOverlap(c, colliders[0]))) {
                    ExitPortal(_portal);
                }
            }
        }

        public void EnterPortal(Portal portalEntered) {
            if (_portal == portalEntered) {
                debug.LogWarning("Already in portal, not entering again");
                return;
            };
            
            debug.Log($"{gameObject.FullPath()} entering portal {portalEntered.name}");

            _portal = portalEntered;
            portalEntered.objectsInPortal.Add(this);
            PortalCopy.SetPortalCopyEnabled(true);
            
            portalEntered.OnPortalTeleport += OnPortalTeleport;
        }

        public void ExitPortal(Portal portalExited) {
            debug.Log($"{gameObject.FullPath()} exiting portal {portalExited.name}");

            portalExited.OnPortalTeleport -= OnPortalTeleport;
            _portal = null;
            portalExited.objectsInPortal.Remove(this);
            PortalCopy.SetPortalCopyEnabled(false);
        }

        /// <summary>
        /// Handles the teleportation of this object through a portal, then stops listening for the event
        /// </summary>
        /// <param name="inPortal"></param>
        /// <param name="objectTeleported"></param>
        private void OnPortalTeleport(Portal inPortal, Collider objectTeleported) {
            if (!collidersHash.Contains(objectTeleported)) return;
            
            teleportedThisFixedUpdate = true;
            EnterPortal(inPortal.otherPortal);
            OnObjectTeleported?.Invoke(inPortal);

            if (PickupObject && PickupObject.isHeld) {
                // Player pushed a cube through a portal, now holding through that portal
                if (!IsHeldThroughPortal) {
                    SetPortalHeldThrough(inPortal);
                }
                // Player pulled a cube back through a portal it was held through, now no longer holding through a portal
                else if (inPortal.otherPortal == PortalHeldThrough) {
                    SetPortalHeldThrough(null);
                }
                else {
                    Debug.LogWarning($"Not sure if this is the right thing to do here, but setting portal held through to {inPortal.name}");
                    SetPortalHeldThrough(inPortal);
                }
            }

            if (inPortal.otherPortal.pillarDimensionObject) {
                foreach (PillarDimensionObject pillarDimensionObject in PillarDimensionObjects) {
                    pillarDimensionObject.Dimension = inPortal.otherPortal.pillarDimensionObject.Dimension;
                }
            }
                
            inPortal.OnPortalTeleport -= OnPortalTeleport;
        }

        private void OnPlayerTeleport(Portal inPortal) {
            if (PickupObject && PickupObject.isHeld) {
                // Player walked forward into a portal that the cube is held through, no longer holding through a portal
                if (IsHeldThroughPortal && PortalHeldThrough == inPortal) {
                    SetPortalHeldThrough(null);
                }
                // Player walked backward into a portal, now holding the cube through the out portal
                else if (!IsHeldThroughPortal) {
                    SetPortalHeldThrough(inPortal.otherPortal);
                }
                else {
                    Debug.LogWarning($"Not sure if this is the right thing to do here, but setting portal held through to {inPortal.otherPortal.name}");
                    SetPortalHeldThrough(inPortal.otherPortal);
                }
            }
        }

        public void SetPortalHeldThrough(Portal heldThrough) {
            _portalHeldThrough = heldThrough;
        }
#region Saving

        public override void LoadSave(PortalableObjectSave save) {
            if (save.portalSittingInRef != null && save.portalSittingInRef.TryGet(out Portal portalSittingIn)) {
                EnterPortal(portalSittingIn);
            }
            
            if (save.portalHeldThroughRef != null && save.portalHeldThroughRef.TryGet(out Portal portalHeldThrough)) {
                SetPortalHeldThrough(portalHeldThrough);
            }
            
            heldThroughPortalResetState.LoadFromSave(save.heldThroughPortalResetStateSave);
        }

        [Serializable]
        public class PortalableObjectSave : SaveObject<PortalableObject> {
            public SuperspectiveReference<Portal, Portal.PortalSave> portalSittingInRef;
            public SuperspectiveReference<Portal, Portal.PortalSave> portalHeldThroughRef;
            public StateMachine<HeldThroughPortalResetState>.StateMachineSave heldThroughPortalResetStateSave;

            public PortalableObjectSave(PortalableObject script) : base(script) {
                portalSittingInRef = script._portal;
                portalHeldThroughRef = script._portalHeldThrough;
                heldThroughPortalResetStateSave = script.heldThroughPortalResetState.ToSave();
            }
        }
#endregion
    }
}
