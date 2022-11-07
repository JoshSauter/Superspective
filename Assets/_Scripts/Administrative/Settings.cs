using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using UnityEngine;
using Saving;
using SuperspectiveUtils;

public class Settings : SingletonSaveableObject<Settings, Settings.SettingsSave> {
	public override string ID => "Settings";
	public static readonly Dictionary<string, Setting> allSettings = new Dictionary<string, Setting>();

	public static Setting GetSetting(string key) {
		return allSettings[key];
	}

	public static HashSet<string> SettingsThatShouldBeDisabled(Dictionary<string, Setting> currentSettings) {
		HashSet<string> resultKeys = new HashSet<string>();
		if (currentSettings.ContainsKey(Video.VSync.key)) {
			if ((int)((DropdownSetting)currentSettings[Video.VSync.key]).dropdownSelection.selection.Datum != 0) {
				resultKeys.Add(Video.TargetFramerate.key);
			}
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
			AudioListener.volume = ((FloatSetting)allSettings[Audio.Volume.key]).Value / 100f;
		}
		if (Changed(Audio.SFXVolume)) {
			AudioManager.instance.sfxVolume = ((FloatSetting)allSettings[Audio.SFXVolume.key]).Value / 100f;
		}
		if (Changed(Audio.MusicVolume)) {
			AudioManager.instance.musicVolume = ((FloatSetting)allSettings[Audio.MusicVolume.key]).Value / 100f;
		}
	}

	public static class Audio {
		public static readonly FloatSetting Volume = new FloatSetting {
			key = "Volume",
			Name = "Volume",
			Value = 50,
			DefaultValue = 50,
			MinValue = 0,
			MaxValue = 100
		};
		public static readonly FloatSetting SFXVolume = new FloatSetting {
			key = "SFXVolume",
			Name = "SFX Volume",
			Value = 50,
			DefaultValue = 50,
			MinValue = 0,
			MaxValue = 100
		};
		public static readonly FloatSetting MusicVolume = new FloatSetting {
			key = "MusicVolume",
			Name = "Music Volume (what music?)",
			Value = 50,
			DefaultValue = 50,
			MinValue = 0,
			MaxValue = 100
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
			Name = "Portal Texture Downsampling",
			Value = 0,
			DefaultValue = 0,
			MinValue = 0,
			MaxValue = 3
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
	}

	public static class Gameplay {
		public static readonly FloatSetting CameraShake = new FloatSetting {
			key = "CameraShake",
			Name = "Camera Shake",
			Value = 100,
			DefaultValue = 100,
			MinValue = 0,
			MaxValue = 100
		};
			
		public static readonly FloatSetting Headbob = new FloatSetting {
			key = "Headbob",
			Name = "Headbob",
			Value = 50,
			DefaultValue = 50,
			MinValue = 0,
			MaxValue = 100
		};

		public static readonly FloatSetting GeneralSensitivity = new FloatSetting {
			key = "GeneralSensitivity",
			Name = "General Sensitivity",
			Value = 30,
			DefaultValue = 30,
			MinValue = 5,
			MaxValue = 100
		};
		public static readonly FloatSetting XSensitivity = new FloatSetting {
			key = "XSensitivity",
			Name = "X Sensitivity",
			Value = 50,
			DefaultValue = 50,
			MinValue = 5,
			MaxValue = 100
		};
		public static readonly FloatSetting YSensitivity = new FloatSetting {
			key = "YSensitivity",
			Name = "Y Sensitivity",
			Value = 50,
			DefaultValue = 50,
			MinValue = 5,
			MaxValue = 100
		};
	}

	public static class Keybinds {
		public static readonly KeybindSetting Forward = new KeybindSetting("Forward", new KeyboardAndMouseInput(KeyCode.W, KeyCode.UpArrow));
		public static readonly KeybindSetting Backward = new KeybindSetting("Backward", new KeyboardAndMouseInput(KeyCode.S, KeyCode.DownArrow));
		public static readonly KeybindSetting Left = new KeybindSetting("Left", new KeyboardAndMouseInput(KeyCode.A, KeyCode.LeftArrow));
		public static readonly KeybindSetting Right = new KeybindSetting("Right", new KeyboardAndMouseInput(KeyCode.D, KeyCode.RightArrow));
		
		public static readonly KeybindSetting Jump = new KeybindSetting("Jump", new KeyboardAndMouseInput(KeyCode.Space));
		public static readonly KeybindSetting Sprint = new KeybindSetting("Sprint", new KeyboardAndMouseInput(KeyCode.LeftShift, KeyCode.RightShift));
		public static readonly KeybindSetting AutoRun = new KeybindSetting("Auto Run", new KeyboardAndMouseInput(KeyCode.BackQuote));
		public static readonly KeybindSetting Interact = new KeybindSetting("Interact", new KeyboardAndMouseInput(KeyCode.E, 0)); // Left-mouse default

		public static readonly KeybindSetting Zoom = new KeybindSetting("Zoom", new KeyboardAndMouseInput(1)); // Right-mouse default
		public static readonly KeybindSetting AlignObject = new KeybindSetting("Align Object", new KeyboardAndMouseInput(2)); // Middle-mouse default
		public static readonly KeybindSetting Pause = new KeybindSetting("Pause", new KeyboardAndMouseInput(KeyCode.Escape));
	}

    protected override void Awake() {
        base.Awake();

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
	    AddSetting(Video.PortalDownsampleAmount);
	    
	    // Gameplay Settings
	    AddSetting(Gameplay.CameraShake);
	    AddSetting(Gameplay.Headbob);
	    AddSetting(Gameplay.GeneralSensitivity);
	    AddSetting(Gameplay.XSensitivity);
	    AddSetting(Gameplay.YSensitivity);
	    
	    // Keybind Settings
	    AddSetting(Keybinds.Forward);
	    AddSetting(Keybinds.Backward);
	    AddSetting(Keybinds.Left);
	    AddSetting(Keybinds.Right);
	    AddSetting(Keybinds.Jump);
	    AddSetting(Keybinds.Sprint);
	    AddSetting(Keybinds.AutoRun);
	    AddSetting(Keybinds.Interact);
	    AddSetting(Keybinds.Zoom);
	    AddSetting(Keybinds.AlignObject);
	    AddSetting(Keybinds.Pause);
    }
    
#region Saving
		[Serializable]
		public class SettingsSave : SerializableSaveObject<Settings> {

			public SettingsSave(Settings script) : base(script) {
			    // TODO
			}

			public override void LoadSave(Settings script) {
			    // TODO
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