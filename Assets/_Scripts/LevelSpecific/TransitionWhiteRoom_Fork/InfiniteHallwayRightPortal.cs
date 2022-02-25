using System;
using System.Collections;
using System.Collections.Generic;
using MagicTriggerMechanics;
using PortalMechanics;
using SuperspectiveUtils;
using UnityEngine;

// TODO: Make saveable
namespace LevelSpecific.TransitionWhiteRoom_Fork {
    public class InfiniteHallwayRightPortal : MonoBehaviour {
        public MagicTrigger enterInfiniteHallwayTrigger;
        public Portal upperPlatformPortal;
        public Portal lowerPlatformPortal;
        public TeleportEnter teleportFacingForward;
        public TeleportEnter teleportFacingBackward;
        bool exitsAreConnected = false;
        bool playerIsInInfiniteHallway = false;

        private void Awake() {
            lowerPlatformPortal.pauseRenderingAndLogic = !exitsAreConnected;
        }

        void Start() {
            SubscribeToEvents();
        }

        void OnDestroy() {
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents() {
            enterInfiniteHallwayTrigger.OnMagicTriggerStayOneTime += EnterInfiniteHallway;
            enterInfiniteHallwayTrigger.OnNegativeMagicTriggerStayOneTime += ExitInfiniteHallway;
            
            upperPlatformPortal.OnPortalTeleportSimple += EnterInfiniteHallwayOnPortalEnter;
            lowerPlatformPortal.OnPortalTeleportSimple += ExitInfiniteHallwayOnPortalEnter;

            teleportFacingForward.OnTeleportSimple += ConnectExits;
            teleportFacingBackward.OnTeleportSimple += ConnectExits;
        }

        void UnsubscribeFromEvents() {
            enterInfiniteHallwayTrigger.OnMagicTriggerStayOneTime -= EnterInfiniteHallway;
            enterInfiniteHallwayTrigger.OnNegativeMagicTriggerStayOneTime -= ExitInfiniteHallway;
            
            upperPlatformPortal.OnPortalTeleportSimple -= EnterInfiniteHallwayOnPortalEnter;
            lowerPlatformPortal.OnPortalTeleportSimple -= ExitInfiniteHallwayOnPortalEnter;

            teleportFacingForward.OnTeleportSimple -= ConnectExits;
            teleportFacingBackward.OnTeleportSimple -= ConnectExits;
        }

        void Update() {
            if (exitsAreConnected && !playerIsInInfiniteHallway) {
                exitsAreConnected = false;
            }

            if (lowerPlatformPortal.pauseRenderingOnly && exitsAreConnected) {
                lowerPlatformPortal.pauseRenderingAndLogic = false;
                lowerPlatformPortal.PortalMaterial();
            }
            else if (!lowerPlatformPortal.pauseRenderingOnly && !exitsAreConnected) {
                lowerPlatformPortal.pauseRenderingAndLogic = true;
                lowerPlatformPortal.DefaultMaterial();
            }
            lowerPlatformPortal.pauseRenderingAndLogic = !exitsAreConnected;

            teleportFacingBackward.teleportEnter.enabled = !exitsAreConnected;
            teleportFacingForward.teleportEnter.enabled = !exitsAreConnected;
        }

        void ConnectExits() {
            exitsAreConnected = true;
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
            playerIsInInfiniteHallway = true;
        }

        void ExitInfiniteHallway() {
            playerIsInInfiniteHallway = false;
            exitsAreConnected = false;
        }
    }
}
