using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audio;
using UnityEngine;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;

public class Settings : Singleton<Settings> {
	public static readonly Dictionary<string, Setting> allSettings = new Dictionary<string, Setting>();

	public static Setting GetSetting(string key) {
		return allSettings[key];
	}

	public static HashSet<string> SettingsThatShouldBeDisabled(Dictionary<string, Setting> currentSettings) {
		bool SettingMatchesCriteria(string key, Predicate<Setting> criteria) {
			return currentSettings.ContainsKey(key) && criteria(currentSettings[key]);
		}
		
		HashSet<string> resultKeys = new HashSet<string>();
		if (SettingMatchesCriteria(Video.VSync.key, (setting) => (int)((DropdownSetting)setting).dropdownSelection.selection.Datum != 0)) {
			resultKeys.Add(Video.TargetFramerate.key);
		}

		bool autosaveDisabled = SettingMatchesCriteria(Autosave.AutosaveEnabled.key, (setting) => !((ToggleSetting)setting).value);
		bool autosaveOnTimerDisabled = autosaveDisabled || SettingMatchesCriteria(Autosave.AutosaveOnTimer.key, (setting) => !((ToggleSetting)setting).value);
		if (autosaveDisabled) {
			resultKeys.Add(Autosave.AutosaveOnLevelChange.key);
			resultKeys.Add(Autosave.AutosaveOnTimer.key);
			resultKeys.Add(Autosave.NumAutosaves.key);
		}
		
		if (autosaveOnTimerDisabled) {
			resultKeys.Add(Autosave.AutosaveInterval.key);
		}

		if (SettingMatchesCriteria(Autoload.AutoloadEnabled.key, (setting) => !((ToggleSetting)setting).value)) {
			resultKeys.Add(Autoload.AutoloadThreshold.key);
		}

		return resultKeys;
	}

	public static void UpdateSettings(Dictionary<string, Setting> updatedSettings) {
		updatedSettings.ToList().ForEach(kv => allSettings[kv.Key].CopySettingsFrom(kv.Value));

		bool Changed(params Setting[] settings) {
			return settings.ToList().Exists(s => updatedSettings.ContainsKey(s.key));
		}
		
		// React to Screen Resoution/Fullscreen mode changed
		if (Changed(Video.Resolution, Video.Fullscreen)) {
			Vector2Int resolution = ((ResolutionDatum)((DropdownSetting)allSettings[Video.Resolution.key]).dropdownSelection.selection.Datum).Resolution;
			FullScreenMode fullScreenMode = (FullScreenMode)((DropdownSetting)allSettings[Video.Fullscreen.key]).dropdownSelection.selection.Datum;
			Screen.SetResolution(resolution.x, resolution.y, fullScreenMode);
		}

		if (Changed(Video.VSync)) {
			QualitySettings.vSyncCount = (int)((DropdownSetting)allSettings[Video.VSync.key]).dropdownSelection.selection.Datum;
		}

		if (Changed(Video.TargetFramerate)) {
			Application.targetFrameRate = (int)((DropdownSetting)allSettings[Video.TargetFramerate.key]).dropdownSelection.selection.Datum;
		}
		
		// React to volume changed
		if (Changed(Audio.Volume)) {
			AudioListener.volume = ((FloatSetting)allSettings[Audio.Volume.key]).value / 100f;
		}
		if (Changed(Audio.SFXVolume)) {
			AudioManager.instance.sfxVolume = ((FloatSetting)allSettings[Audio.SFXVolume.key]).value / 100f;
		}
		if (Changed(Audio.MusicVolume)) {
			AudioManager.instance.musicVolume = ((FloatSetting)allSettings[Audio.MusicVolume.key]).value / 100f;
		}
	}

	public static class Audio {
		public static readonly FloatSetting Volume = new FloatSetting {
			key = "Volume",
			name = "Volume",
			value = 50,
			defaultValue = 50,
			minValue = 0,
			maxValue = 100
		};
		public static readonly FloatSetting SFXVolume = new FloatSetting {
			key = "SFXVolume",
			name = "SFX Volume",
			value = 50,
			defaultValue = 50,
			minValue = 0,
			maxValue = 100
		};
		public static readonly FloatSetting MusicVolume = new FloatSetting {
			key = "MusicVolume",
			name = "Music Volume (what music?)",
			value = 50,
			defaultValue = 50,
			minValue = 0,
			maxValue = 100
		};
	}

	public static class Video {
		public static readonly DropdownSetting Resolution = DropdownSetting.Of(
			key: "Resolution",
			name: "Resolution",
			allDropdownItems: new List<DropdownOption>() {
				DropdownOption.Of("3840 x 2160", ResolutionDatum.Of(3840, 2160)),
				DropdownOption.Of("2560 x 1600", ResolutionDatum.Of(2560, 1600)),
				DropdownOption.Of("2560 x 1440", ResolutionDatum.Of(2560, 1440)),
				DropdownOption.Of("2048 x 1536", ResolutionDatum.Of(2048, 1536)),
				DropdownOption.Of("1920 x 1440", ResolutionDatum.Of(1920, 1440)),
				DropdownOption.Of("1920 x 1200", ResolutionDatum.Of(1920, 1200)),
				DropdownOption.Of("1920 x 1080", ResolutionDatum.Of(1920, 1080)),
				DropdownOption.Of("1680 x 1050", ResolutionDatum.Of(1680, 1050)),
				DropdownOption.Of("1600 x 1200", ResolutionDatum.Of(1600, 1200)),
				DropdownOption.Of("1600 x 1024", ResolutionDatum.Of(1600, 1024)),
				DropdownOption.Of("1600 x 900", ResolutionDatum.Of(1600, 900)),
				DropdownOption.Of("1440 x 900", ResolutionDatum.Of(1440, 900)),
				DropdownOption.Of("1366 x 768", ResolutionDatum.Of(1366, 768)),
				DropdownOption.Of("1360 x 768", ResolutionDatum.Of(1360, 768)),
				DropdownOption.Of("1280 x 1024", ResolutionDatum.Of(1280, 1024)),
				DropdownOption.Of("1280 x 960", ResolutionDatum.Of(1280, 960)),
				DropdownOption.Of("1280 x 800", ResolutionDatum.Of(1280, 800)),
				DropdownOption.Of("1280 x 768", ResolutionDatum.Of(1280, 768)),
				DropdownOption.Of("1280 x 720", ResolutionDatum.Of(1280, 720)),
				DropdownOption.Of("1176 x 664", ResolutionDatum.Of(1176, 664)),
				DropdownOption.Of("1152 x 864", ResolutionDatum.Of(1152, 864)),
				DropdownOption.Of("1024 x 768", ResolutionDatum.Of(1024, 768)),
			},
			defaultIndex: 0,
			selectedIndex: 0
		);

		public static readonly DropdownSetting Fullscreen = DropdownSetting.Of(
			key: "Fullscreen",
			name: "Fullscreen",
			allDropdownItems: new List<DropdownOption>() {
				DropdownOption.Of("Fullscreen", FullScreenMode.ExclusiveFullScreen),
				DropdownOption.Of("Borderless", FullScreenMode.FullScreenWindow),
				DropdownOption.Of("Windowed", FullScreenMode.Windowed)
			},
			defaultIndex: 0,
			selectedIndex: Mathf.Clamp((int)Screen.fullScreenMode, 0, 2)
		);

		public static readonly SmallIntSetting PortalDownsampleAmount = new SmallIntSetting {
			key = "PortalDownSample",
			name = "Portal Texture Downsampling",
			value = 0,
			defaultValue = 0,
			minValue = 0,
			maxValue = 3
		};

		public static readonly DropdownSetting VSync = DropdownSetting.Of(
			key: "VSync",
			name: "VSync",
			allDropdownItems: new List<DropdownOption>() {
				DropdownOption.Of("VSync Off", 0),
				DropdownOption.Of("VSync On", 1),
				DropdownOption.Of("VSync 2x", 2),
				DropdownOption.Of("VSync 3x", 3)
			},
			defaultIndex: 0,
			selectedIndex: QualitySettings.vSyncCount
		);

		public static readonly DropdownSetting TargetFramerate = DropdownSetting.Of(
			key: "TargetFramerate",
			name: "Target Framerate",
			allDropdownItems: new List<DropdownOption>() {
				DropdownOption.Of("Uncapped", -1),
				DropdownOption.Of("25 FPS", 25),
				DropdownOption.Of("30 FPS", 30),
				DropdownOption.Of("60 FPS", 60),
				DropdownOption.Of("80 FPS", 80),
				DropdownOption.Of("120 FPS", 120),
				DropdownOption.Of("144 FPS", 144),
				DropdownOption.Of("180 FPS", 180),
				DropdownOption.Of("200 FPS", 200),
				DropdownOption.Of("240 FPS", 240),
			},
			defaultIndex: 0,
			selectedIndex: 0
		);

		public static readonly ToggleSetting AntiAliasingEnabled = new ToggleSetting() {
			key = "AntiAliasingEnabled",
			name = "Anti-Aliasing Enabled",
			value = false,
			defaultValue = false,
		};

		public static readonly ToggleSetting LetterboxingEnabled = new ToggleSetting() {
			key = "LetterboxingEnabled",
			name = "Enable Letterboxing (Black Bars) When Changing Levels",
			value = true,
			defaultValue = true
		};
	}

	public static class Gameplay {
		public static readonly ToggleSetting SprintByDefault = new ToggleSetting() {
			key = "SprintByDefault",
			name = "Sprint By Default",
			value = true,
			defaultValue = true
		};

		public enum SprintBehaviorMode {
			Toggle,
			Hold
		}
		public static readonly DropdownSetting SprintBehavior = DropdownSetting.Of(
			key: "SprintBehavior",
			name: "Sprint Behavior",
			allDropdownItems: new List<DropdownOption>() {
				DropdownOption.Of("Toggle", SprintBehaviorMode.Toggle),
				DropdownOption.Of("Hold", SprintBehaviorMode.Hold)
			},
			defaultIndex: 1,
			selectedIndex: 1
		);
		
		public static readonly FloatSetting CameraShake = new FloatSetting {
			key = "CameraShake",
			name = "Camera Shake",
			value = 100,
			defaultValue = 100,
			minValue = 0,
			maxValue = 100
		};
			
		public static readonly FloatSetting Headbob = new FloatSetting {
			key = "Headbob",
			name = "Headbob",
			value = 50,
			defaultValue = 50,
			minValue = 0,
			maxValue = 100
		};

		public static readonly FloatSetting GeneralSensitivity = new FloatSetting {
			key = "GeneralSensitivity",
			name = "General Sensitivity",
			value = 30,
			defaultValue = 30,
			minValue = 5,
			maxValue = 100
		};
		public static readonly FloatSetting XSensitivity = new FloatSetting {
			key = "XSensitivity",
			name = "X Sensitivity",
			value = 50,
			defaultValue = 50,
			minValue = 5,
			maxValue = 100
		};
		public static readonly FloatSetting YSensitivity = new FloatSetting {
			key = "YSensitivity",
			name = "Y Sensitivity",
			value = 50,
			defaultValue = 50,
			minValue = 5,
			maxValue = 100
		};

		public static readonly ToggleSetting ShowInteractionHelp = new ToggleSetting() {
			key = "ShowInteractionHelp",
			name = "Show interaction help",
			value = false,
			defaultValue = true
		};
		public static readonly ToggleSetting ShowDisabledReason = new ToggleSetting {
			key = "ShowDisabledReason",
			name = "Show reason for disabled interactions",
			value = true,
			defaultValue = true
		};
	}

	public static class Autosave {
		public static readonly ToggleSetting AutosaveEnabled = new ToggleSetting() {
			key = "AutosaveEnabled",
			name = "Autosave Enabled",
			value = true,
			defaultValue = true
		};
		
		public static readonly ToggleSetting AutosaveOnLevelChange = new ToggleSetting() {
			key = "AutosaveOnLevelChange",
			name = "Autosave On Level Change",
			value = true,
			defaultValue = true
		};

		public static readonly ToggleSetting AutosaveOnTimer = new ToggleSetting() {
			key = "AutosaveOnTimer",
			name = "Autosave On Timer",
			value = true,
			defaultValue = true
		};
		
		public static readonly DropdownSetting AutosaveInterval = DropdownSetting.Of(
			key: "AutosaveInterval",
			name: "Time Between Autosaves",
			allDropdownItems: new List<DropdownOption>() {
				DropdownOption.Of("15 seconds", 15),
				DropdownOption.Of("30 seconds", 30),
				DropdownOption.Of("1 minute", 60),
				DropdownOption.Of("5 minutes", 60 * 5),
				DropdownOption.Of("10 minutes", 60 * 10),
				DropdownOption.Of("15 minutes", 60 * 15),
				DropdownOption.Of("30 minutes", 60 * 30),
				DropdownOption.Of("1 hour", 60 * 60)
			},
			defaultIndex: 3,
			selectedIndex: 3
		);

		public static readonly FloatSetting NumAutosaves = new FloatSetting {
			key = "NumAutosaves",
			name = "# of Autosaves to Keep",
			value = 20,
			defaultValue = 20,
			minValue = 1,
			maxValue = 100
		};
	}

	public static class Autoload {
		public static readonly ToggleSetting AutoloadEnabled = new ToggleSetting() {
			key = "AutoloadEnabled",
			name = "Autoload from last save...",
			value = true,
			defaultValue = false
		};

		public static readonly DropdownSetting AutoloadThreshold = DropdownSetting.Of(
			key: "AutoloadThreshold",
			name: "...within last",
			allDropdownItems: new List<DropdownOption>() {
				DropdownOption.Of("∞", -1),
				DropdownOption.Of("1 day", 1),
				DropdownOption.Of("2 days", 2),
				DropdownOption.Of("3 days", 3),
				DropdownOption.Of("1 week", 7),
				DropdownOption.Of("2 weeks", 14),
				DropdownOption.Of("1 month", 31),
				DropdownOption.Of("3 months", 93),
				DropdownOption.Of("6 months", 186),
				DropdownOption.Of("1 year", 365),
				DropdownOption.Of("∞", -1),
			},
			defaultIndex: 5,
			selectedIndex: 5
		);
	}

	public static class Keybinds {
		public static readonly KeybindSetting Forward = new KeybindSetting("Forward", new KeyboardAndMouseInput(KeyCode.W, KeyCode.UpArrow));
		public static readonly KeybindSetting Backward = new KeybindSetting("Backward", new KeyboardAndMouseInput(KeyCode.S, KeyCode.DownArrow));
		public static readonly KeybindSetting Left = new KeybindSetting("Left", new KeyboardAndMouseInput(KeyCode.A, KeyCode.LeftArrow));
		public static readonly KeybindSetting Right = new KeybindSetting("Right", new KeyboardAndMouseInput(KeyCode.D, KeyCode.RightArrow));
		
		public static readonly KeybindSetting Jump = new KeybindSetting("Jump", new KeyboardAndMouseInput(KeyCode.Space));
		public static readonly KeybindSetting Sprint = new KeybindSetting("Sprint", new KeyboardAndMouseInput(KeyCode.LeftShift, KeyCode.RightShift));
		// public static readonly KeybindSetting AutoRun = new KeybindSetting("Auto Run", new KeyboardAndMouseInput(KeyCode.BackQuote));
		public static readonly KeybindSetting Interact = new KeybindSetting("Interact", new KeyboardAndMouseInput(0, KeyCode.E)); // Left-mouse default

		public static readonly KeybindSetting Zoom = new KeybindSetting("Zoom", new KeyboardAndMouseInput(1)); // Right-mouse default
		public static readonly KeybindSetting AlignObject = new KeybindSetting("Align Object", new KeyboardAndMouseInput(2)); // Middle-mouse default
		public static readonly KeybindSetting Pause = new KeybindSetting("Pause", new KeyboardAndMouseInput(KeyCode.Escape));
	}

    protected void Awake() {
        InitSettingsDict();
    }

    void InitSettingsDict() {
	    allSettings.Clear();

	    void AddSetting(Setting setting) {
		    allSettings.Add(setting.key, setting);
	    }
	    
	    // Audio Settings
	    AddSetting(Audio.Volume);
	    AddSetting(Audio.SFXVolume);
	    AddSetting(Audio.MusicVolume);
	    
	    // Video Settings
	    AddSetting(Video.Fullscreen);
	    AddSetting(Video.Resolution);
	    AddSetting(Video.TargetFramerate);
	    AddSetting(Video.VSync);
	    AddSetting(Video.AntiAliasingEnabled);
	    AddSetting(Video.LetterboxingEnabled);
	    AddSetting(Video.PortalDownsampleAmount);
	    
	    // Gameplay Settings
	    AddSetting(Gameplay.SprintByDefault);
	    AddSetting(Gameplay.SprintBehavior);
	    AddSetting(Gameplay.CameraShake);
	    AddSetting(Gameplay.Headbob);
	    AddSetting(Gameplay.GeneralSensitivity);
	    AddSetting(Gameplay.XSensitivity);
	    AddSetting(Gameplay.YSensitivity);
	    AddSetting(Gameplay.ShowDisabledReason);
	    AddSetting(Gameplay.ShowInteractionHelp);
	    
	    // Autosave Settings
	    AddSetting(Autosave.AutosaveEnabled);
	    AddSetting(Autosave.AutosaveOnLevelChange);
	    AddSetting(Autosave.AutosaveOnTimer);
	    AddSetting(Autosave.AutosaveInterval);
	    AddSetting(Autosave.NumAutosaves);
	    
	    // Autoload Settings
	    AddSetting(Autoload.AutoloadEnabled);
	    AddSetting(Autoload.AutoloadThreshold);
	    
	    // Keybind Settings
	    AddSetting(Keybinds.Forward);
	    AddSetting(Keybinds.Backward);
	    AddSetting(Keybinds.Left);
	    AddSetting(Keybinds.Right);
	    AddSetting(Keybinds.Jump);
	    AddSetting(Keybinds.Sprint);
	    // AddSetting(Keybinds.AutoRun);
	    AddSetting(Keybinds.Interact);
	    AddSetting(Keybinds.Zoom);
	    AddSetting(Keybinds.AlignObject);
	    AddSetting(Keybinds.Pause);
    }
    
#region Saving

	public void WriteSettings(string path) {
		List<string> lines = allSettings.Values.Select(setting => $"{setting.key}={setting.PrintValue()}").ToList();
		string txt = string.Join("\n", lines);
		File.WriteAllText(path, txt);
	}

	public void LoadSettings(string path) {
		if (File.Exists(path)) {
			string settingsTxt = File.ReadAllText(path);
			List<string> lines = settingsTxt.Split("\n").ToList();
			List<string> keysAndValues = lines.SelectMany(l => l.Split("=")).ToList();
			SettingsMenu.instance.settingsCopy = allSettings.MapValues(Setting.Copy);
			for (int i = 0; i < keysAndValues.Count; i++) {
				var keyOrValue = keysAndValues[i];
				// Look for valid key names and get the value from the next index
				if (SettingsMenu.instance.settingsCopy.ContainsKey(keyOrValue)) {
					string key = keyOrValue;
					string value = keysAndValues[++i];
					SettingsMenu.instance.settingsCopy[key].ParseValue(value);
				}
			}
			
			UpdateSettings(SettingsMenu.instance.settingsCopy);
		}
	}
#endregion
}


public class ResolutionDatum {
	public Vector2Int Resolution;

	public static ResolutionDatum Of(int width, int height) {
		return new ResolutionDatum() {
			Resolution = new Vector2Int(width, height)
		};
	}
}