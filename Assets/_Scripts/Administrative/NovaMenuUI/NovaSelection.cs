using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

// For selecting out of a list of Interactable UIBlocks
public class NovaSelection : MonoBehaviour {
    public Selection<string, NovaButton> selection = new Selection<string, NovaButton>();
    private const string NoneSelected = "None Selected";

    [ShowNativeProperty]
    public string SelectedValueName {
        get {
            if (selection == null || selection.selection == null) return NoneSelected;
            switch (selection.type) {
                case SelectionType.ZeroOrOne:
                    return selection.selectionOpt.FlatMap(sv => sv.Text).GetOrElse(NoneSelected);
                case SelectionType.ZeroOrMore:
                    if (selection.allSelections.Count > 0) {
                        return string.Join(", ", selection.allSelections.Values.Select(sv => sv.Text.Get()));
                    }
                    else {
                        return NoneSelected;
                    }
                case SelectionType.ExactlyOne:
                    return selection.selection.Text.Get();
                case SelectionType.OneOrMore:
                    return string.Join(", ", selection.allSelections.Values.Select(sv => sv.Text.Get()));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void Awake() {
        Init();
    }

    public void Init() {
        selection.allItems = GetComponentsInChildren<NovaButton>().OrderBy(b => b.transform.GetSiblingIndex()).ToList();

        foreach (var child in selection.allItems) {
            child.OnClick += HandleButtonClicked;
        }

        selection.OnSelectionChanged += HandleSelectionChanged;
    }

    public void Teardown() {
        //Debug.LogWarning($"{gameObject.FullPath()}.Teardown");
        foreach (var child in selection.allItems) {
            child.OnClick -= HandleButtonClicked;
        }
    }

    private void OnDisable() {
        Teardown();
    }

    private void Update() {
        switch (selection.type) {
            case SelectionType.ZeroOrOne:
            case SelectionType.ZeroOrMore:
                break;
            case SelectionType.ExactlyOne:
            case SelectionType.OneOrMore:
                if (!selection.hasSelection && selection.allItems.Count > 0) {
                    HandleButtonClicked(selection.allItems.ToList()[0]);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleButtonClicked(NovaButton clickedButton) {
        selection.Select(clickedButton.Text.Get(), clickedButton);
    }

    private void HandleSelectionChanged(Dictionary<string, NovaButton> prevSelections, Dictionary<string, NovaButton> newSelections) {
        var selectedButtons = newSelections.Values.ToList();
        var unselectedButtons = selection.allItems.Where(item => !newSelections.ContainsKey(item.Text.Get())).ToList();
        unselectedButtons.ForEach(b => {
            if (b.clickState != NovaButton.ClickState.Idle) {
                b.clickState.Set(NovaButton.ClickState.Idle);
            }
        });
        selectedButtons.ForEach(b => {
            if (b.clickState != NovaButton.ClickState.Clicked) {
                b.clickState.Set(NovaButton.ClickState.Clicked);
            }
        });
    }
}
