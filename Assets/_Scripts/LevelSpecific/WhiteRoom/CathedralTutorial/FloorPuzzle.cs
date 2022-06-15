using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerTrailMechanics;
using StateUtils;
using UnityEngine;


namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorPuzzle : MonoBehaviour {
        [Serializable]
        public struct PowerSource {
            public int value;
            public PowerTrail powerTrail;
            public PowerButton powerButton;
        }

        public CurrentValueDisplay currentValue;
        public PowerSource[] powerSources;
        private Dictionary<PowerTrail, PowerSource> powerSourceLookup;
        public PuzzlePanel[] puzzlePanels;
        public PowerTrail powerTrailBottomMiddle;
        public PowerTrail powerTrailTopMiddle;

        // Start is called before the first frame update
        void Start() {
            powerSourceLookup = powerSources.ToDictionary(ps => ps.powerTrail, ps => ps);

            InitEvents();
        }

        private void OnDisable() {
            TeardownEvents();
        }

        #region Events
        void InitEvents() {
            foreach (var powerSource in powerSources) {
                powerSource.powerTrail.OnPowerFinishRef += AddPower;
                powerSource.powerTrail.OnDepowerBeginRef += RemovePower;
            }
        }

        void TeardownEvents() {
            foreach (var powerSource in powerSources) {
                powerSource.powerTrail.OnPowerFinishRef -= AddPower;
                powerSource.powerTrail.OnDepowerBeginRef -= RemovePower;
            }
        }
        #endregion
        
        IEnumerator AddPowerDelayed(PowerTrail powerTrail) {
            yield return new WaitForSeconds(powerTrailTopMiddle.duration);
            currentValue.actualValue += powerSourceLookup[powerTrail].value;
        }
        
        IEnumerator RemovePowerDelayed(PowerTrail powerTrail) {
            yield return new WaitForSeconds(powerTrailTopMiddle.duration);
            currentValue.actualValue -= powerSourceLookup[powerTrail].value;
        }

        void AddPower(PowerTrail powerTrail) {
            StartCoroutine(AddPowerDelayed(powerTrail));
        }
        
        void RemovePower(PowerTrail powerTrail) {
            StartCoroutine(RemovePowerDelayed(powerTrail));
        }
    }
}
