using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

// For selecting at most one out of a list of Interactable UIBlocks
public class NovaRadioSelection : MonoBehaviour {
    [Tooltip("If selected, one option will always be selected no matter what")]
    public bool exactlyOneSelection = false;
    
    public List<NovaButton> children;
    [ShowNonSerializedField]
    [NonSerialized]
    private int selectedIndex = -1;

    public bool hasSelection => (selectedIndex >= 0 && selectedIndex < children.Count);
    public NovaButton selection => hasSelection ? children[selectedIndex] : null;

    public delegate void RadioSelectionEvent(int indexOfSelected);
    public event RadioSelectionEvent OnRadioSelectionChanged;
    
    private void Awake() {
        Init();
    }

    public void Init() {
        //Debug.LogWarning($"{gameObject.FullPath()}.Init");
        children = GetComponentsInChildren<NovaButton>().OrderBy(b => b.transform.GetSiblingIndex()).ToList();

        foreach (var child in children) {
            child.OnClick += UnclickOtherButtons;
        }

        UnclickOtherButtons(selection);
    }

    public void Teardown() {
        //Debug.LogWarning($"{gameObject.FullPath()}.Teardown");
        foreach (var child in children) {
            child.OnClick -= UnclickOtherButtons;
        }
    }

    private void OnDisable() {
        Teardown();
    }

    private void Update() {
        if (selection == null && exactlyOneSelection) {
            SetSelection(0);
        }
        else if (hasSelection && !selection.gameObject.activeSelf) {
            SetSelection(exactlyOneSelection ? 0 : -1);
        }
    }

    void UnclickOtherButtons(NovaButton clickedButton) {
        int indexOfSelection = -1;
        for (int i = 0; i < children.Count; i++) {
            var child = children[i];
            if (child == clickedButton) {
                indexOfSelection = i;
                continue;
            }
            
            child.buttonState.Set(NovaButton.ButtonState.Idle);
        }

        if (selection != clickedButton || clickedButton == null) {
            SetSelection(indexOfSelection);
        }
    }

    public void SetSelection(int indexOfSelection, bool invokeEvents = true) {
        if (indexOfSelection < 0 || indexOfSelection >= children.Count) {
            if (exactlyOneSelection) {
                return;
            }
            else {
                indexOfSelection = -1;
            }
        }

        int prevIndex = selectedIndex;
        
        //Debug.Log($"Switching to {(hasSelection ? selection.Text : "(Nothing selected)")}");
        selectedIndex = indexOfSelection;
        if (hasSelection && selection.buttonState != NovaButton.ButtonState.Clicked) {
            selection.buttonState.Set(NovaButton.ButtonState.Clicked);
        }

        if (invokeEvents && selectedIndex != prevIndex) {
            OnRadioSelectionChanged?.Invoke(indexOfSelection);
        }
    }
    
    public bool TryFindIndex(string displayName, out int index) {
        for (int i = 0; i < children.Count; i++) {
            if (children[i].TextBlock.Get().Text == displayName) {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }
}
