using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using PortalMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Events;

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

        private float openDistance = 3.75f;
        private Vector3 closed = Vector3.zero;
        private Vector3 open => Vector3.right * openDistance;

        private const float timeToOpen = 3.75f;

        public PortalDoor portalPartnerDoor;

        public enum DoorState {
            Closed,
            Opening,
            Open,
            Closing
        }

        public StateMachine<DoorState> state = new StateMachine<DoorState>(DoorState.Closed);
        
        // Config
        private const float cameraShakeIntensity = .125f;
        private const float cameraLongShakeIntensity = .03125f;
        private const float cameraShakeDuration = .25f;
        private const float portalMovingSoundDelay = .35f;
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            state.AddStateTransition(DoorState.Opening, DoorState.Open, timeToOpen);
            state.AddStateTransition(DoorState.Closing, DoorState.Closed, timeToOpen);
            
            state.AddTrigger(DoorState.Closed, 0f, () => {
                if (state.prevState == DoorState.Closing) {
                    CameraShake.instance.Shake(cameraShakeDuration, cameraShakeIntensity, 0f);
                    AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingEnd, ID, transform.position);
                }

                SetDoors(0f);
            });
            state.AddTrigger(DoorState.Open, 0f, () => {
                if (transform == null) return;
                
                if (state.prevState == DoorState.Opening) {
                    CameraShake.instance.Shake(cameraShakeDuration, cameraShakeIntensity, 0f);
                    AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingEnd, ID, transform.position);
                }
                SetDoors(1f);
            });
            
            state.AddTrigger(DoorState.Closing, 0f, () => {
                if (transform == null) return;
                
                if (state.prevState == DoorState.Open) {
                    CameraShake.instance.Shake(cameraShakeDuration, cameraShakeIntensity, 0f);
                    AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingStart, ID, transform.position);
                }
            });
            state.AddTrigger(DoorState.Opening, 0f, () => {
                if (state.prevState == DoorState.Closed) {
                    CameraShake.instance.Shake(cameraShakeDuration, cameraShakeIntensity, 0f);
                    AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingStart, ID, transform.position);
                }
            });
            state.AddTrigger((enumValue) => enumValue is DoorState.Closing or DoorState.Opening, portalMovingSoundDelay, () => {
                CameraShake.instance.Shake(timeToOpen - portalMovingSoundDelay, cameraLongShakeIntensity, cameraLongShakeIntensity);
                AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMoving, ID, transform.position);
            });

            renderers = transform.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in transform.GetComponentsInChildren<Renderer>()) {
                Material m = r.material;
                m.SetVector("_MinRenderZone", renderZone.bounds.min);
                m.SetVector("_MaxRenderZone", renderZone.bounds.max);
            }
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
            
            switch (state.state) {
                case DoorState.Open:
                    SetDoors(1);
                    break;
                case DoorState.Closed:
                    SetDoors(0);
                    break;
                case DoorState.Opening:
                    SetDoors(state.timeSinceStateChanged / timeToOpen);
                    break;
                case DoorState.Closing:
                    SetDoors(1 - (state.timeSinceStateChanged / timeToOpen));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void TriggerDoors() {
            UpdateStateFromTriggerDoors();
            
            if (portalPartnerDoor != null) {
                portalPartnerDoor.UpdateStateFromTriggerDoors();
            }
        }

        void UpdateStateFromTriggerDoors() {
            switch (state.state) {
                case DoorState.Closed:
                    state.Set(DoorState.Opening, true);
                    break;
                case DoorState.Opening: {
                    float timeSinceStateChanged = timeToOpen - state.timeSinceStateChanged;
                    state.Set(DoorState.Closing);
                    state.timeSinceStateChanged = timeSinceStateChanged;
                    break;
                }
                case DoorState.Open:
                    state.Set(DoorState.Closing, true);
                    break;
                case DoorState.Closing: {
                    float timeSinceStateChanged = timeToOpen - state.timeSinceStateChanged;
                    state.Set(DoorState.Opening);
                    state.timeSinceStateChanged = timeSinceStateChanged;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Interpolation value t 0-1
        void SetDoors(float t) {
            t = Easing.EaseInOut(t);
            Vector3 curPos = doorRight.localPosition;
            curPos.z = t * openDistance;
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