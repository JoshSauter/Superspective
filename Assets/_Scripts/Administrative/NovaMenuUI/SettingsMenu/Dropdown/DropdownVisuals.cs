using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

public class DropdownVisuals : ItemVisuals {
    private const int minNumberOfItemsForScrollbar = 8;
    public Dropdown Dropdown;

    public void PopulateFrom(DropdownSetting setting) {
        bool settingWasntSet = Dropdown.setting != setting;
        Dropdown.setting = setting;

        Dropdown.gameObject.name = $"[Dropdown] {setting.name}";
        Dropdown.Name.Text = setting.name;
        Dropdown.SelectionLabel.Text = setting.SelectedValueName;

        Dropdown.Scroller.ScrollbarVisual.Visible = setting.dropdownSelection.allItems.Count > minNumberOfItemsForScrollbar;

        // Debug.Log($"{setting.name} {this.View.UIBlock.CalculatedPosition.Percent:F2}");
        
        Dropdown.SetDatasource();
        if (settingWasntSet) {
            setting.dropdownSelection.OnSelectionChanged += (prevSelections, newSelections) => {
                Dropdown.UpdateVisuals(newSelections);
            };
        }
        Dropdown.DropdownOptionsListView.Refresh();

        // Dropdown.DropdownOptionsSelection.TryFindIndex(setting.SelectedValue.DisplayName, out int indexOfMatch);
        // Dropdown.DropdownOptionsSelection.SetSelection(indexOfMatch, false);

        Dropdown.DisabledOverlay.Color = setting.isEnabled ? UIStyle.Settings.DisabledOverlayColor.WithAlpha(0f) : UIStyle.Settings.DisabledOverlayColor;
        Dropdown.SelectionButton.isEnabled = setting.isEnabled;
        Dropdown.ResetButton.isEnabled = setting.isEnabled;
        Dropdown.HoverButton.isEnabled = setting.isEnabled;
    }
}
