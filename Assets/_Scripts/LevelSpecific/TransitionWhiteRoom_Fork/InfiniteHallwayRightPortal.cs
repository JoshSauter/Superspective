using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
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
        public bool hasBeenSolved = false;
        public AnimationCurve wallsShiftingCameraShakeAnimCurve;
        public GlobalMagicTrigger inCirclesText;

        private void Awake() {
            lowerPlatformPortal.pauseRendering = !exitsAreConnected;
            lowerPlatformPortal.pauseLogic = !exitsAreConnected;
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

            if (lowerPlatformPortal.pauseRendering && exitsAreConnected) {
                lowerPlatformPortal.pauseRendering = false;
                lowerPlatformPortal.pauseLogic = false;
            }
            else if (!lowerPlatformPortal.pauseRendering && !exitsAreConnected) {
                lowerPlatformPortal.pauseRendering = true;
                lowerPlatformPortal.pauseLogic = true;
            }
            lowerPlatformPortal.SetMaterialsToEffectiveMaterial();
            lowerPlatformPortal.pauseRendering = !exitsAreConnected;
            lowerPlatformPortal.pauseLogic = !exitsAreConnected;

            teleportFacingBackward.teleportEnter.enabled = !exitsAreConnected;
            teleportFacingForward.teleportEnter.enabled = !exitsAreConnected;
        }

        // Called in a UnityEvent from the puzzle solved trigger zones
        public void MarkAsSolved() {
            hasBeenSolved = true;
        }

        void ConnectExits() {
            if (!exitsAreConnected && !hasBeenSolved) {
                CameraShake.instance.Shake(1.5f, 3f, wallsShiftingCameraShakeAnimCurve);
                AudioManager.instance.PlayAtLocation(AudioName.WallsShifting, "InfiniteHallwayRight", teleportFacingForward.transform.position);
                inCirclesText.gameObject.SetActive(true);
                inCirclesText.enabled = true;
            }
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
