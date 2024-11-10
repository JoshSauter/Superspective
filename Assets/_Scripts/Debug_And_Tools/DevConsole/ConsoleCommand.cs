using System;
using UnityEngine;

namespace DeveloperConsole {
    public interface IConsoleCommand {
        string CommandWord { get; }
        CommandResponse Execute(string[] args);
    }

    [Serializable]
    public abstract class ConsoleCommand : IConsoleCommand {
        public ConsoleCommand(string commandWord) {
            this.commandWord = commandWord;
        }
        
        private string commandWord;

        public string CommandWord => commandWord;
        public abstract CommandResponse Execute(string[] args);

        protected bool TryParseBool(string arg, out bool value) {
            if (arg.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                value = true;
                return true;
            }
            if (arg.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                value = false;
                return true;
            }

            value = default;
            return false;
        }
    }
}
