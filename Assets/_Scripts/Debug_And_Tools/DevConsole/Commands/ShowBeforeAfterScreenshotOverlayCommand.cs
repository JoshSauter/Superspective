using UnityEngine;

namespace DeveloperConsole {
    public class ShowBeforeAfterScreenshotOverlayCommand : ConsoleCommand {
        private enum OverlayMode {
            Off,
            Before,
            After
        }
        private OverlayMode currentMode = OverlayMode.Off;
        
        public GameObject before;
        public GameObject after;
        
        public ShowBeforeAfterScreenshotOverlayCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            if (before == null) {
                before = MainCanvas.instance.transform.Find("ForScreenshots").Find("Before").gameObject;
            }
            if (after == null) {
                after = MainCanvas.instance.transform.Find("ForScreenshots").Find("After").gameObject;
            }
            
            currentMode = (OverlayMode)(((int)currentMode + 1) % 3);
            switch (currentMode) {
                case OverlayMode.Off:
                    before.SetActive(false);
                    after.SetActive(false);
                    return new SuccessResponse("Screenshot overlay turned off.");
                case OverlayMode.Before:
                    before.SetActive(true);
                    after.SetActive(false);
                    return new SuccessResponse("Showing 'Before' screenshot overlay.");
                case OverlayMode.After:
                    before.SetActive(false);
                    after.SetActive(true);
                    return new SuccessResponse("Showing 'After' screenshot overlay.");
                default:
                    return new FailureResponse("Unknown overlay mode.");
            }
        }
    }
}
