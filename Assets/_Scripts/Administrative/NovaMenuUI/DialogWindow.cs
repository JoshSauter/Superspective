using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Nova;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Events;

public class DialogWindow : MonoBehaviour {
    public static Stack<DialogWindow> windowsOpen = new Stack<DialogWindow>();
    public static bool anyDialogueIsOpen => windowsOpen != null && windowsOpen.Count > 0;

    public ClipMask ClipMask;
    public ClipMask[] AdditionalClipMasks;
    public NovaButton ConfirmButton;
    public NovaButton CancelButton;
    public UIBlock2D DialogWindowDarkening;

    private const float backgroundDarkeningAlpha = 0.75f;

    protected const float menuFadeAnimationTime = 0.5f;
    private AnimationHandle[] dialogWindowAnimationHandles;
    private AnimationHandle dialogDarkeningAnimationHandle;
    
    public enum DialogWindowState {
        Closed,
        Open
    }

    public bool isOpen => dialogWindowState == DialogWindowState.Open;

    public StateMachine<DialogWindowState> dialogWindowState;

    [Header("Events")]
    public UnityEvent OnSubmit;
    public UnityEvent OnCancel;

    void RunAnimation() {
        float targetZ = -20 * windowsOpen.Count;
        MenuFadeAnimation mainClipMaskAnimation = new MenuFadeAnimation {
            menuToAnimate = ClipMask,
            startAlpha = ClipMask.Tint.a,
            targetAlpha = isOpen ? 1 : 0,
            targetZ = targetZ
        };

        List<MenuFadeAnimation> animations = AdditionalClipMasks.Select(clipMask => new MenuFadeAnimation {
            menuToAnimate = clipMask,
            startAlpha = ClipMask.Tint.a,
            targetAlpha = isOpen ? 1 : 0,
        }).ToList();

        ColorFadeAnimation darkening = new ColorFadeAnimation() {
            UIBlock = DialogWindowDarkening,
            startColor = DialogWindowDarkening.Color,
            endColor = DialogWindowDarkening.Color.WithAlpha(anyDialogueIsOpen ? backgroundDarkeningAlpha : 0)
        };

        animations.Add(mainClipMaskAnimation);
        dialogWindowAnimationHandles = animations.Select(animation => animation.Run(menuFadeAnimationTime)).ToArray();
        dialogDarkeningAnimationHandle = darkening.Run(menuFadeAnimationTime);

        DialogWindowDarkening.transform.localPosition = DialogWindowDarkening.transform.InverseTransformPoint(transform.position.WithZ(targetZ) - Vector3.back) * DialogWindowDarkening.transform.lossyScale.z;
    }

    protected virtual void Start() {
        dialogWindowState = this.StateMachine(DialogWindowState.Closed, false, true);
        
        InitStateMachine();

        RunAnimation();

        ConfirmButton.OnClick += (_) => OnSubmit?.Invoke();
        CancelButton.OnClick += (_) => {
            OnCancel?.Invoke();
            Close();
        };
    }

    void InitStateMachine() {
        dialogWindowState.OnStateChange += (prevState, prevTime) => {
            if (dialogWindowState == DialogWindowState.Open && prevState == DialogWindowState.Closed) {
                windowsOpen.Push(this);
            }
            else if (dialogWindowState == DialogWindowState.Closed && prevState == DialogWindowState.Open) {
                windowsOpen.Pop();
            }
        };
        dialogWindowState.OnStateChangeSimple += RunAnimation;
    }

    public void ToggleOpen() {
        if (dialogWindowState == DialogWindowState.Closed) {
            dialogWindowState.Set(DialogWindowState.Open);
        }
        else {
            dialogWindowState.Set(DialogWindowState.Closed);
        }
    }
    
    public void Open() {
        dialogWindowState.Set(DialogWindowState.Open);
        // DialogWindowDarkening.transform.position = DialogWindowDarkening.transform.position.WithZ(10 + (windowsOpen.Count - 1) * 20);
    }

    public void Close() {
        if (dialogWindowState == DialogWindowState.Closed) return;
        
        dialogWindowState.Set(DialogWindowState.Closed);
        // DialogWindowDarkening.transform.position = DialogWindowDarkening.transform.position.WithZ(10 + (windowsOpen.Count - 1) * 20);
    }

    public void CloseDelayed(float dialogCloseDelay) {
        StartCoroutine(CloseDelayedEnumerator(dialogCloseDelay));
    }

    IEnumerator CloseDelayedEnumerator(float delay) {
        yield return new WaitForSecondsRealtime(delay);
        
        Close();
    }

    public void CancelAllCoroutines() {
        StopAllCoroutines();
    }
}
