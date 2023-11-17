using System;

namespace DeveloperConsole {
    public class AutorunCommand : ConsoleCommand {
        public AutorunCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            PlayerMovement.instance.autoRun = !PlayerMovement.instance.autoRun;
            return new SuccessResponse();
        }
    }
}
