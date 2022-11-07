using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using SuperspectiveUtils;
using UnityEngine;

[Serializable]
// K is the type of the key of the selection options (e.g. key by DropdownOption display name string instead of object reference)
// T is the type of the selection options
public class Selection<K, T> {
    public SelectionType type = SelectionType.ExactlyOne;

    public List<T> allItems;
    [ShowNativeProperty]
    public bool hasSelection => selectionOpt.IsDefined();

    public Dictionary<K, T> allSelections = new Dictionary<K, T>();
    public Option<T> selectionOpt => allSelections.Count > 0 ? Option<T>.Of(allSelections.Values.ToArray()[0]) : new None<T>();
    public T selection => hasSelection ? selectionOpt.Get() : default(T);

    public delegate void SelectionChangedAction(Dictionary<K, T> prevSelection, Dictionary<K, T> newSelection);
    public SelectionChangedAction OnSelectionChanged;

    public void Select(K newSelectionKey, T newSelection, bool triggerEvents = true) {
        switch (type) {
            case SelectionType.ZeroOrOne:
                if (hasSelection && allSelections.ContainsKey(newSelectionKey)) {
                    SetSelection(new Dictionary<K, T>() {{ newSelectionKey, newSelection }}, triggerEvents);
                }
                else {
                    SetSelection(new Dictionary<K, T>() {{newSelectionKey, newSelection}}, triggerEvents);
                }
                break;
            case SelectionType.ZeroOrMore:
                if (allSelections.ContainsKey(newSelectionKey)) {
                    Dictionary<K, T> newSelections = new Dictionary<K, T>(allSelections);
                    newSelections.Remove(newSelectionKey);
                    SetSelection(newSelections, triggerEvents);
                }
                else {
                    Dictionary<K, T> newSelections = new Dictionary<K, T>(allSelections);
                    newSelections.Add(newSelectionKey, newSelection);
                    SetSelection(newSelections, triggerEvents);
                }
                break;
            case SelectionType.ExactlyOne:
                SetSelection(new Dictionary<K, T>() {{newSelectionKey, newSelection}}, triggerEvents);
                break;
            case SelectionType.OneOrMore:
                if (allSelections.ContainsKey(newSelectionKey)) {
                    Dictionary<K, T> newSelections = new Dictionary<K, T>(allSelections);
                    if (allSelections.Count > 1) {
                        newSelections.Remove(newSelectionKey);
                    }
                    SetSelection(newSelections, triggerEvents);
                }
                else {
                    Dictionary<K, T> newSelections = new Dictionary<K, T>(allSelections);
                    newSelections.Add(newSelectionKey, newSelection);
                    SetSelection(newSelections, triggerEvents);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void SetSelection(Dictionary<K, T> newSelections, bool triggerEvents = true) {
        var prevKeys = allSelections.Keys.ToHashSet();
        var newKeys = newSelections.Keys.ToHashSet();
        if (prevKeys.SetEquals(newKeys)) return;
        
        var prevSelection = new Dictionary<K, T>(allSelections);
        allSelections = newSelections;
        if (triggerEvents) {
            OnSelectionChanged?.Invoke(prevSelection, newSelections);
        }
    }
}
