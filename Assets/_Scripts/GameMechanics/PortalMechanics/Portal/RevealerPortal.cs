using System;
using MagicTriggerMechanics;
using MagicTriggerMechanics.TriggerActions;
using MagicTriggerMechanics.TriggerConditions;
using UnityEngine;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using StateUtils;
using SuperspectiveUtils;

namespace PortalMechanics {
    [RequireComponent(typeof(UniqueId))]
    public class RevealerPortal : Portal {
        private const float LOOK_AWAY_TRIGGER_TIME = 0.1f;
        private static readonly Color _GUIColor = new Color(.75f, .55f, .25f, 1f);
        
        protected override int PortalsRequiredToActivate => 1;

        protected override PortalRenderMode EffectiveRenderMode => PortalRenderMode.DimensionWall;
        protected override int PortalLayer => SuperspectivePhysics.VisibilityMaskLayer;
        protected override int PortalTriggerZoneLayer => SuperspectivePhysics.VisibilityMaskLayer;
        protected override int VolumetricPortalLayer => SuperspectivePhysics.VisibilityMaskLayer;

        protected override bool ExtraVolumetricPortalsEnabledCondition => revealState == RevealState.PartiallyVisible;
        // Because we don't actually teleport the player, allowing raycast teleporting can cause double-triggering which breaks the logic
        protected override bool AllowRaycastPortalTeleporting => false;

        private bool PlayerBehindPortal => Vector3.Dot(IntoPortalVector, transform.position - Player.instance.transform.position) < -0.01f;
        
        [TabGroup("Revealer Config")]
        [GUIColor(nameof(_GUIColor))]
        public int dimensionChannel;

        [TabGroup("Revealer Config")]
        [GUIColor(nameof(_GUIColor))]
        public SuperspectiveReference<DimensionObject, DimensionObject.DimensionObjectSave>[] objectsToBecomeVisible;

        [TabGroup("Revealer Config")]
        [GUIColor(nameof(_GUIColor))]
        public SuperspectiveReference<DimensionObject, DimensionObject.DimensionObjectSave>[] objectsToBecomeInvisible;

        [TabGroup("Revealer Config")]
        [GUIColor(nameof(_GUIColor))]
        public GlobalMagicTrigger lookAwayTrigger;

        [TabGroup("Revealer Config")]
        [GUIColor(nameof(_GUIColor))]
        public StateMachine<RevealState> revealState;

        [TabGroup("Revealer Config")]
        [GUIColor(nameof(_GUIColor))]
        public RevealState startingRevealState = RevealState.PartiallyVisible;

        public enum RevealState : byte {
            Invisible,
            PartiallyVisible,
            Visible
        }

        // There's probably a cleaner way to do this
        private Material _dimensionWallMaterial;
        private Material DimensionWallMaterial => _dimensionWallMaterial ??= new Material(Resources.Load<Material>("Materials/DimensionWalls/DimensionWall" + dimensionChannel));

        protected override void Awake() {
            base.Awake();
            
            revealState = this.StateMachine(startingRevealState);
        }
        
        protected override void Start() {
            base.Start();
            
            SetMaterial(DimensionWallMaterial);

            InitializeRevealStateMachine();
        }

        private void SetVisibilityStates(VisibilityState visibleObjectState, VisibilityState invisibleObjectState) {
            foreach (var dimObjRef in objectsToBecomeVisible) {
                dimObjRef.Reference.MatchAction(
                    dimObj => dimObj.SwitchVisibilityState(visibleObjectState),
                    dimObjSave => dimObjSave.visibilityState = visibleObjectState
                );
            }
                
            foreach (var dimObjRef in objectsToBecomeInvisible) {
                dimObjRef.Reference.MatchAction(
                    dimObj => dimObj.SwitchVisibilityState(invisibleObjectState),
                    dimObjSave => dimObjSave.visibilityState = invisibleObjectState
                );
            }
        }

        private void ResetVisibilityStates() {
            foreach (var dimObjRef in objectsToBecomeVisible) {
                dimObjRef.Reference.MatchAction(
                    dimObj => dimObj.SwitchVisibilityState(dimObj.startingVisibilityState),
                    dimObjSave => dimObjSave.visibilityState = dimObjSave.startingVisibilityState
                );
            }
                
            foreach (var dimObjRef in objectsToBecomeInvisible) {
                dimObjRef.Reference.MatchAction(
                    dimObj => dimObj.SwitchVisibilityState(dimObj.startingVisibilityState),
                    dimObjSave => dimObjSave.visibilityState = dimObjSave.startingVisibilityState
                );
            }
        }

        private void InitializeRevealStateMachine() {
            revealState.AddTrigger(RevealState.Invisible, () => {
                SetVisibilityStates(VisibilityState.Invisible, VisibilityState.Visible);
                lookAwayTrigger.enabled = true;
            });
            revealState.AddTrigger(RevealState.PartiallyVisible, ResetVisibilityStates);
            revealState.AddTrigger(RevealState.Visible, () => SetVisibilityStates(VisibilityState.Visible, VisibilityState.Invisible));
        }

        protected override void FixedUpdate() {
            base.FixedUpdate();

            if (!gameObject.IsInActiveScene()) return;
            
            // If the player moves to the backside of the portal, immediately set the render state to invisible
            // We will restore it to partially visible when the player is in front of the portal and looking away
            if (!PlayerIsInThisPortal && PlayerBehindPortal) {
                if (revealState == RevealState.PartiallyVisible) {
                    revealState.Set(RevealState.Invisible);
                }
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            
            trigger.OnMagicTriggerStayOneTime += TriggerVisibilitySwitchForwards;
            trigger.OnNegativeMagicTriggerStayOneTime += TriggerVisibilitySwitchBackwards;

            lookAwayTrigger.OnMagicTriggerStay += PlayerLookedAway;
        }

        private void PlayerLookedAway() {
            if (revealState.State == RevealState.Invisible) {
                revealState.Set(RevealState.PartiallyVisible);
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            
            trigger.OnMagicTriggerStayOneTime -= TriggerVisibilitySwitchForwards;
            trigger.OnNegativeMagicTriggerStayOneTime -= TriggerVisibilitySwitchBackwards;
            
            lookAwayTrigger.OnMagicTriggerStay -= PlayerLookedAway;
        }

        public override void TeleportPlayer(Transform player) {
            // Do nothing, we don't actually teleport the player
        }
        
        private void TriggerVisibilitySwitchForwards() => TriggerVisibilitySwitch(true);
        private void TriggerVisibilitySwitchBackwards() => TriggerVisibilitySwitch(false);

        public void TriggerVisibilitySwitch(bool forwards) {
            switch (revealState.State) {
                case RevealState.Invisible:
                    // Nothing happens if we walk forwards while its inactive
                    break;
                case RevealState.PartiallyVisible:
                    revealState.Set(forwards ? RevealState.Visible : RevealState.Invisible);
                    break;
                case RevealState.Visible:
                    if (!forwards) {
                        revealState.Set(RevealState.PartiallyVisible);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Revealer portals don't move the player (or anything else), they just reveal some DimensionObjects as the player walks through
        public override Vector3 TransformPoint(Vector3 point) => point;
        public override Vector3 TransformDirection(Vector3 direction) => direction;
        public override Quaternion TransformRotation(Quaternion rotation) => rotation;

#if UNITY_EDITOR
        public override void InitializePortal() {
            base.InitializePortal();
            
            InitializeLookAwayTrigger();
        }
#endif

        private void InitializeLookAwayTrigger() {
            lookAwayTrigger = gameObject.GetOrAddComponent<GlobalMagicTrigger>();
            lookAwayTrigger.id = id;
            
            lookAwayTrigger.ClearTriggerConditions();
            lookAwayTrigger.ClearTriggerActions();

            // All portal and volumetric portals need to not be visible
            foreach (var r in renderers) {
                lookAwayTrigger.AddTriggerCondition(new RendererNotVisibleCondition() {
                    mustBeTriggeredForPeriodOfTime = true,
                    triggerTime = LOOK_AWAY_TRIGGER_TIME,
                    targetRenderer = r.r
                });
            }
            foreach (var vp in volumetricPortals) {
                lookAwayTrigger.AddTriggerCondition(new RendererNotVisibleCondition() {
                    mustBeTriggeredForPeriodOfTime = true,
                    triggerTime = LOOK_AWAY_TRIGGER_TIME,
                    targetRenderer = vp.r
                });
            }
            
            // And the player must not already be in front of the portal plane
            lookAwayTrigger.AddTriggerCondition(new PlayerInDirectionFromPointCondition() {
                useLocalCoordinates = true,
                targetPosition = Vector3.zero,
                targetDirection = Vector3.back,
                triggerThreshold = 0.01f
            });
            
            // And the player must not be in the trigger zone
            Collider[] colliders = (trigger?.colliders?.Length ?? 0) > 0 ? trigger.colliders : trigger.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) {
                lookAwayTrigger.AddTriggerCondition(new PlayerOutsideOfColliderCondition() {
                    targetObject = c
                });
            }

            // Other behavior added in OnEnable as an event subscription
            lookAwayTrigger.AddTriggerAction(new DisableSelfScriptAction() {
                actionTiming = ActionTiming.OnceWhileOnStay
            });
            

            lookAwayTrigger.enabled = false;
        }

#region Saving
        // When inheriting from a SuperspectiveObject, we need to override the CreateSave method to return the type of SaveObject that has the data we want to save
        // Otherwise the SaveObject will be of the base class save object type
        public override SaveObject CreateSave() {
            return new RevealerPortalSave(this);
        }

        public override void LoadSave(PortalSave save) {
            base.LoadSave(save);

            if (save is not RevealerPortalSave revealerPortalSave) {
                Debug.LogError($"Expected save object of type {nameof(RevealerPortalSave)} but got {save.GetType()} instead");
                return;
            }
            
        }

        [Serializable]
        public class RevealerPortalSave : PortalSave {
            public RevealerPortalSave(RevealerPortal script) : base(script) { }
        }
#endregion
    }
}
