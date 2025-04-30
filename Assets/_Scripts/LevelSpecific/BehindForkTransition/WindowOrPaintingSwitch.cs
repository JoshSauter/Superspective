using PowerTrailMechanics;
using Saving;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class WindowOrPaintingSwitch : SuperspectiveObject<WindowOrPaintingSwitch, WindowOrPaintingSwitch.WindowOrPaintingSwitchSave> {
        public GameObject window;
        public GameObject painting;
        public PowerTrail powerTrail;

        bool paintingActive = false;

        void Update() {
            if (paintingActive != powerTrail.IsFullyPowered) {
                paintingActive = powerTrail.IsFullyPowered;
                window.SetActive(!paintingActive);
                painting.SetActive(paintingActive);
            }
        }
        
        public override void LoadSave(WindowOrPaintingSwitchSave save) {
            window.SetActive(!paintingActive);
            painting.SetActive(paintingActive);
        }
        
        [System.Serializable]
        public class WindowOrPaintingSwitchSave : SaveObject<WindowOrPaintingSwitch> {
            public WindowOrPaintingSwitchSave(WindowOrPaintingSwitch script) : base(script) { }
        }
    }
}
