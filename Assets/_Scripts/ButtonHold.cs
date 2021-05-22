using System;
using UnityEngine;

public class ButtonHold : Button {
    bool buttonHeld;

    protected override void Awake() {
        base.Awake();

        interactableObject.OnLeftMouseButtonUp += () => buttonHeld = false;
        interactableObject.OnMouseHoverExit += () => buttonHeld = false;
    }

#region events
    public event ButtonAction OnButtonHeld;
#endregion

    protected override void UpdateState() {
        if (timeSinceStateChange == 0 && state == State.ButtonPressing) buttonHeld = true;
        
        switch (state) {
            case State.ButtonUnpressed:
                break;
            case State.ButtonPressing:
                if (timeSinceStateChange > timeToPressButton) {
                    state = State.ButtonPressed;
                }
                break;
            case State.ButtonPressed:
                if (unpressAfterPress && timeSinceStateChange > timeBetweenPressEndDepressStart || !buttonHeld) {
                    state = State.ButtonUnpressing;
                }
                break;
            case State.ButtonUnpressing:
                if (timeSinceStateChange > timeToUnpressButton) {
                    state = State.ButtonUnpressed;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (buttonHeld) {
            OnButtonHeld?.Invoke(this);
        }
    }
}