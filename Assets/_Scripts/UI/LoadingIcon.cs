using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingIcon : Singleton<LoadingIcon> {
    public bool DEBUG = false;
    Image icon;

    // Maybe figure out a different animation for this, but fill animation doens't work well while loading
    float iconSpinSpeed = 0f;
    float iconFadeOutSpeed = 2.5f;
    
    public enum State {
        Idle,
        Loading
    }

    State _state;
    public State state {
        get => _state;
        set {
            if (value == _state) {
                return;
            }

            switch (value) {
                case State.Idle:
                    break;
                case State.Loading:
                    icon.fillAmount = 1;
                    icon.fillClockwise = true;
                    icon.enabled = true;
                    icon.color = new Color(1, 1, 1, Mathf.Max(icon.color.a, .5f));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            _state = value;
        }
    }

    Color clear = new Color(1, 1, 1, 0);

    void Awake() {
        icon = GetComponent<Image>();
    }

    void Update() {
        if (!DEBUG) {
            state = GameManager.instance.IsCurrentlyLoading ? State.Loading : State.Idle;
        }

        if (DEBUG && Input.GetKeyDown("x")) {
            state = state == State.Idle ? State.Loading : State.Idle;
        }

        icon.fillAmount = Mathf.Clamp01(icon.fillAmount + (icon.fillClockwise ? 1 : -1) * iconSpinSpeed * Time.deltaTime);
        switch (state) {
            case State.Idle:
                if (icon.fillAmount == 0) {
                    icon.fillClockwise = !icon.fillClockwise;
                }
                icon.color = Color.Lerp(icon.color, clear, iconFadeOutSpeed * Time.deltaTime);
                if (icon.color.a < 0.01f) {
                    icon.color = clear;
                    icon.enabled = false;
                }
                break;
            case State.Loading:
                if (icon.fillAmount == 1 || icon.fillAmount == 0) {
                    icon.fillClockwise = !icon.fillClockwise;
                }
                icon.color = Color.Lerp(icon.color, Color.white, iconFadeOutSpeed * Time.deltaTime);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
