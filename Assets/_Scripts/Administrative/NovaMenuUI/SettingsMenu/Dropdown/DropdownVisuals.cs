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
        Dropdown.setting = setting;

        Dropdown.gameObject.name = $"{setting.Name} Dropdown";
        Dropdown.Name.Text = setting.Name;
        Dropdown.SelectionLabel.Text = setting.SelectedValueName;

        Dropdown.Scroller.ScrollbarVisual.Visible = setting.AllDropdownItems.Count > minNumberOfItemsForScrollbar;
        
        Dropdown.DropdownOptionsListView.SetDataSource(setting.AllDropdownItems);
        Dropdown.DropdownOptionsRadioSelection.Teardown();
        Dropdown.DropdownOptionsRadioSelection.Init();

        Dropdown.DropdownOptionsRadioSelection.TryFindIndex(setting.SelectedValue.DisplayName, out int indexOfMatch);
        Dropdown.DropdownOptionsRadioSelection.SetSelection(indexOfMatch, false);
    }
}
