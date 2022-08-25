using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Saving;
using SuperspectiveUtils;

public class Settings : SingletonSaveableObject<Settings, Settings.SettingsSave> {
	public override string ID => "Settings";
	private static readonly Dictionary<string, Setting> allSettings = new Dictionary<string, Setting>();

	public static Setting GetSetting(string key) {
		return allSettings[key];
	}

	public static void UpdateSettings(Dictionary<string, Setting> updatedSettings) {
		updatedSettings.ToList().ForEach(kv => allSettings[kv.Key].CopySettingsFrom(kv.Value));
		
		if (updatedSettings.ContainsKey(Video.Resolution.Key) || updatedSettings.ContainsKey(Video.Fullscreen.Key)) {
			Vector2Int resolution = ((ResolutionDatum)((DropdownSetting)allSettings[Video.Resolution.Key]).SelectedValue.Datum).Resolution;
			FullScreenMode fullScreenMode = (FullScreenMode)((DropdownSetting)allSettings[Video.Fullscreen.Key]).SelectedValue.Datum;
			Screen.SetResolution(resolution.x, resolution.y, fullScreenMode);
		}
	}

	public static class Audio {
		// TODO: Use volume
		public static readonly FloatSetting Volume = new FloatSetting {
			Key = "Volume",
			Name = "Volume",
			Value = 50,
			DefaultValue = 50,
			MinValue = 0,
			MaxValue = 100
		};
	}

	public static class Video {
		public static readonly DropdownSetting Resolution = new DropdownSetting {
			Key = "Resolution",
			Name = "Resolution",
			AllDropdownItems = new List<DropdownOption>() {
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
			DefaultIndex = 0,
			SelectedIndex = 0
		};

		public static readonly DropdownSetting Fullscreen = new DropdownSetting() {
			Key = "Fullscreen",
			Name = "Fullscreen",
			AllDropdownItems = new List<DropdownOption>() {
				DropdownOption.Of("Fullscreen", FullScreenMode.ExclusiveFullScreen),
				DropdownOption.Of("Borderless", FullScreenMode.FullScreenWindow),
				DropdownOption.Of("Windowed", FullScreenMode.Windowed)
			},
			DefaultIndex = 0,
			SelectedIndex = Mathf.Clamp((int)Screen.fullScreenMode, 0, 2)
		};

		public static readonly FloatSetting PortalDownsampleAmount = new FloatSetting {
			Key = "PortalDownSample",
			Name = "Portal Texture Downsampling",
			Value = 0,
			DefaultValue = 0,
			MinValue = 0,
			MaxValue = 3
		};
	}

	public static class Gameplay {
		public static readonly FloatSetting Headbob = new FloatSetting {
			Key = "Headbob",
			Name = "Headbob",
			Value = 50,
			DefaultValue = 50,
			MinValue = 0,
			MaxValue = 100
		};

		public static readonly FloatSetting GeneralSensitivity = new FloatSetting {
			Key = "GeneralSensitivity",
			Name = "General Sensitivity",
			Value = 30,
			DefaultValue = 30,
			MinValue = 5,
			MaxValue = 100
		};
		public static readonly FloatSetting XSensitivity = new FloatSetting {
			Key = "XSensitivity",
			Name = "X Sensitivity",
			Value = 50,
			DefaultValue = 50,
			MinValue = 5,
			MaxValue = 100
		};
		public static readonly FloatSetting YSensitivity = new FloatSetting {
			Key = "YSensitivity",
			Name = "Y Sensitivity",
			Value = 50,
			DefaultValue = 50,
			MinValue = 5,
			MaxValue = 100
		};
	}

    protected override void Awake() {
        base.Awake();

        InitSettingsDict();
    }

    void InitSettingsDict() {
	    allSettings.Clear();

	    void AddSetting(Setting setting) {
		    allSettings.Add(setting.Key, setting);
	    }
	    
	    // Audio Settings
	    AddSetting(Audio.Volume);
	    
	    // Video Settings
	    AddSetting(Video.Fullscreen);
	    AddSetting(Video.Resolution);
	    AddSetting(Video.PortalDownsampleAmount);
	    
	    // Gameplay Settings
	    AddSetting(Gameplay.Headbob);
	    AddSetting(Gameplay.GeneralSensitivity);
	    AddSetting(Gameplay.XSensitivity);
	    AddSetting(Gameplay.YSensitivity);
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