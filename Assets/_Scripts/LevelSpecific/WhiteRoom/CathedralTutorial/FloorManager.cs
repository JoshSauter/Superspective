using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PortalMechanics;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorManager : Singleton<FloorManager> {
        public enum Floor {
            Floor1,
            Floor2,
            Floor3
        }

        public StateMachine<Floor> floor = new StateMachine<Floor>(Floor.Floor1);

        [Serializable]
        struct PortalsOnAFloor {
            public Floor floor;
            public List<Portal> portals;
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
            if (portalFloors != null && floors == null || floors.Count == 0) {
                floors = portalFloors.ToDictionary(pf => pf.floor, pf => pf);
            }
            
            floor.AddTrigger((enumValue) => true, 0f, (enumValue) => {
                TurnOffAllPortals();
                TurnOnPortalsForFloor(enumValue);
            });
        }

        void TurnOffAllPortals() {
            foreach (var floor in floors.Values) {
                foreach (Portal floorPortal in floor.portals) {
                    floorPortal.pauseRenderingOnly = true;
                }
            }
        }

        void TurnOnPortalsForFloor(Floor floor) {
            foreach (Portal portal in floors[floor].portals) {
                portal.pauseRenderingOnly = false;
            }
        }

        public void SetFloorByName(String floorToSet) {
            if (Floor.TryParse(floorToSet, true, out Floor floor)) {
                SetFloor(floor);
            }
            else {
                Debug.LogError($"Can't parse {floorToSet}");
            }
        }

        private void SetFloor(Floor floorToSet) {
            this.floor.Set(floorToSet);
        }
    }
}