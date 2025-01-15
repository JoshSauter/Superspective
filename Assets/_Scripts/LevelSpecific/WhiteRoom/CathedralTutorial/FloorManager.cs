using System;
using System.Collections.Generic;
using System.Linq;
using PortalMechanics;
using Saving;
using StateUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorManager : SingletonSuperspectiveObject<FloorManager, FloorManager.FloorManagerSave> {

        public enum Floor : byte {
            None,
            Floor1,
            Floor2,
            Floor3,
            Center
        }

        public StateMachine<Floor> floor;

        [Serializable]
        struct PortalsOnAFloor {
            public Floor floor;
            public List<Portal> portals;
        }

        [SerializeField]
        FloorPuzzle[] puzzleFloors;
        public Dictionary<Floor, FloorPuzzle> floors;
        private int _currentValue = 0;

        public int CurrentValue {
            get => _currentValue;
            set {
                if (value != _currentValue) {
                    foreach (var pf in puzzleFloors) {
                        if (pf.floor == Floor.Center) continue;

                        pf.currentValue.actualValue = value;
                    }

                    _currentValue = value;
                }
            }
        }

        protected override void OnValidate() {
            base.OnValidate();
            
            if (puzzleFloors != null) {
                floors = puzzleFloors.ToDictionary(pf => pf.floor, pf => pf);
            }
        }

        protected override void Start() {
            base.Start();

            floor = this.StateMachine(Floor.None);
            
            if (puzzleFloors != null && (floors == null || floors.Count == 0)) {
                floors = puzzleFloors.ToDictionary(pf => pf.floor, pf => pf);
            }
            
            if (floor == Floor.None) {
                TurnOffAllPortals();
            }
            
            floor.AddTrigger((enumValue) => enumValue != Floor.None, () => {
                TurnOffAllPortals();
                TurnOnPortalsForFloor(floor.State);
            });
        }

        void TurnOffAllPortals() {
            foreach (var floor in floors.Values) {
                foreach (Portal floorPortal in floor.floorPortals) {
                    floorPortal.RenderMode = PortalRenderMode.Invisible;
                }
            }
        }

        void TurnOnPortalsForFloor(Floor floor) {
            foreach (Portal portal in floors[floor].floorPortals) {
                portal.RenderMode = PortalRenderMode.Normal;
            }
        }

        public void TurnOffAllPowerSources() {
            foreach (var kv in floors) {
                Floor floor = kv.Key;
                FloorPuzzle puzzle = kv.Value;
                
                if (floor == Floor.Center || floor == Floor.None) continue;
                
                foreach (var powerSource in puzzle.powerSources) {
                    powerSource.powerTrail.pwr.PowerIsOn = false;
                    if (powerSource.powerButton.pwr.PowerIsOn) {
                        powerSource.powerButton.PressButton();
                    }
                }
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

        public override void LoadSave(FloorManagerSave save) {
            CurrentValue = save.currentValue;
            floor.LoadFromSave(save.stateSave);
        }

        public override string ID => "CathedralTutorialFloorManager";

        [Serializable]
        public class FloorManagerSave : SaveObject<FloorManager> {
            public StateMachine<Floor>.StateMachineSave stateSave;
            public int currentValue;
            
            public FloorManagerSave(FloorManager script) : base(script) {
                this.currentValue = script.CurrentValue;
                this.stateSave = script.floor.ToSave();
            }
        }
    }
}
