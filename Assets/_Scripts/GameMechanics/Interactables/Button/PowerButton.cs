using System;
using System.Collections;
using System.Collections.Generic;
using PowerTrailMechanics;
using Saving;
using UnityEngine;

public class PowerButton : SaveableObject<PowerButton, PowerButton.PowerButtonSave> {
    public Button button;
    public PowerState powerState;

    public bool powerIsOn;
    
    
    public delegate void PowerAction();

    public event PowerAction OnPowerStart;
    public event PowerAction OnPowerFinish;
    public event PowerAction OnDepowerStart;
    public event PowerAction OnDepowerFinish;

    protected override void OnValidate() {
        base.OnValidate();
        
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
        if (powerState == PowerState.Depowered) {
            powerState = PowerState.PartiallyPowered;
        }
        else if (powerState == PowerState.Powered) {
            powerState = PowerState.PartiallyPowered;
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
            powerState = PowerState.Powered;
            OnPowerFinish?.Invoke();
        }
        else {
            powerState = PowerState.Depowered;
            OnDepowerFinish?.Invoke();
        }
    }
    #endregion
    
    #region On/Off mode

    void HandleOnOffButtonPressBegin(Button button) {
        powerIsOn = true;
        powerState = PowerState.PartiallyPowered;
        OnPowerStart?.Invoke();
    }

    void HandleOnOffButtonPressFinish(Button button) {
        powerState = PowerState.Powered;
        OnPowerFinish?.Invoke();
    }

    void HandleOnOffButtonUnpressBegin(Button button) {
        powerIsOn = false;
        powerState = PowerState.PartiallyPowered;
        OnDepowerStart?.Invoke();
    }

    void HandleOnOffButtonUnpressFinish(Button button) {
        powerState = PowerState.Depowered;
        OnDepowerFinish?.Invoke();
    }
    
    #endregion
    
    #region Saving
    
    [Serializable]
    public class PowerButtonSave : SerializableSaveObject<PowerButton> {
        private PowerState powerState;
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
