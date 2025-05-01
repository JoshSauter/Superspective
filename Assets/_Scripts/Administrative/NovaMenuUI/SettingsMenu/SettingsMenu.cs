using System.Collections.Generic;
using System.Linq;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

namespace NovaMenuUI {
    public class SettingsMenu : NovaSubMenu<SettingsMenu> {
        // Copy of the applied settings when the settings menu opens
        public Dictionary<string, Setting> settingsCopy;
        
        public NovaButton ApplyButton;
        public NovaButton ApplyAndResumeButton;

        public SettingsList[] settingsLists;

        public override bool CanClose => !settingsLists.ToList().Exists(settingsList => settingsList.HasSettingOpen());

        public override void Awake() {
            base.Awake();

            InitEvents();
        }

        void OnDisable() {
            TeardownEvents();
        }

        private void Update() {
            if (settingsCopy == null || settingsCopy.Count == 0) return;
            
            // Move this to only get called when settings on the menu are updated if this causes performance issues:
            var settingsThatShouldBeDisabled = Settings.SettingsThatShouldBeDisabled(settingsCopy);
            bool dirty = false;
            foreach (var kv in settingsCopy) {
                string key = kv.Key;
                Setting setting = kv.Value;

                bool wasEnabled = setting.isEnabled;

                setting.isEnabled = !settingsThatShouldBeDisabled.Contains(key);

                if (wasEnabled != setting.isEnabled) {
                    dirty = true;
                }
            }

            if (dirty) {
                foreach (var settingList in settingsLists) {
                    settingList.listView.Refresh();
                }
            }
        }

        private void ApplySettingsAndClose() {
            ApplySettings();
            NovaPauseMenu.instance.ClosePauseMenu();
        }
        
        private void ApplySettings() {
            bool IsDirty(KeyValuePair<string, Setting> kv) {
                string key = kv.Key;
                Setting settingInMenu = kv.Value;

                return !settingInMenu.IsEqual(Settings.GetSetting(key));
            }

            Dictionary<string, Setting> dirtiedSettingsByKey = settingsCopy
                .Where(IsDirty)
                .ToDictionary();

            if (dirtiedSettingsByKey.Count == 0) return;
            
            Debug.Log($"The following settings are dirty: {string.Join(",", dirtiedSettingsByKey.Keys)}");
            
            Settings.UpdateSettings(dirtiedSettingsByKey);
		    
            SaveManager.SaveSettings();
        }

        private void RevertSettings() {
            CopyCurrentSettingsToMenu();
        }

        private void CopyCurrentSettingsToMenu() {
            settingsCopy = Settings.allSettings.MapValues(Setting.Copy);
            
            // If the current resolution doesn't exist in the resolution options, add it
            AddCurrentResolutionIfDoesNotExist();
        }

        void AddCurrentResolutionIfDoesNotExist() {
            // Looking for a resolution option matching the current resolution
            ResolutionDatum target = ResolutionDatum.Of(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);
            DropdownSetting resolutionDropdown = settingsCopy[Settings.Video.Resolution.key] as DropdownSetting;
            List<ResolutionDatum> availableResolutions = resolutionDropdown.dropdownSelection.allItems
                .Select(i => i.Datum)
                .OfType<ResolutionDatum>()
                .ToList();

            // Look for a matching resolution in the available resolutions
            bool foundMatch = false;
            int index;
            for (index = 0; index < availableResolutions.Count; index++) {
                if (availableResolutions[index].Resolution == target.Resolution) {
                    foundMatch = true;
                    break;
                }
            }

            // If we didn't find a match, add the current resolution
            if (!foundMatch) {
                resolutionDropdown.dropdownSelection.allItems.Add(new DropdownOption {
                    Datum = target,
                    DisplayName = $"{target.Resolution.x} x {target.Resolution.y}"
                });
                // Resort the options by resolution width
                resolutionDropdown.dropdownSelection.allItems = resolutionDropdown.dropdownSelection.allItems.OrderByDescending(item => ((ResolutionDatum)item.Datum).Resolution.x).ToList();
                // Remember where this was inserted
                index = resolutionDropdown.dropdownSelection.allItems.FindIndex(option => (ResolutionDatum)option.Datum == target);
            }

            resolutionDropdown.DefaultIndex = index;
            var selectionItem = resolutionDropdown.dropdownSelection.allItems[index];
            resolutionDropdown.dropdownSelection.Select(selectionItem.DisplayName, selectionItem);
        }

        void InitEvents() {
            OnMenuOpen += CopyCurrentSettingsToMenu;
            OnMenuClose += RevertSettings;

            ApplyButton.OnClickSimple += ApplySettings;
            ApplyAndResumeButton.OnClickSimple += ApplySettingsAndClose;
        }

        void TeardownEvents() {
            OnMenuOpen -= CopyCurrentSettingsToMenu;
            OnMenuClose -= RevertSettings;

            ApplyButton.OnClickSimple -= ApplySettings;
            ApplyAndResumeButton.OnClickSimple -= ApplySettingsAndClose;
        }
    }
}
