using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Nova;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Serialization;

public class Dropdown : UIControl<DropdownVisuals> {
    private const float dropdownCloseDelay = 0.15f;
    
    public ItemView ItemView;

    public TextBlock Name;
    public NovaButton SelectionButton;
    public TextBlock SelectionLabel;
    public UIBlock DropdownOptionsArea;
    public ListView DropdownOptionsListView;
    public NovaButton ResetButton;
    public Scroller Scroller;
    public UIBlock2D DisabledOverlay;

    public enum DropdownState {
        Closed,
        Open
    }
    // Initialized open, immediately set to Closed at start
    public StateMachine<DropdownState> state = new StateMachine<DropdownState>(DropdownState.Open, false, true);

    public DropdownSetting setting;

    [ShowNativeProperty]
    public Selection<string, DropdownOption> selectionForInspector => setting?.dropdownSelection ?? new Selection<string, DropdownOption>();

    // Start is called before the first frame update
    void Awake() {
        DropdownOptionsListView.AddDataBinder<DropdownOption, DropdownOptionVisuals>(BindDropdownOption);

        SelectionButton.OnClick += (_) => {
            state.Set(state == DropdownState.Closed ? DropdownState.Open : DropdownState.Closed);
        };
        DropdownOptionsListView.AddGestureHandler<Gesture.OnClick, DropdownOptionVisuals>(HandleOptionClicked);
        DropdownOptionsListView.AddGestureHandler<Gesture.OnHover, DropdownOptionVisuals>(HandleOptionHovered);
        DropdownOptionsListView.AddGestureHandler<Gesture.OnUnhover, DropdownOptionVisuals>(HandleOptionUnhovered);
        
        state.OnStateChangeSimple += () => Debug.Log($"Setting Dropdown state to: {state.state}");

        ResetButton.OnClick += (_) => UpdateVisuals(setting.defaultSelection);
        
        //NovaUIBackground.instance.BackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnClick>(HandleBGClicked);

        InitStateMachine();
    }

    private void Update() {
        if (PlayerButtonInput.instance.PausePressed && state == DropdownState.Open) {
            StartCoroutine(CloseDropdown(state));
        }
    }

    public void UpdateVisuals(Dictionary<string, DropdownOption> newSelections) {
        Debug.Log($"UpdateVisuals: {string.Join(", ", newSelections.Keys)}");
        SelectionLabel.Text = setting.SelectedValueName;
        DropdownOptionsListView.Refresh();
    }

    IEnumerator CloseDropdown(DropdownState stateWhenClicked) {
        yield return new WaitForSecondsRealtime(dropdownCloseDelay);
        //Debug.LogError("Dropdown.CloseDropdown: " + state.state);
        if (stateWhenClicked == DropdownState.Open) {
            state.Set(DropdownState.Closed);
        }
    }

    private void InitStateMachine() {
        state.AddTrigger(DropdownState.Closed, () => {
            DropdownOptionsArea.gameObject.SetActive(false);
        });
        state.AddTrigger(DropdownState.Open, () => {
            DropdownOptionsArea.gameObject.SetActive(true);
            DropdownOptionsListView.Refresh();
        });
    }

    private void Start() {
        state.Set(DropdownState.Closed);
    }

    private void SetSelectionFromButtonText(NovaButton buttonClicked) {
        if (state == DropdownState.Closed) return;
        
        foreach (DropdownOption dropdownOption in setting.dropdownSelection.allItems.Where(item => item.DisplayName == buttonClicked.Text.Get())) {
            setting.dropdownSelection.Select(dropdownOption.DisplayName, dropdownOption);
            if (setting.dropdownSelection.type is SelectionType.ExactlyOne or SelectionType.ZeroOrOne) {
                state.Set(state == DropdownState.Closed ? DropdownState.Open : DropdownState.Closed);
            }
        }
    }

    private void BindDropdownOption(Data.OnBind<DropdownOption> evt, DropdownOptionVisuals target, int index) {
        target.name.Text = evt.UserData.DisplayName;
        target.View.gameObject.name = $"{target.name.Text} Option";
        Debug.Log($"Bind {target.View.gameObject.name}");

        if (setting.dropdownSelection.allSelections.ContainsKey(evt.UserData.DisplayName)) {
            target.background.Color = UIStyle.NovaButton.ClickedBgColor;
            target.name.Color = UIStyle.NovaButton.ClickedComponentColor;
        }
        else {
            target.background.Color = UIStyle.NovaButton.DefaultBgColor;
            target.name.Color = UIStyle.NovaButton.DefaultComponentColor;
        }
    }

    public void SetDatasource() {
        DropdownOptionsListView.SetDataSource(setting.dropdownSelection.allItems);
    }

    private void HandleOptionUnhovered(Gesture.OnUnhover evt, DropdownOptionVisuals target, int index) {
        if (!setting.dropdownSelection.allSelections.ContainsKey(setting.dropdownSelection.allItems[index].DisplayName)) {
            target.background.Color = UIStyle.NovaButton.DefaultBgColor;
            target.name.Color = UIStyle.NovaButton.DefaultComponentColor;
        }
    }

    private void HandleOptionHovered(Gesture.OnHover evt, DropdownOptionVisuals target, int index) {
        if (!setting.dropdownSelection.allSelections.ContainsKey(setting.dropdownSelection.allItems[index].DisplayName)) {
            target.background.Color = UIStyle.NovaButton.HoverBgColor;
            target.name.Color = UIStyle.NovaButton.DefaultComponentColor;
        }
    }

    private void HandleOptionClicked(Gesture.OnClick evt, DropdownOptionVisuals target, int index) {
        var selection = setting.dropdownSelection.allItems[index];
        setting.dropdownSelection.Select(selection.DisplayName, selection);
        if (setting.dropdownSelection.type is SelectionType.ExactlyOne or SelectionType.ZeroOrOne) {
            StartCoroutine(CloseDropdown(state));
        }
    }
}