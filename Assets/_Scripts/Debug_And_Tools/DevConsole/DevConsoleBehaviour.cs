using System;
using System.Collections.Generic;
using Saving;
using SuperspectiveUtils;
using TMPro;
using UnityEngine;

namespace DeveloperConsole {
    public class DevConsoleBehaviour : Singleton<DevConsoleBehaviour> {
        public bool IsActive => uiCanvas.gameObject.activeSelf;
        
        private ConsoleCommand[] commands;

        [Header("UI")]
        public Canvas uiCanvas;

        [SerializeField]
        private TMP_InputField inputField;

        private TMP_Text _placeholderText;
        private TMP_Text PlaceholderText => _placeholderText ??= inputField.PlaceholderText();
        private string originalPlaceholderText;

        public float pausedTimeScale;
        
        private DeveloperConsole _developerConsole;
        private DeveloperConsole DeveloperConsole => _developerConsole ??= new DeveloperConsole(commands);

        private string lastAutoCompletedWord;
        private string lastPlayerInput;
        private int matchIndex = 0;

        private const float responseReadoutDelay = 1f;
        
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
                new TogglePortalRenderingCommand("togglePortals"),
                new ToggleDebugModeCommand("toggleDebug"),
                new ToggleUICommand("toggleUI"),
                new ChangeScaleCommand("changeScale"),
                new HideCullEverythingLayerCommand("hideCullMask"),
                new ShowInvisibleWallsCommand("showInvisibleWalls"),
                new ShowTriggersCommand("showTriggers"),
                new ShowVolumetricPortalsCommand("showVolumetricPortals"),
                new SetFarClipPlaneCommand("setFarClipPlane"),
                new SetTimeScaleCommand("setTimeScale"),
                new TogglePortalDebuggingCommand("togglePortalDebugging"),
                #if UNITY_EDITOR
                new ToggleAutosaveInEditorCommand("toggleAutosaveInEditor"),
                #endif
            };
            
            inputField.onSubmit.AddListener(ProcessCommand);
            inputField.onValueChanged.AddListener(RememberPlayerInput);
        }

        private void Start() {
            originalPlaceholderText = PlaceholderText.text;
        }

        public void Update() {
            if (Input.GetKeyDown(KeyCode.BackQuote)) {
                Toggle();
            }

            if (uiCanvas.gameObject.activeSelf) {
                if (Input.GetKeyDown(KeyCode.Tab)) {
                    lastAutoCompletedWord = DeveloperConsole.AutoCompleteCommand(lastPlayerInput, matchIndex);
                    inputField.text = lastAutoCompletedWord;
                    inputField.MoveToEndOfLine(false, false);
                    matchIndex++;
                }
            }
        }

        public void Toggle() {
            if (uiCanvas.gameObject.activeSelf) {
                Time.timeScale = pausedTimeScale;
                uiCanvas.gameObject.SetActive(false);
            }
            else {
                pausedTimeScale = Time.timeScale;
                Time.timeScale = 0;
                PlaceholderText.color = Color.white;
                PlaceholderText.text = originalPlaceholderText;
                uiCanvas.gameObject.SetActive(true);
                inputField.ActivateInputField();
            }
        }

        private void ToggleOff() {
            if (!uiCanvas.gameObject.activeSelf) return;
            Toggle();
        }

        public void ProcessCommand(string inputValue) {
            CommandResponse response = DeveloperConsole.ProcessCommand(inputValue);
            if (response) {
                this.InvokeRealtime(nameof(ToggleOff), responseReadoutDelay);
                PlaceholderText.color = Color.green;
                PlaceholderText.text = ((SuccessResponse)response).Message;
            }
            else {
                this.InvokeRealtime(nameof(ToggleOff), responseReadoutDelay);
                PlaceholderText.color = Color.red;
                PlaceholderText.text = ((FailureResponse)response).Reason;
            }

            inputField.text = string.Empty;
            lastAutoCompletedWord = string.Empty;
        }

        private void RememberPlayerInput(string inputText) {
            if (inputText == lastAutoCompletedWord) return;
            
            lastPlayerInput = inputText;
            matchIndex = 0;
        }
    }
}
