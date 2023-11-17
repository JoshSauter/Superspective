using System;
using Interactables;
using UnityEngine;

public class ButtonHold : Button {
    bool buttonHeld;

    protected override void Awake() {
        base.Awake();

        interactableObject.OnLeftMouseButtonUp += ReleaseButton;
        interactableObject.OnMouseHoverExit += ReleaseButton;
    }

#region events
    public event ButtonAction OnButtonHeld;
#endregion

    protected override void InitializeStateMachine() {
        base.InitializeStateMachine();
        stateMachine.AddStateTransition(State.ButtonPressed, State.ButtonUnpressing, () => !buttonHeld);
    }
    
    protected override void Update() {
        base.Update();

        UpdateState();
        if (buttonHeld) {
            OnButtonHeld?.Invoke(this);
        }
    }

    void UpdateState() {
        if (stateMachine.timeSinceStateChanged == 0 && stateMachine.state == State.ButtonPressing) buttonHeld = true;

        if (buttonHeld) {
            OnButtonHeld?.Invoke(this);
        }
    }

    public void ReleaseButton() {
        buttonHeld = false;
    }
}
