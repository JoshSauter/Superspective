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
    }
}
