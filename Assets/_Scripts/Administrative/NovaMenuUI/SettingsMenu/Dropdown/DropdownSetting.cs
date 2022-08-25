using System;
using System.Collections.Generic;
using System.Linq;
using SuperspectiveUtils;
using UnityEngine;

// Assumes dropdown selection is exactly one, create a different Setting if otherwise
public class DropdownSetting : Setting {
    public string Name;

    public List<DropdownOption> AllDropdownItems;
    public int SelectedIndex;
    public int DefaultIndex;

    public DropdownOption SelectedValue => AllDropdownItems[SelectedIndex];
    public string SelectedValueName => SelectedValue.DisplayName;
    
    public static DropdownSetting Copy(DropdownSetting from) {
        List<DropdownOption> DeepCopyList(List<DropdownOption> from) {
            List<DropdownOption> result = new List<DropdownOption>();
            foreach (var item in from) {
                result.Add(item);
            }

            return result;
        }
        
        return new DropdownSetting() {
            Key = from.Key,
            Name = from.Name,
            AllDropdownItems = DeepCopyList(from.AllDropdownItems),
            SelectedIndex = from.SelectedIndex,
            DefaultIndex = from.DefaultIndex
        };
    }

    public override bool IsEqual(Setting otherSetting) {
        if (otherSetting is not DropdownSetting other) return false;

        bool allDropdownItemsEqual = true;
        // NOTE: Only additions/removals from dropdown items is supported, can't change existing ones
        for (int i = 0; i < AllDropdownItems.Count; i++) {
            DropdownOption thisOption = AllDropdownItems[i];
            DropdownOption otherOption = other.AllDropdownItems[i];

            bool isSameOption = thisOption.DisplayName == otherOption.DisplayName;

            if (!isSameOption) {
                allDropdownItemsEqual = false;
                break;
            }
        }

        bool selectedIndexEqual = SelectedIndex == other.SelectedIndex;
        bool defaultValueEqual = DefaultIndex == other.DefaultIndex;

        return Name == other.Name &&
               allDropdownItemsEqual &&
               selectedIndexEqual &&
               defaultValueEqual;
    }

    public override void CopySettingsFrom(Setting otherSetting) {
        if (otherSetting is not DropdownSetting other) {
            Debug.LogError($"{otherSetting} is not a DropdownSetting");
            return;
        }
        
        AllDropdownItems.Clear();
        foreach (var dropdownItem in other.AllDropdownItems) {
            DropdownOption newOption = new DropdownOption() {
                Datum = dropdownItem.Datum,
                DisplayName = dropdownItem.DisplayName
            };
            
            AllDropdownItems.Add(newOption);
        }

        Name = other.Name;
        SelectedIndex = other.SelectedIndex;
        DefaultIndex = other.DefaultIndex;
    }
}
