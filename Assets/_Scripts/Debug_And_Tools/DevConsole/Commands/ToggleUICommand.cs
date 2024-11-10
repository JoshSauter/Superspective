using System;
using System.Linq;
using LevelManagement;
using NovaMenuUI;
using UnityEngine;

namespace DeveloperConsole {
    public class ToggleUICommand : ConsoleCommand {
        private static bool simpleFPSWasEnabled = false;
        
        private static bool uiEnabled = true;
        private static bool UIEnabled {
            get => uiEnabled;
            set {
                // Parse the input and run the command here
                var canvasObjects = GameObject.FindObjectsOfType<Canvas>().Except(new[] { DevConsoleBehaviour.instance.uiCanvas }).ToList();
                var novaCamera = NovaInputManager.instance.UICamera;
                
                canvasObjects.ForEach(c => c.enabled = value);
                novaCamera.enabled = value;
                simpleFPSWasEnabled = SimpleFPS.instance.enabled;
                SimpleFPS.instance.enabled = value && simpleFPSWasEnabled;

                uiEnabled = value;
            }
        }
        public ToggleUICommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            try {
                UIEnabled = !UIEnabled;
                return new SuccessResponse(UIEnabled ? "UI Enabled" : "UI Disabled");
            }
            catch (Exception e) {
                return new FailureResponse(e.ToString());
            }
        }
    }
}
