using System.Collections;
using System.Collections.Generic;
using Nova;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Events;

public class DialogWindow : MonoBehaviour {
    public static DialogWindow windowOpen;
    public static bool anyDialogueIsOpen => windowOpen != null;
    
    public ClipMask ClipMask;
    public NovaButton ConfirmButton;
    public NovaButton CancelButton;
    public UIBlock2D DialogWindowDarkening;

    private const float backgroundDarkeningAlpha = 0.75f;

    private const float menuFadeAnimationTime = 0.5f;
    private AnimationHandle dialogWindowAnimationHandle;
    private AnimationHandle dialogDarkeningAnimationHandle;
    
    public enum DialogWindowState {
        Closed,
        Open
    }

    public bool isOpen => dialogWindowState == DialogWindowState.Open;

    public StateMachine<DialogWindowState> dialogWindowState = new StateMachine<DialogWindowState>(DialogWindowState.Closed, false, true);

    [Header("Events")]
    public UnityEvent OnSubmit;
    public UnityEvent OnCancel;

    void RunAnimation() {
        MenuFadeAnimation animation = new MenuFadeAnimation {
            menuToAnimate = ClipMask,
            startAlpha = ClipMask.Tint.a,
            targetAlpha = isOpen ? 1 : 0,
            targetZ = -20
        };

        ColorFadeAnimation darkening = new ColorFadeAnimation() {
            UIBlock = DialogWindowDarkening,
            startColor = DialogWindowDarkening.Color,
            endColor = DialogWindowDarkening.Color.WithAlpha(isOpen ? backgroundDarkeningAlpha : 0)
        };

        dialogWindowAnimationHandle = animation.Run(menuFadeAnimationTime);
        dialogDarkeningAnimationHandle = darkening.Run(menuFadeAnimationTime);
    }

    protected virtual void Start() {
        InitStateMachine();

        RunAnimation();

        ConfirmButton.OnClick += (_) => OnSubmit?.Invoke();
        CancelButton.OnClick += (_) => {
            OnCancel?.Invoke();
            Close();
        };
    }

    void InitStateMachine() {
        dialogWindowState.OnStateChangeSimple += RunAnimation;
    }
    
    public void Open() {
        dialogWindowState.Set(DialogWindowState.Open);
        windowOpen = this;
    }

    public void Close() {
        dialogWindowState.Set(DialogWindowState.Closed);
        windowOpen = null;
    }
}
