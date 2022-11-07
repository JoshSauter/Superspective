using Nova;

public class TextAreaVisuals : ItemVisuals {
    public TextBlock Name;
    public SuperspectiveTextField textField;

    public void PopulateFrom(TextAreaSetting setting) {
        View.UIBlock.gameObject.name = $"{setting.Name} TextArea";
        Name.Text = setting.Name;
        textField.Text = setting.Text;
        textField.placeHolderMessage = setting.PlaceholderText;
        textField.placeHolderText.Text = setting.PlaceholderText;

        textField.OnTextChanged += () => setting.Text = textField.Text;
    }
}
