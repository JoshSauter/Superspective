using System;
using System.Collections;
using System.Collections.Generic;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

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

        private float openDistance = 3.75f;
        private Vector3 closed = Vector3.zero;
        private Vector3 open => Vector3.right * openDistance;

        private float timeToOpen = 4f;
        
        public enum DoorState {
            Closed,
            Opening,
            Open,
            Closing
        }

        public StateMachine<DoorState> state = new StateMachine<DoorState>(DoorState.Closed);
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            state.AddStateTransition(DoorState.Opening, DoorState.Open, timeToOpen);
            state.AddStateTransition(DoorState.Closing, DoorState.Closed, timeToOpen);
            
            state.AddTrigger(DoorState.Closed, 0f, () => SetDoors(0f));
            state.AddTrigger(DoorState.Open, 0f, () => SetDoors(1f));

            foreach (Renderer r in transform.GetComponentsInChildren<Renderer>()) {
                Material m = r.material;
                m.SetVector("_MinRenderZone", renderZone.bounds.min);
                m.SetVector("_MaxRenderZone", renderZone.bounds.max);
            }
        }

        // Update is called once per frame
        void Update() {
            if (DebugInput.GetKeyDown("o")) {
                TriggerDoors();
            }
            
            switch (state.state) {
                case DoorState.Open:
                case DoorState.Closed:
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
                portalDoor.state.FromSave(stateSave);
            }
		}
#endregion
}