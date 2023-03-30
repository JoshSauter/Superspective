using Nova;
using SuperspectiveUtils;

public class ToggleVisuals : ItemVisuals {
    // TODO: Centralize disableAlpha and DisabledOverlay logic
    private const float disabledAlpha = .65f;
    public TextBlock name;
    public NovaButton ToggleButton;
    public UIBlock2D DisabledOverlay;
    public ToggleSetting toggleSetting => _toggleSetting;
    private ToggleSetting _toggleSetting;
    private bool hasSubbedToEvents = false;

    public void PopulateFrom(ToggleSetting setting) {
        name.Text = setting.name;
        View.UIBlock.gameObject.name = $"[Toggle] {setting.name}";
        _toggleSetting = setting;
        
        ToggleButton.buttonState.Set(setting.value ? NovaButton.ButtonState.Clicked : NovaButton.ButtonState.Idle);
        ToggleButton.TextBlock.Map(tb => tb.Visible = setting.value);
        
        DisabledOverlay.Color = DisabledOverlay.Color.WithAlpha(setting.isEnabled ? 0f : disabledAlpha);
        ToggleButton.novaInteractable.enabled = setting.isEnabled;

        if (!hasSubbedToEvents) {
            void UpdateWithNewValue(bool newValue) {
                toggleSetting.value = newValue;
                ToggleButton.TextBlock.Map(tb => tb.Visible = newValue);
            }

            ToggleButton.OnClickSimple += () => UpdateWithNewValue(true);
            ToggleButton.OnClickResetSimple += () => UpdateWithNewValue(false);
            
            hasSubbedToEvents = true;
        }
    }
}