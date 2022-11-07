using System;
using System.Collections.Generic;
using System.Linq;
using SuperspectiveUtils;
using UnityEngine;

public enum SelectionType {
    ZeroOrOne,
    ZeroOrMore,
    ExactlyOne,
    OneOrMore
}

public class DropdownSetting : Setting {
    public string name;

    public Selection<string, DropdownOption> dropdownSelection;
    public Dictionary<string, DropdownOption> defaultSelection = new Dictionary<string, DropdownOption>();

    private const string NoneSelected = "None Selected";

    private DropdownSetting() {}

    public static DropdownSetting Of(string key, string name, List<DropdownOption> allDropdownItems, int defaultIndex, int selectedIndex) {
        DropdownSetting setting = new DropdownSetting();
        setting.Init(
            type: SelectionType.ExactlyOne,
            key: key,
            isEnabled: true,
            name: name,
            allItems: allDropdownItems,
            allDefaults: new Dictionary<string, DropdownOption>() {{ allDropdownItems[defaultIndex].DisplayName, allDropdownItems[defaultIndex] }},
            allSelections: new Dictionary<string, DropdownOption>() {{ allDropdownItems[selectedIndex].DisplayName, allDropdownItems[selectedIndex] }});
        return setting;
    }
    
    public static DropdownSetting Of(SelectionType type, string key, bool isEnabled, string name, List<DropdownOption> allDropdownItems, int defaultIndex, int selectedIndex) {
        DropdownSetting setting = new DropdownSetting();
        setting.Init(
            type: type,
            key: key,
            isEnabled: true,
            name: name,
            allItems: allDropdownItems,
            allDefaults: new Dictionary<string, DropdownOption>() {{ allDropdownItems[defaultIndex].DisplayName, allDropdownItems[defaultIndex] }},
            allSelections: new Dictionary<string, DropdownOption>() {{ allDropdownItems[selectedIndex].DisplayName, allDropdownItems[selectedIndex] }});
        return setting;
    }
    
    public static DropdownSetting Of(SelectionType type, string key, string name, List<DropdownOption> allDropdownItems, Option<int> defaultIndexOpt, Option<int> selectedIndexOpt) {
        DropdownSetting setting = new DropdownSetting();
        var defaults = new Dictionary<string, DropdownOption>();
        defaultIndexOpt.ForEach(index => defaults.Add(allDropdownItems[index].DisplayName, allDropdownItems[index]));

        var selections = new Dictionary<string, DropdownOption>();
        selectedIndexOpt.ForEach(index => selections.Add(allDropdownItems[index].DisplayName, allDropdownItems[index]));
        setting.Init(
            type: type,
            key: key,
            isEnabled: true,
            name: name,
            allItems: allDropdownItems,
            allDefaults: defaults,
            allSelections: selections);
        return setting;
    }

    private void Init(
        SelectionType type = SelectionType.ExactlyOne,
        string key = "",
        bool isEnabled = true,
        string name = "",
        List<DropdownOption> allItems = null,
        Dictionary<string, DropdownOption> allDefaults = null,
        Dictionary<string, DropdownOption> allSelections = null) {
        this.key = key;
        this.isEnabled = isEnabled;
        this.name = name;
        this.dropdownSelection = new Selection<string, DropdownOption>() {
            type = type,
            allItems = allItems,
            allSelections = allSelections ?? new Dictionary<string, DropdownOption>()
        };
        this.defaultSelection = allDefaults;
    }
    
    public string SelectedValueName {
        get {
            switch (dropdownSelection.type) {
                case SelectionType.ZeroOrOne:
                    return dropdownSelection.selectionOpt.Map(sv => sv.DisplayName).GetOrElse(NoneSelected);
                case SelectionType.ZeroOrMore:
                    if (dropdownSelection.allSelections.Count > 0) {
                        return string.Join(", ", dropdownSelection.allSelections.Values.Select(sv => sv.DisplayName));
                    }
                    else {
                        return NoneSelected;
                    }
                case SelectionType.ExactlyOne:
                    return dropdownSelection.selection.DisplayName;
                case SelectionType.OneOrMore:
                    return string.Join(", ", dropdownSelection.allSelections.Values.Select(sv => sv.DisplayName));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public int DefaultIndex {
        set => defaultSelection = new Dictionary<string, DropdownOption>() {{ dropdownSelection.allItems[value].DisplayName, dropdownSelection.allItems[value] }};
    }

    public static DropdownSetting Copy(DropdownSetting from) {
        List<T> DeepCopyList<T>(List<T> from) {
            List<T> result = new List<T>();
            foreach (var item in from) {
                result.Add(item);
            }

            return result;
        }

        DropdownSetting newSetting = new DropdownSetting();
        newSetting.Init(
            type: from.dropdownSelection.type,
            key: from.key,
            isEnabled: from.isEnabled,
            name: from.name,
            allItems: DeepCopyList(from.dropdownSelection.allItems),
            allSelections: DeepCopyList(from.dropdownSelection.allSelections.ToList()).ToDictionary(),
            allDefaults: DeepCopyList(from.defaultSelection.ToList()).ToDictionary());
        return newSetting;
    }

    public override bool IsEqual(Setting otherSetting) {
        if (otherSetting is not DropdownSetting other) return false;

        bool allDropdownItemsEqual = dropdownSelection.allItems.Count == other.dropdownSelection.allItems.Count;
        allDropdownItemsEqual = allDropdownItemsEqual && dropdownSelection.type == other.dropdownSelection.type;

        if (allDropdownItemsEqual) {
            // NOTE: Only additions/removals from dropdown items is supported, can't change existing ones
            for (int i = 0; i < dropdownSelection.allItems.Count; i++) {
                DropdownOption thisOption = dropdownSelection.allItems[i];
                DropdownOption otherOption = other.dropdownSelection.allItems[i];

                bool isSameOption = thisOption.DisplayName == otherOption.DisplayName;

                if (!isSameOption) {
                    allDropdownItemsEqual = false;
                    break;
                }
            }
        }

        bool selectedIndexesEqual = dropdownSelection.allSelections.Keys.ToHashSet().SetEquals(other.dropdownSelection.allSelections.Keys.ToHashSet());
        bool defaultValuesEqual = defaultSelection.Keys.ToHashSet().SetEquals(other.defaultSelection.Keys.ToHashSet());

        return name == other.name &&
               isEnabled == other.isEnabled &&
               allDropdownItemsEqual &&
               selectedIndexesEqual &&
               defaultValuesEqual;
    }

    public override void CopySettingsFrom(Setting otherSetting) {
        void CopyCollection<T, S>(T copyTo, T copyFrom, Func<S, S> copy) where T : IEnumerable<S>, ICollection<S> {
            copyTo.Clear();
            foreach (var copyItem in copyFrom) {
                copyTo.Add(copy(copyItem));
            }
        }
        
        if (otherSetting is not DropdownSetting other) {
            Debug.LogError($"{otherSetting} is not a DropdownSetting");
            return;
        }
        
        CopyCollection<List<DropdownOption>, DropdownOption>(dropdownSelection.allItems, other.dropdownSelection.allItems, (item) => {
            DropdownOption newOption = new DropdownOption() {
                Datum = item.Datum,
                DisplayName = item.DisplayName
            };

            return newOption;
        });
        CopyCollection<Dictionary<string, DropdownOption>, KeyValuePair<string, DropdownOption>>(dropdownSelection.allSelections, other.dropdownSelection.allSelections, (item) => {
            DropdownOption newOption = new DropdownOption() {
                Datum = item.Value.Datum,
                DisplayName = item.Value.DisplayName
            };

            return new KeyValuePair<string, DropdownOption>(item.Key, newOption);
        });
        CopyCollection<Dictionary<string, DropdownOption>, KeyValuePair<string, DropdownOption>>(defaultSelection, other.defaultSelection, (item) => { 
            DropdownOption newOption = new DropdownOption() {
                Datum = item.Value.Datum,
                DisplayName = item.Value.DisplayName
            };

            return new KeyValuePair<string, DropdownOption>(item.Key, newOption);
        });

        name = other.name;
        isEnabled = other.isEnabled;
    }

    public override string PrintValue() {
        return SelectedValueName;
    }
}