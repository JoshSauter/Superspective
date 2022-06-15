using System;
using System.Collections;
using System.Collections.Generic;
using PowerTrailMechanics;
using Saving;
using UnityEngine;

public class PowerButton : SaveableObject<PowerButton, PowerButton.PowerButtonSave> {
    public Button button;
    public PowerTrailState powerState;

    public bool powerIsOn;
    
    
    public delegate void PowerAction();

    public event PowerAction OnPowerStart;
    public event PowerAction OnPowerFinish;
    public event PowerAction OnDepowerStart;
    public event PowerAction OnDepowerFinish;

    void OnValidate() {
        if (button == null) {
            button = GetComponent<Button>();
        }
    }
    
    // Start is called before the first frame update
    protected override void Start() {
        base.Start();

        if (button == null) {
            button = GetComponent<Button>();
        }

        if (button == null) {
            Debug.LogError("No button set/found on object");
            this.enabled = false;
            return;
        }
    }

    private void OnDisable() {
        TeardownEvents();
    }

    private void OnEnable() {
        InitEvents();
    }

    void InitEvents() {
        // Toggle mode
        if (button.unpressAfterPress) {
            button.OnButtonPressBegin += HandleToggleButtonPressBegin;
            button.OnButtonPressFinish += HandleToggleButtonPressFinish;
        }
        // On/Off mode
        else {
            button.OnButtonPressBegin += HandleOnOffButtonPressBegin;
            button.OnButtonPressFinish += HandleOnOffButtonPressFinish;
            button.OnButtonUnpressBegin += HandleOnOffButtonUnpressBegin;
            button.OnButtonUnpressFinish += HandleOnOffButtonUnpressFinish;
        }
    }

    void TeardownEvents() {
        // Toggle mode
        if (button.unpressAfterPress) {
            button.OnButtonPressBegin -= HandleToggleButtonPressBegin;
            button.OnButtonPressFinish -= HandleToggleButtonPressFinish;
        }
        // On/Off mode
        else {
            button.OnButtonPressBegin -= HandleOnOffButtonPressBegin;
            button.OnButtonPressFinish -= HandleOnOffButtonPressFinish;
            button.OnButtonUnpressBegin -= HandleOnOffButtonUnpressBegin;
            button.OnButtonUnpressFinish -= HandleOnOffButtonUnpressFinish;
        }
    }

    #region Toggle mode
    void HandleToggleButtonPressBegin(Button button) {
        powerIsOn = !powerIsOn;
        if (powerState == PowerTrailState.Depowered) {
            powerState = PowerTrailState.PartiallyPowered;
        }
        else if (powerState == PowerTrailState.Powered) {
            powerState = PowerTrailState.PartiallyPowered;
        }

        if (powerIsOn) {
            OnPowerStart?.Invoke();
        }
        else {
            OnDepowerStart?.Invoke();
        }
    }
    
    void HandleToggleButtonPressFinish(Button button) {
        if (powerIsOn) {
            powerState = PowerTrailState.Powered;
            OnPowerFinish?.Invoke();
        }
        else {
            powerState = PowerTrailState.Depowered;
            OnDepowerFinish?.Invoke();
        }
    }
    #endregion
    
    #region On/Off mode

    void HandleOnOffButtonPressBegin(Button button) {
        powerIsOn = true;
        powerState = PowerTrailState.PartiallyPowered;
        OnPowerStart?.Invoke();
    }

    void HandleOnOffButtonPressFinish(Button button) {
        powerState = PowerTrailState.Powered;
        OnPowerFinish?.Invoke();
    }

    void HandleOnOffButtonUnpressBegin(Button button) {
        powerIsOn = false;
        powerState = PowerTrailState.PartiallyPowered;
        OnDepowerStart?.Invoke();
    }

    void HandleOnOffButtonUnpressFinish(Button button) {
        powerState = PowerTrailState.Depowered;
        OnDepowerFinish?.Invoke();
    }
    
    #endregion
    
    #region Saving
    
    [Serializable]
    public class PowerButtonSave : SerializableSaveObject<PowerButton> {
        private PowerTrailState powerState;
        private bool powerIsOn;
        
        public PowerButtonSave(PowerButton script) : base(script) {
            this.powerState = script.powerState;
            this.powerIsOn = script.powerIsOn;
        }
        public override void LoadSave(PowerButton script) {
            script.powerState = this.powerState;
            script.powerIsOn = this.powerIsOn;
        }
    }
    #endregion
}
