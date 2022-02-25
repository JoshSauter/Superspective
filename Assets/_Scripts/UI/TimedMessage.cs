using System;
using System.Collections;
using System.Collections.Generic;
using StateUtils;
using SuperspectiveUtils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimedMessage : MonoBehaviour {
    public TextMeshProUGUI text;
    public Image background;

    public float timeToDisplay = 3f;

    public float fadeInTime = 1f;
    public float fadeOutTime = 3f;

    private float alpha {
        get => text.color.a;
        set {
            text.color = new Color(text.color.r, text.color.g, text.color.b, value);
            background.color = new Color(background.color.r, background.color.g, background.color.b, value*0.95f);
        }
    }

    enum State {
        Off,
        FadingIn,
        Displayed,
        FadingOut
    }

    private readonly StateMachine<State> state = new StateMachine<State>(State.Off);

    private void OnTriggerEnter(Collider other) {
        if (other.TaggedAsPlayer()) {
            ShowMessage();
            GetComponent<Collider>().enabled = false;
        }
    }

    // Start is called before the first frame update
    void Start() {
        state.AddStateTransition(State.FadingIn, State.Displayed, fadeInTime);
        state.AddStateTransition(State.Displayed, State.FadingOut, timeToDisplay);
        state.AddStateTransition(State.FadingOut, State.Off, fadeOutTime);

        state.AddTrigger(State.Off, 0f, () => alpha = 0);
        state.AddTrigger(State.Displayed, 0f, () => alpha = 1);
    }

    // Update is called once per frame
    void Update() {
        switch (state.state) {
            case State.Off:
            case State.Displayed:
                break;
            case State.FadingIn:
                alpha = Mathf.Lerp(0f, 1f, state.timeSinceStateChanged / fadeInTime);
                break;
            case State.FadingOut:
                alpha = Mathf.Lerp(1, 0, state.timeSinceStateChanged / fadeOutTime);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ShowMessage() {
        if (state == State.Off) {
            state.Set(State.FadingIn);
        }
    }
}
