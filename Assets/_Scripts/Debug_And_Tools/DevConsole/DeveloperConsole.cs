using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeveloperConsole {
    public class DeveloperConsole {
        private readonly IEnumerable<IConsoleCommand> commands;
        private readonly Dictionary<string, IConsoleCommand> commandLookup;
        
        public DeveloperConsole(IEnumerable<IConsoleCommand> commands) {
            // Avoid multiple enumerations of the commands
            List<IConsoleCommand> consoleCommands = commands.ToList();

            this.commands = consoleCommands;
            this.commandLookup = consoleCommands.ToDictionary(c => c.CommandWord);
        }
        
        public bool ProcessCommand(string commandInput) {
            Debug.Log($"Processing input for {commandInput}");
            
            string[] inputSplit = commandInput.Split(' ');
            string commandWord = inputSplit[0];
            string[] args = inputSplit.Skip(1).ToArray();
            
            return ProcessCommand(commandWord, args);
        }

        public bool ProcessCommand(string commandWord, string[] args) {
            if (!commandLookup.ContainsKey(commandWord)) return false;

            IConsoleCommand command = commandLookup[commandWord];
            CommandResponse result = command.Execute(args);
            if (result) {
                Debug.Log($"Executed command: {commandWord}");
            }
            else {
                Debug.LogError($"Failed to execute command: {commandWord}, reason: {((FailureResponse)result).Reason}");
            }

            return result;
        }
    }
}
