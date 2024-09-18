using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Interactables;
using PortalMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    [RequireComponent(typeof(UniqueId))]
    public class PortalDoor : SaveableObject<PortalDoor, PortalDoorSave> {
        [SerializeField]
        private Transform doorLeft;
        [SerializeField]
        private Transform doorRight;

        [SerializeField]
        private Transform doorHitboxLeft;
        [SerializeField]
        private Transform doorHitboxRight;

        public float minHitboxScale;

        [SerializeField]
        private BoxCollider renderZone;
        private Renderer[] renderers;
        public bool renderersCanMove = false;

        private Vector3 closed = Vector3.zero;
        private Vector3 open => Vector3.right * OPEN_DISTANCE;

        public PortalDoor portalPartnerDoor;

        public StateMachine<DoorState> state;


        // Define the different states the door can be in
        public enum DoorState {
            Closed,
            Opening,
            Open,
            Closing
        }

        // Config
        private const float OPEN_CLOSE_TIME = 3.75f;
        private const float OPEN_DISTANCE = 3.75f;
        private const float CAMERA_SHAKE_INTENSITY = .125f;
        private const float CAMERA_LONG_SHAKE_INTENSITY = .03125f;
        private const float CAMERA_SHAKE_DURATION = .25f;
        private const float DOOR_MOVING_SOUND_DELAY = .35f;
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();

            InitializeStateMachine();

            renderers = transform.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in transform.GetComponentsInChildren<Renderer>()) {
                Material m = r.material;
                m.SetVector("_MinRenderZone", renderZone.bounds.min);
                m.SetVector("_MaxRenderZone", renderZone.bounds.max);
            }
        }

        private PortalDoor portalDoor;
        private Portal portal;

        private void InitializeStateMachine() {
            // Set initial state
            state = this.StateMachine(DoorState.Closed);
            
            // Set up timed state transitions
            state.AddStateTransition(DoorState.Opening, DoorState.Open, OPEN_CLOSE_TIME);
            state.AddStateTransition(DoorState.Closing, DoorState.Closed, OPEN_CLOSE_TIME);
            
            // Add triggers that happen when a state is entered:
            state.AddTrigger(DoorState.Closed, () => SetDoorsOpenAmount(0f));
            state.AddTrigger(DoorState.Open, () => SetDoorsOpenAmount(1f));
            
            // Define code that should run every frame for a given state:
            state.WithUpdate(DoorState.Opening, time => SetDoorsOpenAmount(time / OPEN_CLOSE_TIME));
            state.WithUpdate(DoorState.Closing, time => SetDoorsOpenAmount(1 - time / OPEN_CLOSE_TIME));
            
            state.AddTrigger(DoorState.Closing, () => {
                if (state.PrevState == DoorState.Open) {
                    CameraShake.instance.Shake(transform.position, CAMERA_SHAKE_INTENSITY, CAMERA_SHAKE_DURATION);
                    AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingStart, ID, transform.position);
                }
            });
            state.AddTrigger(DoorState.Opening, () => {
                if (state.PrevState == DoorState.Closed) {
                    CameraShake.instance.Shake(transform.position, CAMERA_SHAKE_INTENSITY, CAMERA_SHAKE_DURATION);
                    AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingStart, ID, transform.position);
                }
            });
            // Add trigger that happens at a certain time after entering a given state
            state.AddTrigger(
                enumValue => enumValue is DoorState.Closing or DoorState.Opening, 
                DOOR_MOVING_SOUND_DELAY, 
                () => {
                    CameraShake.CameraShakeEvent shake = new CameraShake.CameraShakeEvent() {
                        duration = CAMERA_SHAKE_DURATION,
                        intensity = CAMERA_LONG_SHAKE_INTENSITY,
                        intensityCurve = AnimationCurve.Constant(0, 1, 1),
                        locationProvider = () => transform.position,
                        spatial = .75f
                    };
                CameraShake.instance.Shake(shake);
                AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMoving, ID, transform.position);
            });
            
        }

        // Update is called once per frame
        void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;
            
            if (renderersCanMove) {
                foreach (Renderer r in renderers) {
                    Material m = r.material;
                    m.SetVector("_MinRenderZone", renderZone.bounds.min);
                    m.SetVector("_MaxRenderZone", renderZone.bounds.max);
                }
            }
            
            if (DebugInput.GetKeyDown("o")) {
                TriggerDoors();
            }
        }

        public void TriggerDoors() {
            UpdateStateFromTriggerDoors();
            
            if (portalPartnerDoor != null) {
                portalPartnerDoor.UpdateStateFromTriggerDoors();
            }
        }

        void UpdateStateFromTriggerDoors() {
            switch (state.State) {
                case DoorState.Closed:
                    state.Set(DoorState.Opening, true);
                    break;
                case DoorState.Opening: {
                    float timeSinceStateChanged = OPEN_CLOSE_TIME - state.Time;
                    state.Set(DoorState.Closing);
                    state.Time = timeSinceStateChanged;
                    break;
                }
                case DoorState.Open:
                    state.Set(DoorState.Closing, true);
                    break;
                case DoorState.Closing: {
                    float timeSinceStateChanged = OPEN_CLOSE_TIME - state.Time;
                    state.Set(DoorState.Opening);
                    state.Time = timeSinceStateChanged;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Interpolation value t 0-1
        void SetDoorsOpenAmount(float t) {
            t = Easing.EaseInOut(t);
            Vector3 curPos = doorRight.localPosition;
            curPos.z = t * OPEN_DISTANCE;
            doorRight.localPosition = curPos;
            curPos.z *= -1;
            doorLeft.localPosition = curPos;

            Vector3 curScale = doorHitboxRight.localScale;
            curScale.z = Mathf.Lerp(minHitboxScale, 1, 1-t);
            doorHitboxRight.localScale = curScale;
            doorHitboxLeft.localScale = curScale;
        }
    }

    #region Saving
		
		[Serializable]
		public class PortalDoorSave : SerializableSaveObject<PortalDoor> {
            private StateMachine<PortalDoor.DoorState>.StateMachineSave stateSave;

			public PortalDoorSave(PortalDoor portalDoor) : base(portalDoor) {
                this.stateSave = portalDoor.state.ToSave();
            }

			public override void LoadSave(PortalDoor portalDoor) {
                portalDoor.state.LoadFromSave(stateSave);
            }
		}
#endregion
}