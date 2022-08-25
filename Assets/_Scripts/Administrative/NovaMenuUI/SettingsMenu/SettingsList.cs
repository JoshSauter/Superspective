using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

public class SettingsList : MonoBehaviour {
    
    public enum MenuType {
        Audio,
        Video,
        Gameplay
    }

    public NovaRadioSelection allMenuSelectButtons;
    public NovaButton menuSelectButton;
    public MenuType menuType;
    public ListView listView;
    // SettingsItems may include stuff like Headers or other objects that aren't settings but are still needed in the list view
    public List<SettingsItem> settingsItems;
    public Dictionary<string, Setting> settingsByKey;

    public NovaButton ApplyButton;
    public NovaButton ApplyAndResumeButton;

    private bool settingsListIsOpen => listView.gameObject.activeSelf;

    void Start() {
        InitSettingsItems();

        menuSelectButton.OnClick += (_) => OpenMenu();
        menuSelectButton.OnClickReset += (_) => CloseMenu();

        ApplyButton.OnClick += ApplySettings;
        ApplyAndResumeButton.OnClick += _ => {
            ApplySettings(_);
            NovaPauseMenu.instance.ClosePauseMenu();
        };
        
        listView.AddDataBinder<FloatSetting, SliderVisual>(BindSlider);
        listView.AddDataBinder<DropdownSetting, DropdownVisuals>(BindDropdown);

        listView.SetDataSource(settingsItems);
    }

    private void CloseMenu() {
        listView.gameObject.SetActive(false);
    }

    private void OpenMenu() {
        listView.gameObject.SetActive(true);
    }

    private void ApplySettings(NovaButton button) {
        bool IsDirty(KeyValuePair<string, Setting> kv) {
            string key = kv.Key;
            Setting settingInMenu = kv.Value;

            return !settingInMenu.IsEqual(Settings.GetSetting(key));
        }

        Dictionary<string, Setting> dirtiedSettingsByKey = settingsByKey
            .Where(IsDirty)
            .ToDictionary();
        
        Debug.Log($"The following settings are dirty: {string.Join(",", dirtiedSettingsByKey.Keys)}");
        
        Settings.UpdateSettings(dirtiedSettingsByKey);
    }

    private void Update() {
        // Kinda hacky but keeps the menus consistent
        if (settingsListIsOpen && allMenuSelectButtons.selection != menuSelectButton) {
            CloseMenu();
        }
    }

    void InitSettingsItems() {
        settingsItems = new List<SettingsItem>();
        // Explicitly define list order to allow for flexibility
        // Make copies of existing config in Settings so that we can apply the changes or discard them
        switch (menuType) {
            case MenuType.Audio:
                settingsItems.Add(FloatSetting.Copy(Settings.Audio.Volume));
                break;
            case MenuType.Video:
                settingsItems.Add(DropdownSetting.Copy(Settings.Video.Fullscreen));
                settingsItems.Add(DropdownSetting.Copy(Settings.Video.Resolution));
                settingsItems.Add(FloatSetting.Copy(Settings.Video.PortalDownsampleAmount));
                break;
            case MenuType.Gameplay:
                settingsItems.Add(FloatSetting.Copy(Settings.Gameplay.Headbob));
                settingsItems.Add(FloatSetting.Copy(Settings.Gameplay.GeneralSensitivity));
                settingsItems.Add(FloatSetting.Copy(Settings.Gameplay.XSensitivity));
                settingsItems.Add(FloatSetting.Copy(Settings.Gameplay.YSensitivity));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        settingsByKey = settingsItems.OfType<Setting>().ToDictionary(s => s.Key, s => s);
        
        // If the current resolution doesn't exist in the resolution options, add it
        if (menuType == MenuType.Video) {
            AddCurrentResolutionIfDoesNotExist();
        }
    }

    void AddCurrentResolutionIfDoesNotExist() {
        // Looking for a resolution option matching the current resolution
        ResolutionDatum target = ResolutionDatum.Of(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);
        DropdownSetting resolutionDropdown = settingsByKey[Settings.Video.Resolution.Key] as DropdownSetting;
        List<ResolutionDatum> availableResolutions = resolutionDropdown.AllDropdownItems
            .Select(i => i.Datum)
            .OfType<ResolutionDatum>()
            .ToList();

        // Look for a matching resolution in the available resolutions
        bool foundMatch = false;
        int index;
        for (index = 0; index < availableResolutions.Count; index++) {
            if (availableResolutions[index] == target) {
                foundMatch = true;
                break;
            }
        }

        // If we didn't find a match, add the current resolution
        if (!foundMatch) {
            resolutionDropdown.AllDropdownItems.Add(new DropdownOption {
                Datum = target,
                DisplayName = $"{target.Resolution.x} x {target.Resolution.y}"
            });
            // Resort the options by resolution width
            resolutionDropdown.AllDropdownItems = resolutionDropdown.AllDropdownItems.OrderByDescending(item => ((ResolutionDatum)item.Datum).Resolution.x).ToList();
            // Remember where this was inserted
            index = resolutionDropdown.AllDropdownItems.FindIndex(option => (ResolutionDatum)option.Datum == target);
        }

        resolutionDropdown.DefaultIndex = index;
        resolutionDropdown.SelectedIndex = index;
    }

    private void BindDropdown(Data.OnBind<DropdownSetting> evt, DropdownVisuals target, int index) {
        DropdownSetting setting = evt.UserData;
        target.PopulateFrom(setting);
    }

    private void BindSlider(Data.OnBind<FloatSetting> evt, SliderVisual target, int index) {
        FloatSetting setting = evt.UserData;
        target.PopulateFrom(setting);
    }
}
