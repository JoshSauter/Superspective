using PortalMechanics;
using PowerTrailMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using MagicTriggerMechanics;
using SerializableClasses;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class PortalToBlackHallway : SaveableObject<PortalToBlackHallway, PortalToBlackHallway.PortalToBlackHallwaySave> {
        Portal portal;
        bool poweredNow = false;
        public PowerTrail powerTrail;
        public SerializableReference<MagicTrigger, MagicTrigger.MagicTriggerSave>[] blackHallwayLoopTeleporters;

        protected override void Awake() {
            base.Awake();
            portal = GetComponent<Portal>();
        }

        protected override void Init() {
            StartCoroutine(Initialize());
        }

        IEnumerator Initialize() {
            yield return new WaitUntil(() => portal.otherPortal != null);
            portal.changeCameraEdgeDetection = powerTrail.fullyPowered;
            portal.otherPortal.changeCameraEdgeDetection = powerTrail.fullyPowered;

            HandlePowerTrail(powerTrail.fullyPowered);
        }

        void Update() {
            if (!hasInitialized) return;

            if (powerTrail.fullyPowered != poweredNow) {
                poweredNow = powerTrail.fullyPowered;
                HandlePowerTrail(poweredNow);
            }
        }

        void HandlePowerTrail(bool poweredNow) {
            SetEdgeColors(poweredNow);
            portal.changeActiveSceneOnTeleport = poweredNow;

            foreach (var teleporter in blackHallwayLoopTeleporters) {
                teleporter.GetOrNull()?.gameObject.SetActive(!poweredNow);
            }
        }

        void SetEdgeColors(bool on) {
            if (portal.otherPortal != null) {
                portal.changeCameraEdgeDetection = on;
                portal.otherPortal.changeCameraEdgeDetection = on;
            }
        }

#region Saving
        public override string ID => "PortalToBlackHallway";

        public override bool SkipSave => true;

        [Serializable]
        public class PortalToBlackHallwaySave : SerializableSaveObject<PortalToBlackHallway> {
            readonly bool poweredNow;

            public PortalToBlackHallwaySave(PortalToBlackHallway script) : base(script) {
                this.poweredNow = script.poweredNow;
            }

            public override void LoadSave(PortalToBlackHallway script) {
                script.poweredNow = this.poweredNow;
                script.HandlePowerTrail(script.poweredNow);
            }
        }
#endregion
    }
}