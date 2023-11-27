using System.Collections.Generic;
using System.Linq;
using SuperspectiveUtils;
using UnityEngine;

namespace DeveloperConsole {
    public class DeveloperConsole {
        private readonly IEnumerable<IConsoleCommand> commands;
        private readonly Dictionary<string, IConsoleCommand> commandLookup;
        private Trie wordTree;
        
        public DeveloperConsole(IEnumerable<IConsoleCommand> commands) {
            // Avoid multiple enumerations of the commands
            List<IConsoleCommand> consoleCommands = commands.ToList();

            this.commands = consoleCommands;
            this.commandLookup = consoleCommands.ToDictionary(c => c.CommandWord);
            this.wordTree = new Trie(commandLookup.Keys);
        }
        
        public CommandResponse ProcessCommand(string commandInput) {
            Debug.Log($"Processing input for {commandInput}");
            
            string[] inputSplit = commandInput.Split(' ');
            string commandWord = inputSplit[0];
            string[] args = inputSplit.Skip(1).ToArray();
            
            return ProcessCommand(commandWord, args);
        }

        public CommandResponse ProcessCommand(string commandWord, string[] args) {
            if (!commandLookup.ContainsKey(commandWord)) return new FailureResponse($"No matching command word: '{commandWord}'");

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

        public string AutoCompleteCommand(string inputFieldText, int matchIndex) {
            return wordTree.AutoComplete(inputFieldText, matchIndex);
        }
    }
}
