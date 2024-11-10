using Nova;
using NovaMenuUI;
using SuperspectiveUtils;

namespace NovaMenuUI {
    public class ToggleVisuals : ItemVisuals {
        private static UIStylesheet Style => UIStyleManager.instance.CurrentStylesheet;
        public TextBlock name;
        public NovaButton HoverButton; // Just used for the hovering over settings visuals
        public NovaButton ToggleButton;
        public NovaButton ResetButton;
        public UIBlock2D DisabledOverlay;
        public ToggleSetting toggleSetting => _toggleSetting;
        private ToggleSetting _toggleSetting;
        private bool hasSubbedToEvents = false;

        public void PopulateFrom(ToggleSetting setting) {
            name.Text = setting.name;
            View.UIBlock.gameObject.name = $"[Toggle] {setting.name}";
            _toggleSetting = setting;
        
            ToggleButton.clickState.Set(setting.value ? NovaButton.ClickState.Clicked : NovaButton.ClickState.Idle);
            ToggleButton.TextBlock.Map(tb => tb.Visible = setting.value);
        
            DisabledOverlay.Color = setting.isEnabled ? Style.Settings.DisabledOverlayColor.WithAlpha(0f) : Style.Settings.DisabledOverlayColor;
            ToggleButton.isEnabled = setting.isEnabled;
            HoverButton.isEnabled = setting.isEnabled;
            HoverButton.backgroundColorOverride = Style.Settings.BackgroundColor;
            HoverButton.hoverColorOverride = Style.Settings.HoverColor;
            ResetButton.isEnabled = setting.isEnabled;

            if (!hasSubbedToEvents) {
                void UpdateWithNewValue(bool newValue) {
                    toggleSetting.value = newValue;
                    ToggleButton.TextBlock.Map(tb => tb.Visible = newValue);
                }

                ToggleButton.OnClickSimple += () => UpdateWithNewValue(true);
                ToggleButton.OnClickResetSimple += () => UpdateWithNewValue(false);

                ResetButton.OnClickSimple += () => {
                    if (toggleSetting.value != toggleSetting.defaultValue) {
                        ToggleButton.Click();
                    }
                };
            
                hasSubbedToEvents = true;
            }
        }
    }
}