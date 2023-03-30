using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using StateUtils;
using SuperspectiveUtils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimedMessage : MonoBehaviour {
    public TextMeshProUGUI[] text;
    public Image[] backgrounds;

    public bool displayForever = false;
    [HideIf("displayForever")]
    public float timeToDisplay = 3f;

    public float fadeInTime = 1f;
    public float fadeOutTime = 3f;

    private float alpha {
        get => text[0].color.a;
        set {
            foreach (var txt in text) {
                txt.color = txt.color.WithAlpha(value);
            }
            foreach (var background in backgrounds) {
                background.color = background.color.WithAlpha(value);
            }
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
        if (!displayForever) {
            state.AddStateTransition(State.Displayed, State.FadingOut, timeToDisplay);
            state.AddStateTransition(State.FadingOut, State.Off, fadeOutTime);
        }

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
        if (state == State.Off || state == State.FadingOut) {
            state.Set(State.FadingIn);
        }
    }

    public void ShowMessage(string msg) {
        foreach (var txt in text) {
            txt.text = msg;
        }
        ShowMessage();
    }

    public void HideMessage() {
        if (state == State.Displayed || state == State.FadingIn) {
            state.Set(State.FadingOut);
        }
    }
}
