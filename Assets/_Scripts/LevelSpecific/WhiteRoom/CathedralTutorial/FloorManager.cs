using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PortalMechanics;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorManager : SingletonSaveableObject<FloorManager, FloorManager.FloorManagerSave> {

        public enum Floor {
            None,
            Floor1,
            Floor2,
            Floor3,
            Center
        }

        public StateMachine<Floor> floor = new StateMachine<Floor>(Floor.None);

        [Serializable]
        struct PortalsOnAFloor {
            public Floor floor;
            public List<Portal> portals;
        }

        [SerializeField]
        FloorPuzzle[] puzzleFloors;
        public Dictionary<Floor, FloorPuzzle> floors;
        private int _currentValue = 0;

        public int currentValue {
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

        private void OnValidate() {
            if (puzzleFloors != null) {
                floors = puzzleFloors.ToDictionary(pf => pf.floor, pf => pf);
            }
        }

        protected override void Start() {
            base.Start();
            
            if (puzzleFloors != null && (floors == null || floors.Count == 0)) {
                floors = puzzleFloors.ToDictionary(pf => pf.floor, pf => pf);
            }
            
            if (floor == Floor.None) {
                TurnOffAllPortals();
            }
            
            floor.AddTrigger((enumValue) => enumValue != Floor.None, 0f, (enumValue) => {
                TurnOffAllPortals();
                TurnOnPortalsForFloor(enumValue);
            });
        }

        void TurnOffAllPortals() {
            foreach (var floor in floors.Values) {
                foreach (Portal floorPortal in floor.floorPortals) {
                    floorPortal.pauseRendering = true;
                }
            }
        }

        void TurnOnPortalsForFloor(Floor floor) {
            foreach (Portal portal in floors[floor].floorPortals) {
                portal.pauseRendering = false;
            }
        }

        public void TurnOffAllPowerSources() {
            foreach (var kv in floors) {
                Floor floor = kv.Key;
                FloorPuzzle puzzle = kv.Value;
                
                if (floor == Floor.Center || floor == Floor.None) continue;
                
                foreach (var powerSource in puzzle.powerSources) {
                    powerSource.powerTrail.powerIsOn = false;
                    if (powerSource.powerButton.powerIsOn) {
                        powerSource.powerButton.button.PressButton();
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

        public override string ID => "CathedralTutorialFloorManager";

        [Serializable]
        public class FloorManagerSave : SerializableSaveObject<FloorManager> {
            private StateMachine<Floor>.StateMachineSave stateSave;
            private int currentValue;
            public FloorManagerSave(FloorManager script) : base(script) {
                this.currentValue = script.currentValue;
                this.stateSave = script.floor.ToSave();
            }
            public override void LoadSave(FloorManager script) {
                script.currentValue = this.currentValue;
                script.floor.LoadFromSave(this.stateSave);
            }
        }
    }
}