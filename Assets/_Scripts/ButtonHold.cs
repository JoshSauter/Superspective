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

    protected override void UpdateButton() {
        if (timeSinceStateChange == 0 && state == State.ButtonPressing) buttonHeld = true;

        timeSinceStateChange += Time.deltaTime;
        switch (state) {
            case State.ButtonDepressed:
                break;
            case State.ButtonPressed:
                if (depressAfterPress && timeSinceStateChange > timeBetweenPressEndDepressStart || !buttonHeld)
                    state = State.ButtonDepressing;
                break;
            case State.ButtonPressing:
                if (timeSinceStateChange < timeToPressButton) {
                    float t = timeSinceStateChange / timeToPressButton;

                    transform.position = Vector3.Lerp(depressedPos, pressedPos, buttonPressCurve.Evaluate(t));
                }
                else {
                    transform.position = pressedPos;
                    state = State.ButtonPressed;
                }

                break;
            case State.ButtonDepressing:
                buttonHeld = false;
                if (timeSinceStateChange < timeToDepressButton) {
                    float t = timeSinceStateChange / timeToDepressButton;

                    transform.position = Vector3.Lerp(pressedPos, depressedPos, buttonDepressCurve.Evaluate(t));
                }
                else {
                    transform.position = pressedPos;
                    state = State.ButtonDepressed;
                }

                break;
        }
    }

    protected void TriggerButtonHeldEvents() {
        if (OnButtonHeld != null) OnButtonHeld(this);
    }
}