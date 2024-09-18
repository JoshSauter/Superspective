using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Interactables;
using NaughtyAttributes;
using PortalMechanics;
using PoweredObjects;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;


namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorPuzzle : MonoBehaviour {
        [Serializable]
        public struct PuzzlePowerSource {
            public bool negative;
            public SpriteRenderer valueIcon;
            public int value => (negative ? -1 : 1) * int.Parse(valueIcon.sprite.name);
            public PowerTrail powerTrail;
            public Button powerButton;
        }

        public FloorManager.Floor floor;
        public Portal[] floorPortals;

        public CurrentValueDisplay currentValue;
        public PuzzlePowerSource[] powerSources;
        private Dictionary<PoweredObject, PuzzlePowerSource> powerSourceLookup;
        public PowerTrail powerTrailBottomMiddle;
        public PowerTrail powerTrailTopMiddle;
        public PortalDoor entranceDoor;
        public CurrentValueShutter currentValueShutter;

        private void Awake() {
            floorPortals = transform.GetComponentsInChildrenRecursively<Portal>().Where(p => p.gameObject.activeSelf).ToArray();
        }

        // Start is called before the first frame update
        void Start() {
            powerSourceLookup = powerSources.ToDictionary(ps => ps.powerTrail.pwr, ps => ps);

            InitShutter();
            InitEvents();
        }

        private void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;
            
            if (entranceDoor != null) {
                if ((int)FloorManager.instance.floor.State > (int)floor &&
                    entranceDoor.state == PortalDoor.DoorState.Closed) {
                    entranceDoor.TriggerDoors();
                }
            }
        }

        private void OnDisable() {
            TeardownEvents();
        }

        void InitShutter() {
            switch (floor) {
                case FloorManager.Floor.None:
                    break;
                case FloorManager.Floor.Floor1:
                    currentValueShutter.state.Set(CurrentValueShutter.State.Open);
                    break;
                case FloorManager.Floor.Floor2:
                    break;
                case FloorManager.Floor.Floor3:
                    currentValueShutter.state.Set(CurrentValueShutter.State.Shut);
                    break;
                case FloorManager.Floor.Center:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Events
        void InitEvents() {
            foreach (var powerSource in powerSources) {
                powerSource.powerTrail.pwr.OnPowerFinishRef += AddPower;
                powerSource.powerTrail.pwr.OnDepowerBeginRef += RemovePower;
            }

            SaveManager.BeforeLoad += StopAllCoroutines;
        }

        void TeardownEvents() {
            foreach (var powerSource in powerSources) {
                powerSource.powerTrail.pwr.OnPowerFinishRef -= AddPower;
                powerSource.powerTrail.pwr.OnDepowerBeginRef -= RemovePower;
            }

            SaveManager.BeforeLoad -= StopAllCoroutines;
        }
        #endregion

        IEnumerator AddPowerDelayed(PoweredObject powerTrail) {
            yield return new WaitForSeconds(powerTrailTopMiddle.duration);
            FloorManager.instance.currentValue += powerSourceLookup[powerTrail].value;
        }
        
        IEnumerator RemovePowerDelayed(PoweredObject powerTrail) {
            yield return new WaitForSeconds(powerTrailTopMiddle.duration);
            FloorManager.instance.currentValue -= powerSourceLookup[powerTrail].value;
        }

        void AddPower(PoweredObject powerTrail) {
            StartCoroutine(AddPowerDelayed(powerTrail));
        }
        
        void RemovePower(PoweredObject powerTrail) {
            StartCoroutine(RemovePowerDelayed(powerTrail));
        }
    }
}
