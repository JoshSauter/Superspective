using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using PortalMechanics;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;


namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorPuzzle : MonoBehaviour {
        [Serializable]
        public struct PowerSource {
            public bool negative;
            public SpriteRenderer valueIcon;
            public int value => (negative ? -1 : 1) * int.Parse(valueIcon.sprite.name);
            public PowerTrail powerTrail;
            public PowerButton powerButton;
        }

        public FloorManager.Floor floor;
        public Portal[] floorPortals;

        public CurrentValueDisplay currentValue;
        public PowerSource[] powerSources;
        private Dictionary<PowerTrail, PowerSource> powerSourceLookup;
        public PowerTrail powerTrailBottomMiddle;
        public PowerTrail powerTrailTopMiddle;
        public PortalDoor entranceDoor;
        public CurrentValueShutter currentValueShutter;

        private void Awake() {
            floorPortals = transform.GetComponentsInChildrenRecursively<Portal>().Where(p => p.gameObject.activeSelf).ToArray();
        }

        // Start is called before the first frame update
        void Start() {
            powerSourceLookup = powerSources.ToDictionary(ps => ps.powerTrail, ps => ps);

            InitShutter();
            InitEvents();
        }

        private void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;
            
            if (entranceDoor != null) {
                if ((int)FloorManager.instance.floor.state > (int)floor &&
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
                powerSource.powerTrail.OnPowerFinishRef += AddPower;
                powerSource.powerTrail.OnDepowerBeginRef += RemovePower;
            }

            SaveManager.BeforeLoad += StopAllCoroutines;
        }

        void TeardownEvents() {
            foreach (var powerSource in powerSources) {
                powerSource.powerTrail.OnPowerFinishRef -= AddPower;
                powerSource.powerTrail.OnDepowerBeginRef -= RemovePower;
            }

            SaveManager.BeforeLoad -= StopAllCoroutines;
        }
        #endregion

        IEnumerator AddPowerDelayed(PowerTrail powerTrail) {
            yield return new WaitForSeconds(powerTrailTopMiddle.duration);
            FloorManager.instance.currentValue += powerSourceLookup[powerTrail].value;
        }
        
        IEnumerator RemovePowerDelayed(PowerTrail powerTrail) {
            yield return new WaitForSeconds(powerTrailTopMiddle.duration);
            FloorManager.instance.currentValue -= powerSourceLookup[powerTrail].value;
        }

        void AddPower(PowerTrail powerTrail) {
            StartCoroutine(AddPowerDelayed(powerTrail));
        }
        
        void RemovePower(PowerTrail powerTrail) {
            StartCoroutine(RemovePowerDelayed(powerTrail));
        }
    }
}
