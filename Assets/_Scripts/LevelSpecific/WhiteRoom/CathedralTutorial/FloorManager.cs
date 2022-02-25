using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PortalMechanics;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorManager : Singleton<FloorManager> {
        public enum Floor {
            Neg4,
            Neg3,
            Neg2,
            Neg1,
            Pos1,
            Pos2,
            Pos3,
            Pos4
        }

        public Floor floor;

        [Serializable]
        struct PortalsOnAFloor {
            public Floor floor;
            public Portal portal1, portal2, portal3, flipSignsPortal;
        }

        [SerializeField]
        PortalsOnAFloor[] portalFloors;
        private Dictionary<Floor, PortalsOnAFloor> floors;

        private void OnValidate() {
            if (portalFloors != null) {
                floors = portalFloors.ToDictionary(pf => pf.floor, pf => pf);
            }
        }

        public void Start() {
            RegisterTriggers();
        }

        private void OnDestroy() {
            UnregisterTriggers();
        }

        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorPos1 = () => instance.SetFloor(Floor.Pos1);
        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorPos2 = () => instance.SetFloor(Floor.Pos2);
        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorPos3 = () => instance.SetFloor(Floor.Pos3);
        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorPos4 = () => instance.SetFloor(Floor.Pos4);
        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorNeg1 = () => instance.SetFloor(Floor.Neg1);
        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorNeg2 = () => instance.SetFloor(Floor.Neg2);
        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorNeg3 = () => instance.SetFloor(Floor.Neg3);
        private readonly Portal.SimplePortalTeleportPlayerAction SetFloorNeg4 = () => instance.SetFloor(Floor.Neg4);
        
        // I'd rather not talk about it
        void RegisterTriggers() {
            // Pos1
            floors[Floor.Pos1].portal1.BeforePortalTeleportPlayerSimple += SetFloorPos2;
            floors[Floor.Pos1].portal2.BeforePortalTeleportPlayerSimple += SetFloorPos3;
            floors[Floor.Pos1].portal3.BeforePortalTeleportPlayerSimple += SetFloorPos4;
            floors[Floor.Pos1].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorNeg1;
            
            // Pos2
            floors[Floor.Pos2].portal1.BeforePortalTeleportPlayerSimple += SetFloorPos1;
            floors[Floor.Pos2].portal2.BeforePortalTeleportPlayerSimple += SetFloorPos3;
            floors[Floor.Pos2].portal3.BeforePortalTeleportPlayerSimple += SetFloorPos4;
            floors[Floor.Pos2].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorNeg2;
            
            // Pos3
            floors[Floor.Pos3].portal1.BeforePortalTeleportPlayerSimple += SetFloorPos1;
            floors[Floor.Pos3].portal2.BeforePortalTeleportPlayerSimple += SetFloorPos2;
            floors[Floor.Pos3].portal3.BeforePortalTeleportPlayerSimple += SetFloorPos4;
            floors[Floor.Pos3].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorNeg3;
            
            // Pos4
            floors[Floor.Pos4].portal1.BeforePortalTeleportPlayerSimple += SetFloorPos1;
            floors[Floor.Pos4].portal2.BeforePortalTeleportPlayerSimple += SetFloorPos2;
            floors[Floor.Pos4].portal3.BeforePortalTeleportPlayerSimple += SetFloorPos3;
            floors[Floor.Pos4].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorNeg4;
            
            // Neg1
            floors[Floor.Neg1].portal1.BeforePortalTeleportPlayerSimple += SetFloorNeg2;
            floors[Floor.Neg1].portal2.BeforePortalTeleportPlayerSimple += SetFloorNeg3;
            floors[Floor.Neg1].portal3.BeforePortalTeleportPlayerSimple += SetFloorNeg4;
            floors[Floor.Neg1].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorPos1;
            
            // Neg2
            floors[Floor.Neg2].portal1.BeforePortalTeleportPlayerSimple += SetFloorNeg1;
            floors[Floor.Neg2].portal2.BeforePortalTeleportPlayerSimple += SetFloorNeg3;
            floors[Floor.Neg2].portal3.BeforePortalTeleportPlayerSimple += SetFloorNeg4;
            floors[Floor.Neg2].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorPos2;
            
            // Neg3
            floors[Floor.Neg3].portal1.BeforePortalTeleportPlayerSimple += SetFloorNeg1;
            floors[Floor.Neg3].portal2.BeforePortalTeleportPlayerSimple += SetFloorNeg2;
            floors[Floor.Neg3].portal3.BeforePortalTeleportPlayerSimple += SetFloorNeg4;
            floors[Floor.Neg3].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorPos3;
            
            // Neg4
            floors[Floor.Neg4].portal1.BeforePortalTeleportPlayerSimple += SetFloorNeg1;
            floors[Floor.Neg4].portal2.BeforePortalTeleportPlayerSimple += SetFloorNeg2;
            floors[Floor.Neg4].portal3.BeforePortalTeleportPlayerSimple += SetFloorNeg3;
            floors[Floor.Neg4].flipSignsPortal.BeforePortalTeleportPlayerSimple += SetFloorPos4;
        }

        void UnregisterTriggers() {
            // Pos1
            floors[Floor.Pos1].portal1.BeforePortalTeleportPlayerSimple -= SetFloorPos2;
            floors[Floor.Pos1].portal2.BeforePortalTeleportPlayerSimple -= SetFloorPos3;
            floors[Floor.Pos1].portal3.BeforePortalTeleportPlayerSimple -= SetFloorPos4;
            floors[Floor.Pos1].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorNeg1;
            
            // Pos2
            floors[Floor.Pos2].portal1.BeforePortalTeleportPlayerSimple -= SetFloorPos1;
            floors[Floor.Pos2].portal2.BeforePortalTeleportPlayerSimple -= SetFloorPos3;
            floors[Floor.Pos2].portal3.BeforePortalTeleportPlayerSimple -= SetFloorPos4;
            floors[Floor.Pos2].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorNeg2;
            
            // Pos3
            floors[Floor.Pos3].portal1.BeforePortalTeleportPlayerSimple -= SetFloorPos1;
            floors[Floor.Pos3].portal2.BeforePortalTeleportPlayerSimple -= SetFloorPos2;
            floors[Floor.Pos3].portal3.BeforePortalTeleportPlayerSimple -= SetFloorPos4;
            floors[Floor.Pos3].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorNeg3;
            
            // Pos4
            floors[Floor.Pos4].portal1.BeforePortalTeleportPlayerSimple -= SetFloorPos1;
            floors[Floor.Pos4].portal2.BeforePortalTeleportPlayerSimple -= SetFloorPos2;
            floors[Floor.Pos4].portal3.BeforePortalTeleportPlayerSimple -= SetFloorPos3;
            floors[Floor.Pos4].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorNeg4;
            
            // Neg1
            floors[Floor.Neg1].portal1.BeforePortalTeleportPlayerSimple -= SetFloorNeg2;
            floors[Floor.Neg1].portal2.BeforePortalTeleportPlayerSimple -= SetFloorNeg3;
            floors[Floor.Neg1].portal3.BeforePortalTeleportPlayerSimple -= SetFloorNeg4;
            floors[Floor.Neg1].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorPos1;
            
            // Neg2
            floors[Floor.Neg2].portal1.BeforePortalTeleportPlayerSimple -= SetFloorNeg1;
            floors[Floor.Neg2].portal2.BeforePortalTeleportPlayerSimple -= SetFloorNeg3;
            floors[Floor.Neg2].portal3.BeforePortalTeleportPlayerSimple -= SetFloorNeg4;
            floors[Floor.Neg2].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorPos2;
            
            // Neg3
            floors[Floor.Neg3].portal1.BeforePortalTeleportPlayerSimple -= SetFloorNeg1;
            floors[Floor.Neg3].portal2.BeforePortalTeleportPlayerSimple -= SetFloorNeg2;
            floors[Floor.Neg3].portal3.BeforePortalTeleportPlayerSimple -= SetFloorNeg4;
            floors[Floor.Neg3].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorPos3;
            
            // Neg4
            floors[Floor.Neg4].portal1.BeforePortalTeleportPlayerSimple -= SetFloorNeg1;
            floors[Floor.Neg4].portal2.BeforePortalTeleportPlayerSimple -= SetFloorNeg2;
            floors[Floor.Neg4].portal3.BeforePortalTeleportPlayerSimple -= SetFloorNeg3;
            floors[Floor.Neg4].flipSignsPortal.BeforePortalTeleportPlayerSimple -= SetFloorPos4;
        }

        void SetFloor(Floor floorToSet) {
            this.floor = floorToSet;
        }
    }
}