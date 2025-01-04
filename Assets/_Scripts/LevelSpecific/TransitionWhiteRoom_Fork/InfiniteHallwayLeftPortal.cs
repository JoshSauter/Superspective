using System;
using MagicTriggerMechanics;
using PortalMechanics;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

// TODO: Entering the portal from the top doesn't work right... The portal renders incorrectly
// TODO: Loading doesn't seem to put the text in the right spot, it reverts to its original position
namespace LevelSpecific.TransitionWhiteRoom_Fork {
    public class InfiniteHallwayLeftPortal : SuperspectiveObject<InfiniteHallwayLeftPortal, InfiniteHallwayLeftPortal.Save> {
        private const int MAX_INF_TELEPORT_FOR_TEXT = 11; // How many times we can move the text before we should just turn it off
        
        public MagicTrigger enterInfiniteHallwayTrigger;
        public MagicTrigger pastTeleportTrigger;
        public MagicTrigger pastTeleportTriggerUpper;
        public int timesInfTeleported = 0;
        bool playerIsInInfiniteHallway = false;
        bool exitsAreConnected = false;
        public Portal upperPlatformPortal;
        public Portal lowerPlatformPortal;
        public TeleportEnter infiniteTeleporter;
        public GlobalMagicTrigger goingNowhereText;

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

            pastTeleportTrigger.OnMagicTriggerStayOneTime += ConnectExits;
            pastTeleportTriggerUpper.OnMagicTriggerStayOneTime += ConnectExits;
            
            infiniteTeleporter.OnTeleport += MoveText;
        }

        void MoveText(Collider teleportEnter, Collider teleportExit, GameObject player) {
            goingNowhereText.transform.position += (teleportExit.transform.position - teleportEnter.transform.position);
            timesInfTeleported++;
            if (timesInfTeleported >= MAX_INF_TELEPORT_FOR_TEXT) {
                // Turn on the GlobalMagicTrigger that will disable the text when the player looks away
                goingNowhereText.enabled = true;
            }
        }

        void UnsubscribeFromEvents() {
            enterInfiniteHallwayTrigger.OnMagicTriggerStayOneTime -= EnterInfiniteHallway;
            enterInfiniteHallwayTrigger.OnNegativeMagicTriggerStayOneTime -= ExitInfiniteHallway;

            upperPlatformPortal.OnPortalTeleportSimple -= EnterInfiniteHallwayOnPortalEnter;
            lowerPlatformPortal.OnPortalTeleportSimple -= ExitInfiniteHallwayOnPortalEnter;

            pastTeleportTrigger.OnMagicTriggerStayOneTime -= ConnectExits;
            pastTeleportTriggerUpper.OnMagicTriggerStayOneTime -= ConnectExits;
            
            infiniteTeleporter.OnTeleport += MoveText;
        }

        void Update() {
            if (exitsAreConnected && !playerIsInInfiniteHallway) {
                Camera playerCam = SuperspectiveScreen.instance.playerCamera;
                if (!upperPlatformPortal.IsVisibleFrom(playerCam) && !lowerPlatformPortal.IsVisibleFrom(playerCam)) {
                    exitsAreConnected = false;
                }
            }

            lowerPlatformPortal.transform.forward = exitsAreConnected ? Vector3.right : Vector3.left;
            float lowerPortalPosX = exitsAreConnected ? 102 : 100;
            Vector3 curPos = lowerPlatformPortal.transform.localPosition;
            curPos.x = lowerPortalPosX;
            lowerPlatformPortal.transform.localPosition = curPos;

            infiniteTeleporter.teleportEnter.enabled = playerIsInInfiniteHallway && !exitsAreConnected;
        }

        void ConnectExits() {
            exitsAreConnected = true;
        }
        
        void EnterInfiniteHallwayOnPortalEnter(Collider objPortaled) {
            if (objPortaled.TaggedAsPlayer()) {
                EnterInfiniteHallway();
            }
        }

        void ExitInfiniteHallwayOnPortalEnter(Collider objPortaled) {
            if (objPortaled.TaggedAsPlayer()) {
                ExitInfiniteHallway();
            }
        }

        void EnterInfiniteHallway() {
            playerIsInInfiniteHallway = true;
        }

        void ExitInfiniteHallway() {
            playerIsInInfiniteHallway = false;
        }
    
#region Saving
        public override void LoadSave(Save save) {
            playerIsInInfiniteHallway = save.playerIsInInfiniteHallway;
            exitsAreConnected = save.exitsAreConnected;
            timesInfTeleported = save.timesInfTeleported;
        }
        
        public override string ID => "InfiniteHallwayLeftPortal";
    
        [Serializable]
        public class Save : SaveObject<InfiniteHallwayLeftPortal> {
            public bool playerIsInInfiniteHallway;
            public bool exitsAreConnected;
            public int timesInfTeleported;

            public Save(InfiniteHallwayLeftPortal script) : base(script) {
                playerIsInInfiniteHallway = script.playerIsInInfiniteHallway;
                exitsAreConnected = script.exitsAreConnected;
                timesInfTeleported = script.timesInfTeleported;
            }
        }
#endregion
    }
}
