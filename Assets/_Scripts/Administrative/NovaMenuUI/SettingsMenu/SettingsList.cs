using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;

namespace NovaMenuUI {
    public class SettingsList : MonoBehaviour {
        protected static UIStylesheet Style => UIStyleManager.instance.CurrentStylesheet;
    
        public enum MenuType {
            Audio,
            Video,
            Gameplay,
            Controls
        }

        public NovaSelection allMenuSelectButtons;
        public NovaButton menuSelectButton;
        public MenuType menuType;
        public ListView listView;
        // SettingsItems may include stuff like Headers or other objects that aren't settings but are still needed in the list view
        public List<SettingsItem> settingsItems;

        public bool HasSettingOpen() {
            // check for any keybind settings currently listening for a new keybind
            if (Keybind.IsListeningForNewKeybind) return true;
            
            // check for any Dropdown settings that are currently open
            for (int i = listView.MinLoadedIndex; i < listView.MaxLoadedIndex; i++) {
                if (listView.TryGetItemView(i, out ItemView item)) {
                    if (item.Visuals is DropdownVisuals dropdownVisuals) {
                        if (dropdownVisuals.Dropdown.state == Dropdown.DropdownState.Open) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool SettingsListIsOpen => listView.gameObject.activeSelf;

        private void Awake() {
            SettingsMenu.instance.OnMenuOpen += InitSettingsItems;
            menuSelectButton.OnClickSimple += OpenMenu;
            menuSelectButton.OnClickResetSimple += CloseMenu;
        }

        public static void AddAllDataBinders(ListView listView) {
            listView.AddDataBinder<FloatSetting, SliderVisual>(BindSlider);
            listView.AddDataBinder<SmallIntSetting, SmallIntSliderVisuals>(BindSmallIntSlider);
            listView.AddDataBinder<DropdownSetting, DropdownVisuals>(BindDropdown);
            listView.AddDataBinder<TextAreaSetting, TextAreaVisuals>(BindTextArea);
            listView.AddDataBinder<ToggleSetting, ToggleVisuals>(BindToggle);
            listView.AddDataBinder<KeybindSetting, KeybindVisuals>(BindKeybind);
            listView.AddDataBinder<HeaderSettingsItem, HeaderVisuals>(BindHeader);
            listView.AddDataBinder<SeparatorSettingsItem, SeparatorVisuals>(BindSeparator);
            listView.AddDataBinder<SpacerSettingItem, SpacerVisuals>(BindSpacer);
        }

        void Start() {
            AddAllDataBinders(listView);
        }

        private void OnDisable() {
            SettingsMenu.instance.OnMenuOpen -= InitSettingsItems;
            menuSelectButton.OnClickSimple -= OpenMenu;
            menuSelectButton.OnClickResetSimple -= CloseMenu;
        }

        private void CloseMenu() {
            listView.gameObject.SetActive(false);
        }

        private void OpenMenu() {
            listView.gameObject.SetActive(true);
            
            listView.SetDataSource(settingsItems);
        }

        private void Update() {
            // Kinda hacky but keeps the menus consistent
            if (SettingsListIsOpen && allMenuSelectButtons.selection.selection != menuSelectButton) {
                CloseMenu();
                return;
            }
        }

        void InitSettingsItems() {
            StartCoroutine(InitSettingsItemsDelayed());
        }

        IEnumerator InitSettingsItemsDelayed() {
            // Wait until the settings have been copied over to SettingsMenu
            yield return new WaitForEndOfFrame();
            
            void AddSettingCopy(Setting settingToGetCopyOf) {
                settingsItems.Add(SettingsMenu.instance.settingsCopy[settingToGetCopyOf.key]);
            }
            
            settingsItems = new List<SettingsItem>();
            // Explicitly define list order to allow for flexibility
            // Use copies of existing config in Settings so that we can apply the changes or discard them
            switch (menuType) {
                case MenuType.Audio:
                    AddSettingCopy(Settings.Audio.Volume);
                    AddSettingCopy(Settings.Audio.SFXVolume);
                    AddSettingCopy(Settings.Audio.MusicVolume);
                    break;
                case MenuType.Video:
                    AddSettingCopy(Settings.Video.Fullscreen);
                    AddSettingCopy(Settings.Video.Resolution);
                    AddSettingCopy(Settings.Video.TargetFramerate);
                    AddSettingCopy(Settings.Video.VSync);
                    AddSettingCopy(Settings.Video.AntiAliasingEnabled);
                    AddSettingCopy(Settings.Video.LetterboxingEnabled);
                    AddSettingCopy(Settings.Video.PortalDownsampleAmount);
                    AddSettingCopy(Settings.Gameplay.CameraShake);
                    AddSettingCopy(Settings.Video.DoubleThickEdges);
                    break;
                case MenuType.Gameplay:
                    settingsItems.Add(HeaderSettingsItem.Of("Movement"));
                    AddSettingCopy(Settings.Gameplay.SprintByDefault);
                    AddSettingCopy(Settings.Gameplay.SprintBehavior);
                    settingsItems.Add(new SpacerSettingItem());
                    settingsItems.Add(HeaderSettingsItem.Of("Gameplay Visuals"));
                    AddSettingCopy(Settings.Gameplay.CameraShake);
                    AddSettingCopy(Settings.Gameplay.Headbob);
                    settingsItems.Add(new SpacerSettingItem());
                    settingsItems.Add(HeaderSettingsItem.Of("Camera"));
                    AddSettingCopy(Settings.Gameplay.GeneralSensitivity);
                    AddSettingCopy(Settings.Gameplay.XSensitivity);
                    AddSettingCopy(Settings.Gameplay.YSensitivity);
                    settingsItems.Add(new SpacerSettingItem());
                    settingsItems.Add(HeaderSettingsItem.Of("UI"));
                    AddSettingCopy(Settings.Video.LetterboxingEnabled);
                    AddSettingCopy(Settings.Gameplay.ShowInteractionHelp);
                    AddSettingCopy(Settings.Gameplay.ShowDisabledReason);
                    settingsItems.Add(new SpacerSettingItem());
                    settingsItems.Add(HeaderSettingsItem.Of("Autosaves"));
                    AddSettingCopy(Settings.Autosave.AutosaveEnabled);
                    AddSettingCopy(Settings.Autosave.AutosaveOnTimer);
                    AddSettingCopy(Settings.Autosave.AutosaveInterval);
                    AddSettingCopy(Settings.Autosave.AutosaveOnLevelChange);
                    AddSettingCopy(Settings.Autosave.NumAutosaves);
                    settingsItems.Add(new SpacerSettingItem());
                    settingsItems.Add(HeaderSettingsItem.Of("Autoload"));
                    AddSettingCopy(Settings.Autoload.AutoloadEnabled);
                    AddSettingCopy(Settings.Autoload.AutoloadThreshold);
                    settingsItems.Add(new SpacerSettingItem());
                    settingsItems.Add(new SpacerSettingItem());
                    settingsItems.Add(new SpacerSettingItem());
                    break;
                case MenuType.Controls:
                    settingsItems.Add(HeaderSettingsItem.Of("Movement"));
                    AddSettingCopy(Settings.Keybinds.Forward);
                    AddSettingCopy(Settings.Keybinds.Backward);
                    AddSettingCopy(Settings.Keybinds.Left);
                    AddSettingCopy(Settings.Keybinds.Right);
                    settingsItems.Add(new SpacerSettingItem());
                    AddSettingCopy(Settings.Keybinds.Jump);
                    AddSettingCopy(Settings.Keybinds.Sprint);
                    // AddSettingCopy(Settings.Keybinds.AutoRun);
                    settingsItems.Add(HeaderSettingsItem.Of("Interaction"));
                    AddSettingCopy(Settings.Keybinds.Interact);
                    AddSettingCopy(Settings.Keybinds.AlignObject);
                    AddSettingCopy(Settings.Keybinds.ExitLockedView);
                    AddSettingCopy(Settings.Keybinds.Zoom);
                    //settingsItems.Add(new SpacerSettingItem());
                    //AddSettingCopy(Settings.Keybinds.Pause);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            listView.SetDataSource(settingsItems);
        }

        public static void BindDropdown(Data.OnBind<DropdownSetting> evt, DropdownVisuals target, int index) {
            DropdownSetting setting = evt.UserData;
            target.PopulateFrom(setting);
        }

        public static void BindSlider(Data.OnBind<FloatSetting> evt, SliderVisual target, int index) {
            FloatSetting setting = evt.UserData;
            target.PopulateFrom(setting);
        }

        public static void BindSmallIntSlider(Data.OnBind<SmallIntSetting> evt, SmallIntSliderVisuals target, int index) {
            SmallIntSetting setting = evt.UserData;
            target.PopulateFrom(setting);
        }
        
        public static void BindTextArea(Data.OnBind<TextAreaSetting> evt, TextAreaVisuals target, int index) {
            TextAreaSetting setting = evt.UserData;
            target.PopulateFrom(setting);
        }

        public static void BindToggle(Data.OnBind<ToggleSetting> evt, ToggleVisuals target, int index) {
            ToggleSetting setting = evt.UserData;
            target.PopulateFrom(setting);
        }

        public static void BindKeybind(Data.OnBind<KeybindSetting> evt, KeybindVisuals target, int index) {
            KeybindSetting setting = evt.UserData;
            target.PopulateFrom(setting);
        }

        public static void BindHeader(Data.OnBind<HeaderSettingsItem> evt, HeaderVisuals target, int index) {
            target.View.UIBlock.gameObject.name = $"[Header] {evt.UserData.Name}";
            target.Name.Text = evt.UserData.Name;
        }
        public static void BindSeparator(Data.OnBind<SeparatorSettingsItem> evt, SeparatorVisuals target, int index) {
            target.View.UIBlock.gameObject.name = "[Separator]";
        }

        public static void BindSpacer(Data.OnBind<SpacerSettingItem> evt, SpacerVisuals target, int index) {
            target.View.UIBlock.gameObject.name = "[Spacer]";
        }
    }
}
