using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SerializableClasses;
using UnityEngine;

namespace LevelManagement {
    // Default player positions are stored in a JSON file, and consist of position, rotation, and camera edge detection settings
    [Serializable]
    public class DefaultPlayerSettings {
        public const string FILE_PATH = "Assets/Resources/DefaultLevelSettings.json";
        public List<DefaultPlayerSettingsForScene> playerSettings;

        [NonSerialized]
        private Dictionary<Levels, DefaultPlayerSettingsForScene> _playerSettingsByLevel;
        public Dictionary<Levels, DefaultPlayerSettingsForScene> playerSettingsByLevel {
            get {
                if (_playerSettingsByLevel == null) {
                    _playerSettingsByLevel = playerSettings.ToDictionary(s => s.level);
                }

                return _playerSettingsByLevel;
            }
        }
        
        private DefaultPlayerSettings() { }

        public void SetDefaultPlayerPositionForScene(Levels level) {
            Camera mainCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            BladeEdgeDetection edgeDetection = (mainCam == null) ? null : mainCam.GetComponent<BladeEdgeDetection>();
            DefaultPlayerSettingsForScene curSettings = new DefaultPlayerSettingsForScene() {
                level = level,
                position = Player.instance.transform.position,
                rotation = Player.instance.transform.rotation.eulerAngles,
                edgeDetectionSettings = edgeDetection != null ? new EDSettings(edgeDetection) : null
            };

            int index = playerSettings.FindIndex(s => s.level == level);
            if (index >= 0) {
                playerSettings.RemoveAt(index);
            }
            playerSettings.Add(curSettings);

            playerSettingsByLevel[level] = curSettings;
            
            SaveToDisk();
        }
        
        private void SaveToDisk() {
            File.WriteAllText(FILE_PATH, JsonUtility.ToJson(this, true));
            Debug.Log($"Wrote default player settings to {FILE_PATH}");
        }
        
        public static DefaultPlayerSettings LoadFromDisk() {
            string json = File.ReadAllText(FILE_PATH);
            DefaultPlayerSettings defaultSettings = JsonUtility.FromJson<DefaultPlayerSettings>(json);
            defaultSettings._playerSettingsByLevel = defaultSettings.playerSettings.ToDictionary(s => s.level);
            return defaultSettings;
        }
    }

    [Serializable]
    public class DefaultPlayerSettingsForScene {
        public Levels level;
        public Vector3 position;
        public Vector3 rotation;
        public EDSettings edgeDetectionSettings;

        public void Apply() {
            Player.instance.transform.position = position;
            Player.instance.transform.rotation = Quaternion.Euler(rotation);
            edgeDetectionSettings.ApplyTo(Player.instance.playerCam.GetComponent<BladeEdgeDetection>());
        }
    }
    
    [Serializable]
    public class EDSettings {
        [SerializeField]
        public BladeEdgeDetection.EdgeColorMode edgeColorMode;
        [SerializeField]
        public SerializableColor edgeColor;
        [SerializeField]
        public SerializableGradient edgeColorGradient;
        // Can't serialize textures easily

        public EDSettings(BladeEdgeDetection edgeDetection) {
            this.edgeColorMode = edgeDetection.edgeColorMode;
            this.edgeColor = edgeDetection.edgeColor;

            this.edgeColorGradient = new Gradient {
                alphaKeys = edgeDetection.edgeColorGradient.alphaKeys,
                colorKeys = edgeDetection.edgeColorGradient.colorKeys,
                mode = edgeDetection.edgeColorGradient.mode
            };
        }

        public void ApplyTo(BladeEdgeDetection edgeDetection) {
            edgeDetection.edgeColorMode = this.edgeColorMode;
            edgeDetection.edgeColor = this.edgeColor;
            edgeDetection.edgeColorGradient = this.edgeColorGradient;
        }
    }
}