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
    public class PortalToBlackHallway : SuperspectiveObject<PortalToBlackHallway, PortalToBlackHallway.PortalToBlackHallwaySave> {
        Portal portal;
        bool poweredNow = false;
        public PowerTrail powerTrail;
        public SuperspectiveReference<MagicTrigger, MagicTrigger.MagicTriggerSave>[] blackHallwayLoopTeleporters;

        protected override void Awake() {
            base.Awake();
            portal = GetComponent<Portal>();
        }

        protected override void Init() {
            base.Init();
            StartCoroutine(Initialize());
        }

        IEnumerator Initialize() {
            yield return new WaitUntil(() => portal.otherPortal != null);
            portal.changeCameraEdgeDetection = powerTrail.IsFullyPowered;
            portal.otherPortal.changeCameraEdgeDetection = powerTrail.IsFullyPowered;

            HandlePowerTrail(powerTrail.IsFullyPowered);
        }

        void Update() {
            if (!hasInitialized) return;

            if (powerTrail.IsFullyPowered != poweredNow) {
                poweredNow = powerTrail.IsFullyPowered;
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

        public override void LoadSave(PortalToBlackHallwaySave save) {
            poweredNow = save.poweredNow;
        }

        public override string ID => "PortalToBlackHallway";

        public override bool SkipSave => true;

        [Serializable]
        public class PortalToBlackHallwaySave : SaveObject<PortalToBlackHallway> {
            public bool poweredNow;

            public PortalToBlackHallwaySave(PortalToBlackHallway script) : base(script) {
                this.poweredNow = script.poweredNow;
            }
        }
#endregion
    }
}
