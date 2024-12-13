using System;
using Saving;

namespace DeveloperConsole {
    public class ToggleAutosaveInEditorCommand : ConsoleCommand {
        public ToggleAutosaveInEditorCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            // Parse the input and run the command here
            #if UNITY_EDITOR
            bool autosavesEnabledInEditor = AutosaveManager.instance.ToggleCanMakeAutosaveInEditor();
            return new SuccessResponse($"Autosaves {(autosavesEnabledInEditor ? "enabled" : "disabled")} for editor");
            #endif
            return new FailureResponse("This command is only available in the editor");
        }
    }
}
