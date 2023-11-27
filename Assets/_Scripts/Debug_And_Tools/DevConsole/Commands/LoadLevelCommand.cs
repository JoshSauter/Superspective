using System;
using LevelManagement;

namespace DeveloperConsole {
    public class LoadLevelCommand : ConsoleCommand {
        public LoadLevelCommand(string commandWord) : base(commandWord) { }

        public override CommandResponse Execute(string[] args) {
            if (args.Length < 1) {
                return LoadLevel(LevelManager.instance.ActiveScene);
            }
            
            string levelArg = args[0];
            if (int.TryParse(levelArg, out int levelIndex)) {
                if (!Enum.IsDefined(typeof(Levels), levelIndex)) {
                    return new FailureResponse($"Argument {levelIndex} is not a valid level index.");
                }
                
                if (args.Length > 1 && bool.TryParse(args[1], out bool resetPlayerPosition)) {
                    return LoadLevel((Levels)levelIndex, resetPlayerPosition);
                }
                else {
                    return LoadLevel((Levels)levelIndex);
                }
            }
            else if (Enum.TryParse(levelArg, true, out Levels level)) {
                if (args.Length > 1 && bool.TryParse(args[1], out bool resetPlayerPosition)) {
                    return LoadLevel((Levels)levelIndex, resetPlayerPosition);
                }
                else {
                    return LoadLevel(level);
                }
            }
            else if (LevelManager.enumToSceneName.ContainsValue(levelArg)) {
                return LoadLevel(LevelManager.enumToSceneName[levelArg]);
            }
            else {
                return new FailureResponse($"Argument {levelArg} is not a valid level index or level name.");
            }
        }

        private CommandResponse LoadLevel(Levels level, bool resetPlayerPosition = true) {
            if (level.IsTestingLevel()) return new FailureResponse($"Cannot load testing level {level} in build.");
            
            DevConsoleBehaviour.instance.Toggle();
            LevelManager.instance.SwitchActiveScene(
                level,
                true,
                true,
                true,
                false,
                resetPlayerPosition ? () => LevelManager.instance.LoadDefaultPlayerPosition(level) : null
            );

            return new SuccessResponse($"Loaded level {level.ToName()} ({level.ToDisplayName()})");
        }
    }
}
