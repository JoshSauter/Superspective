using System;
using Interactables;
using SuperspectiveUtils;
using UnityEngine;

public class ButtonHold : Button {
    [SerializeField]
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
        stateMachine.AddTrigger(State.ButtonPressing, () => buttonHeld = true);
    }
    
    protected override void Update() {
        base.Update();

        if (buttonHeld) {
            OnButtonHeld?.Invoke(this);
        }
    }

    public void ReleaseButton() {
        debug.Log($"Releasing button");
        buttonHeld = false;
    }
}
