using System;
using Audio;
using MagicTriggerMechanics;
using PortalMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.TransitionWhiteRoom_Fork {
    public class InfiniteHallwayRightPortal : SuperspectiveObject<InfiniteHallwayRightPortal, InfiniteHallwayRightPortal.Save> {
        public enum RightHallwayState : byte {
            Inactive,
            PlayerInHallway,
            ExitsConnected,
            Solved
        }
        public StateMachine<RightHallwayState> rightHallwayState;
        
        public MagicTrigger enterInfiniteHallwayTrigger;
        public Portal upperPlatformPortal;
        public Portal lowerPlatformPortal;
        public TeleportEnter turnAroundTeleport;
        public TeleportEnter turnAroundTeleportBackwards;
        bool ExitsAreConnected => rightHallwayState.State is RightHallwayState.Solved or RightHallwayState.ExitsConnected;
        public GameObject inCirclesText;

        protected override void Awake() {
            base.Awake();
            
            lowerPlatformPortal.SetPortalModes(!ExitsAreConnected ? PortalRenderMode.Invisible : PortalRenderMode.Normal, !ExitsAreConnected ? PortalPhysicsMode.None : PortalPhysicsMode.Normal);

            InitializeStateMachine();
        }

        private void InitializeStateMachine() {
            rightHallwayState = this.StateMachine(RightHallwayState.Inactive);
            
            rightHallwayState.AddTrigger(RightHallwayState.ExitsConnected, () => {
                float intensity = 12f;
                float duration = 1.5f;
                CameraShake.instance.Shake(intensity, duration);
                AudioManager.instance.PlayAtLocation(AudioName.WallsShifting, "InfiniteHallwayRight", turnAroundTeleport.transform.position);
                inCirclesText.gameObject.SetActive(true);
            });
        }

        protected override void Start() {
            base.Start();
            
            SubscribeToEvents();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents() {
            enterInfiniteHallwayTrigger.OnMagicTriggerStayOneTime += EnterInfiniteHallway;
            enterInfiniteHallwayTrigger.OnNegativeMagicTriggerStayOneTime += ExitInfiniteHallway;
            
            upperPlatformPortal.OnPortalTeleportSimple += EnterInfiniteHallwayOnPortalEnter;
            lowerPlatformPortal.OnPortalTeleportSimple += ExitInfiniteHallwayOnPortalEnter;

            turnAroundTeleport.OnTeleportSimple += ConnectExits;
            turnAroundTeleportBackwards.OnTeleportSimple += ConnectExits;
        }

        void UnsubscribeFromEvents() {
            enterInfiniteHallwayTrigger.OnMagicTriggerStayOneTime -= EnterInfiniteHallway;
            enterInfiniteHallwayTrigger.OnNegativeMagicTriggerStayOneTime -= ExitInfiniteHallway;
            
            upperPlatformPortal.OnPortalTeleportSimple -= EnterInfiniteHallwayOnPortalEnter;
            lowerPlatformPortal.OnPortalTeleportSimple -= ExitInfiniteHallwayOnPortalEnter;

            turnAroundTeleport.OnTeleportSimple -= ConnectExits;
            turnAroundTeleportBackwards.OnTeleportSimple -= ConnectExits;
        }

        void Update() {
            bool shouldPause = !ExitsAreConnected;

            lowerPlatformPortal.SetPortalModes(shouldPause ? PortalRenderMode.Invisible : PortalRenderMode.Normal, shouldPause ? PortalPhysicsMode.None : PortalPhysicsMode.Normal);

            turnAroundTeleport.teleportEnter.enabled = !ExitsAreConnected;
        }

        // Called in a UnityEvent from the puzzle solved trigger zones
        public void MarkAsSolved() {
            rightHallwayState.Set(RightHallwayState.Solved);
        }

        void ConnectExits() {
            if (rightHallwayState.State is RightHallwayState.PlayerInHallway) {
                rightHallwayState.Set(RightHallwayState.ExitsConnected);
            }
        }
        
        void EnterInfiniteHallwayOnPortalEnter(Collider objPortaled) {
            if (objPortaled.TaggedAsPlayer()) {
                EnterInfiniteHallway();
                ConnectExits();
            }
        }

        void ExitInfiniteHallwayOnPortalEnter(Collider objPortaled) {
            if (objPortaled.TaggedAsPlayer()) {
                ExitInfiniteHallway();
            }
        }

        void EnterInfiniteHallway() {
            if (rightHallwayState == RightHallwayState.Inactive) {
                rightHallwayState.Set(RightHallwayState.PlayerInHallway);
            }
        }

        void ExitInfiniteHallway() {
            if (rightHallwayState.State is RightHallwayState.PlayerInHallway or RightHallwayState.ExitsConnected) {
                rightHallwayState.Set(RightHallwayState.Inactive);
            }
        }
    
#region Saving

        public override void LoadSave(Save save) { }

        public override string ID => "InfiniteHallwayRightPortal";
        
        [Serializable]
        public class Save : SaveObject<InfiniteHallwayRightPortal> {
            public Save(InfiniteHallwayRightPortal script) : base(script) { }
        }
#endregion
    }
}
