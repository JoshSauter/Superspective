using System;
using System.Collections.Generic;
using Saving;
using TMPro;
using UnityEngine;

namespace DeveloperConsole {
    public class DevConsoleBehaviour : Singleton<DevConsoleBehaviour> {
        public bool IsActive => uiCanvas.activeSelf;
        
        private ConsoleCommand[] commands;

        [Header("UI")]
        [SerializeField]
        private GameObject uiCanvas;

        [SerializeField]
        private TMP_InputField inputField;

        private float pausedTimeScale;
        
        private DeveloperConsole _developerConsole;
        private DeveloperConsole DeveloperConsole => _developerConsole ??= new DeveloperConsole(commands);
        
        // Command-specific config
        public DynamicObject cubePrefab;
        public DynamicObject multiDimensionCubePrefab;
        public DynamicObject cubeRedPrefab;
        public DynamicObject cubeGreenPrefab;
        public DynamicObject cubeBluePrefab;

        private void Awake() {
            Dictionary<SpawnCommand.DynamicObjectType, DynamicObject> spawnableObjects = new Dictionary<SpawnCommand.DynamicObjectType, DynamicObject>() {
                { SpawnCommand.DynamicObjectType.Cube, cubePrefab },
                { SpawnCommand.DynamicObjectType.MultiDimensionCube, multiDimensionCubePrefab },
                { SpawnCommand.DynamicObjectType.CubeRed, cubeRedPrefab },
                { SpawnCommand.DynamicObjectType.CubeGreen, cubeGreenPrefab },
                { SpawnCommand.DynamicObjectType.CubeBlue, cubeBluePrefab },
            };
            commands = new ConsoleCommand[] {
                new LoadLevelCommand("loadLevel"),
                new SpawnCommand("spawn", spawnableObjects),
                new AutorunCommand("autorun"),
            };
            
            inputField.onSubmit.AddListener(ProcessCommand);
        }

        public void Update() {
            if (Input.GetKeyDown(KeyCode.BackQuote)) {
                Toggle();
            }
        }

        public void Toggle() {
            if (uiCanvas.activeSelf) {
                Time.timeScale = pausedTimeScale;
                uiCanvas.SetActive(false);
            }
            else {
                pausedTimeScale = Time.timeScale;
                Time.timeScale = 0;
                uiCanvas.SetActive(true);
                inputField.ActivateInputField();
            }
        }

        public void ProcessCommand(string inputValue) {
            if (DeveloperConsole.ProcessCommand(inputValue)) {
                Toggle();
            }

            inputField.text = string.Empty;
        }
    }
}
