using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using StateUtils;
using UnityEngine;

public class Dropdown : UIControl<DropdownVisuals> {
    public ItemView ItemView;

    public TextBlock Name;
    public NovaButton SelectionButton;
    public TextBlock SelectionLabel;
    public UIBlock DropdownOptionsArea;
    public ListView DropdownOptionsListView;
    public NovaRadioSelection DropdownOptionsRadioSelection;
    public NovaButton ResetButton;
    public Scroller Scroller;

    public enum DropdownState {
        Closed,
        Open
    }
    // Initialized open, immediately set to Closed at start
    public StateMachine<DropdownState> state = new StateMachine<DropdownState>(DropdownState.Open, false, true);

    public DropdownSetting setting;

    // Start is called before the first frame update
    void Awake() {
        DropdownOptionsListView.AddDataBinder<DropdownOption, DropdownOptionVisuals>(BindDropdownDatum);

        SelectionButton.OnClick += (_) => {
            state.Set(state == DropdownState.Closed ? DropdownState.Open : DropdownState.Closed);
            Debug.Log($"Setting Dropdown state to: {state.state}");
        };

        DropdownOptionsRadioSelection.OnRadioSelectionChanged += (indexOfSelection) => {
            if (indexOfSelection >= 0) {
                NovaButton selection = DropdownOptionsRadioSelection.children[indexOfSelection];
                
                if (selection.GetComponent<ItemView>().TryGetVisuals(out DropdownOptionVisuals optionVisuals)) {
                    UpdateValue(setting.AllDropdownItems.FindIndex(i => i.DisplayName == optionVisuals.Name.Text));
                }
            }
        };

        ResetButton.OnClick += (_) => UpdateValue(setting.DefaultIndex);
        
        NovaUIBackground.instance.BackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnClick>(HandleBGClicked);

        InitStateMachine();
    }

    private void HandleBGClicked(Gesture.OnClick evt) {
        // TODO: Consume events from NovaButton script when that feature's released
        if (state == DropdownState.Open) {
            state.Set(DropdownState.Closed);
        }
    }

    void UpdateValue(int newSelectedIndex) {
        Debug.Log($"UpdateValue: {newSelectedIndex}");
        setting.SelectedIndex = newSelectedIndex;
        Visuals.PopulateFrom(setting);
    }

    private void InitStateMachine() {
        state.AddTrigger(DropdownState.Closed, () => {
            DropdownOptionsRadioSelection.Teardown();
            DropdownOptionsArea.gameObject.SetActive(false);
        });
        state.AddTrigger(DropdownState.Open, () => {
            DropdownOptionsArea.gameObject.SetActive(true);
            DropdownOptionsRadioSelection.Init();
        });
    }

    private void Update() {
        if (!DropdownOptionsArea.gameObject.activeSelf) return;
        
        if (DropdownOptionsRadioSelection.TryFindIndex(setting.SelectedValue.DisplayName, out int indexOfMatch)) {
            DropdownOptionsRadioSelection.SetSelection(indexOfMatch, false);
        }
    }

    private void Start() {
        state.Set(DropdownState.Closed);
    }

    private void BindDropdownDatum(Data.OnBind<DropdownOption> evt, DropdownOptionVisuals target, int index) {
        target.Name.Text = evt.UserData.DisplayName;

        if (index == setting.SelectedIndex) {
            target.Background.Color = UIStyle.NovaButton.ClickedBgColor;
            target.Name.Color = UIStyle.NovaButton.ClickedComponentColor;
        }
        else {
            target.Background.Color = UIStyle.NovaButton.DefaultBgColor;
            target.Name.Color = UIStyle.NovaButton.DefaultComponentColor;
        }
    }
}
