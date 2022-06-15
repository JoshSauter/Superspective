using System;
using UnityEngine;

namespace PowerTrailMechanics {
    public class TriggerPowerTrailFromButton : MonoBehaviour {
        public enum PowerControl {
            PowerOnAndOff,
            PowerOnOnly,
            PowerOffOnly
        }

        public PowerButton button;
        public PowerControl whatToControl;
        PowerTrail thisPowerTrail;

        void Start() {
            thisPowerTrail = GetComponent<PowerTrail>();
        }

        private void OnEnable() {
            thisPowerTrail = GetComponent<PowerTrail>();
            InitEvents();
        }

        private void OnDisable() {
            TeardownEvents();
        }

        void InitEvents() {
            if (whatToControl == PowerControl.PowerOnAndOff || whatToControl == PowerControl.PowerOnOnly) {
                button.OnPowerFinish += TurnPowerOn;
            }

            if (whatToControl == PowerControl.PowerOnAndOff || whatToControl == PowerControl.PowerOffOnly) {
                button.OnDepowerStart += TurnPowerOff;
            }
        }

        void TeardownEvents() {
            if (whatToControl == PowerControl.PowerOnAndOff || whatToControl == PowerControl.PowerOnOnly) {
                button.OnPowerFinish -= TurnPowerOn;
            }

            if (whatToControl == PowerControl.PowerOnAndOff || whatToControl == PowerControl.PowerOffOnly) {
                button.OnDepowerStart -= TurnPowerOff;
            }
        }

        void TurnPowerOn() {
            thisPowerTrail.powerIsOn = true;
        }

        void TurnPowerOff() {
            thisPowerTrail.powerIsOn = false;
        }
    }
}
