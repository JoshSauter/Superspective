using System;
using System.Collections;
using System.Collections.Generic;
using StateUtils;
using UnityEngine;
using UnityEngine.UI;

public class LoadingIcon : Singleton<LoadingIcon> {
    Image icon;

    private float iconFadeInOutTime = .75f;
    private float minIconPresentTime = 1f;
    private AnimationCurve fadeInOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    public enum State {
        Idle,
        FadingIn,
        IconPresent,
        FadingOut
    }
    public StateMachine<State> stateMachine;

    Color clear = new Color(1, 1, 1, 0);

    public bool IsDisplayed => stateMachine != State.Idle;

    public void ShowLoadingIcon() {
        if (stateMachine.state is State.Idle or State.FadingOut) {
            stateMachine.Set(State.FadingIn);
        }
    }

    void Awake() {
        stateMachine = this.StateMachine(State.Idle);
        icon = GetComponent<Image>();

        InitializeStateMachine();
    }

    void InitializeStateMachine() {
        stateMachine.AddStateTransition(State.FadingIn, State.IconPresent, iconFadeInOutTime);
        stateMachine.AddStateTransition(
            State.IconPresent,
            State.FadingOut,
            () => stateMachine.timeSinceStateChanged >= minIconPresentTime && !GameManager.instance.IsCurrentlyLoading
        );
        stateMachine.AddStateTransition(State.FadingOut, State.Idle, iconFadeInOutTime);
        
        stateMachine.AddTrigger(State.FadingIn, () => {
            icon.enabled = true;
            icon.color = new Color(1, 1, 1, Mathf.Max(icon.color.a, .5f));
        });
        
        stateMachine.AddTrigger(State.Idle, () => {
            icon.enabled = false;
            icon.color = clear;
        });
    }

    void Update() {
        switch (stateMachine.state) {
            case State.Idle:
                break;
            case State.FadingOut:
                icon.color = Color.Lerp(icon.color, clear, fadeInOutCurve.Evaluate(stateMachine.timeSinceStateChanged / iconFadeInOutTime));
                break;
            case State.FadingIn:
                icon.color = Color.Lerp(icon.color, Color.white, fadeInOutCurve.Evaluate(stateMachine.timeSinceStateChanged / iconFadeInOutTime));
                break;
            case State.IconPresent:
                icon.color = Color.white;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
