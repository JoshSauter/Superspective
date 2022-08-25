using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Nova;
using StateUtils;
using SuperspectiveUtils;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class NovaButton : MonoBehaviour {
    public bool DEBUG;
    public DebugLogger debug;
    public enum ButtonState {
        Idle,
        Hovered,
        ClickHeld,
        Clicked
    }

    public StateMachine<ButtonState> buttonState = new StateMachine<ButtonState>(ButtonState.Idle, false, true);

    private AnimationHandle buttonColorAnimationHandle;

    private Option<ClipMask> clipMask; // May or may not exist on some parent object, but if it does, we shouldn't be interactable while the clipMask alpha is 0
    private const float clipMaskInteractThreshold = 0.75f;
    private Option<DialogWindow> dialogWindow; // Similar to clipMask, may or may not exist on some parent object, used to determine when to ignore inputs
    [Tooltip("Will be set to the UIBlock2D on this object if not specified in Inspector")]
    public UIBlock2D BackgroundUIBlock;
    public Option<TextBlock> TextBlock;
    public Interactable novaInteractable;
    public UIBlock[] OtherUIBlocks;
    public Option<string> Text => TextBlock.Map(t => t.Text);

    public const float colorLerpTime = .085f;
    public bool unclickAfterClick = false;
    [ShowIf("unclickAfterClick")]
    public float timeToUnclick = .5f;

    public delegate void OnClickAction(NovaButton thisButton);
    public event OnClickAction OnClickReset; // Triggered when we move back to Idle from Clicked
    public event OnClickAction OnClick;
    
    private List<UIBlock> allComponents {
        get {
            List<UIBlock> result = new List<UIBlock>(OtherUIBlocks);
            TextBlock.ForEach(t => result.Add(t));

            return result;
        }
    }

    public struct ButtonColorAnimation : IAnimation {
        public Color startBgColor;
        public Color endBgColor;
        public Color startComponentColor;
        public Color endComponentColor;
        public UIBlock2D backgroundToAnimate;
        public UIBlock[] componentsToAnimate;
        
        public void Update(float percentDone) {
            float t = Easing.EaseInOut(percentDone);
            backgroundToAnimate.Color = Color.Lerp(startBgColor, endBgColor, t);

            foreach (var uiBlock in componentsToAnimate) {
                uiBlock.Color = Color.Lerp(startComponentColor, endComponentColor, t);
            }
        }
    }

    private void OnValidate() {
        GetComponentInChildren<UIBlock2D>().Color = UIStyle.NovaButton.DefaultBgColor;
        if (TryGetComponent(out TextBlock text)) {
            text.Color = UIStyle.NovaButton.DefaultComponentColor;
        }
    }

    // Start is called before the first frame update
    void Awake() {
        debug = new DebugLogger(gameObject, () => DEBUG);
        
        novaInteractable = GetComponent<Interactable>();
        if (BackgroundUIBlock == null) {
            BackgroundUIBlock = GetComponentInChildren<UIBlock2D>();
        }

        TextBlock = Option<TextBlock>.Of(GetComponentInChildren<TextBlock>());
        clipMask = Option<ClipMask>.Of(gameObject.FindInParentsRecursively<ClipMask>());
        dialogWindow = Option<DialogWindow>.Of(gameObject.FindInParentsRecursively<DialogWindow>());

        InitButtonStateMachine();

        BackgroundUIBlock.Color = UIStyle.NovaButton.DefaultBgColor;
        TextBlock.ForEach(t => t.Color = UIStyle.NovaButton.DefaultComponentColor);
    }

    private void Update() {
        clipMask.ForEach(mask => {
            // Only enable the buttons once they're mostly visible
            novaInteractable.enabled = mask.Tint.a > clipMaskInteractThreshold;
        });
    }

    void InitButtonStateMachine() {
        if (unclickAfterClick) {
            buttonState.AddStateTransition(ButtonState.Clicked, ButtonState.Hovered, timeToUnclick);
        }
        
        buttonState.AddTrigger(ButtonState.Clicked, () => {
            OnClick?.Invoke(this);
        });
        buttonState.AddTrigger(ButtonState.Idle, () => {
            if (buttonState.prevState == ButtonState.Clicked) {
                OnClickReset?.Invoke(this);
            }
        });
        
        buttonState.AddTrigger((enumValue) => true, (newState) => {
            BackgroundUIBlock.Shadow.Direction = (newState is ButtonState.Clicked or ButtonState.ClickHeld) ?
                ShadowDirection.In :
                ShadowDirection.Out;
        });
        buttonState.OnStateChangeSimple += () => {
            void RunAnimation(Color endBgColor, Color endComponentColor) {
                UIBlock[] all = allComponents.ToArray();
                Color startComponentColor = (all.Length > 0) ? all[0].Color : Color.magenta;
                ButtonColorAnimation animation = new ButtonColorAnimation {
                    startBgColor = BackgroundUIBlock.Color,
                    endBgColor = endBgColor,
                    startComponentColor = startComponentColor,
                    endComponentColor = endComponentColor,
                    backgroundToAnimate = BackgroundUIBlock,
                    componentsToAnimate = all,
                };

                buttonColorAnimationHandle = animation.Run(colorLerpTime);
            }

            buttonColorAnimationHandle.Cancel();
            Color endBgColor, endTextColor;
            switch (buttonState.state) {
                case ButtonState.Idle:
                    endBgColor = UIStyle.NovaButton.DefaultBgColor;
                    endTextColor = UIStyle.NovaButton.DefaultComponentColor;
                    break;
                case ButtonState.Hovered:
                    endBgColor = UIStyle.NovaButton.HoverBgColor;
                    endTextColor = UIStyle.NovaButton.DefaultComponentColor;
                    break;
                case ButtonState.ClickHeld:
                    endBgColor = UIStyle.NovaButton.ClickHeldBgColor;
                    endTextColor = UIStyle.NovaButton.ClickHeldComponentColor;
                    break;
                case ButtonState.Clicked:
                    endBgColor = UIStyle.NovaButton.ClickedBgColor;
                    endTextColor = UIStyle.NovaButton.ClickedComponentColor;
                    break;
                default:
                    return;
            }
            RunAnimation(endBgColor, endTextColor);
        };
    }

    void OnEnable() {
        SubscribeToMouseEvents();
    }

    private void OnDisable() {
        UnsubscribeFromMouseEvents();
    }

    bool ShouldIgnoreInputs() {
        return DialogWindow.anyDialogueIsOpen && (dialogWindow.ForAll(window => DialogWindow.windowOpen != window));
    }
    
    private void HandleHoverEvent(Gesture.OnHover evt) {
        if (ShouldIgnoreInputs()) return;
        
        if (buttonState != ButtonState.Clicked) {
            debug.Log($"Hovered on {gameObject}");
            buttonState.Set(ButtonState.Hovered);
        }
    }

    private void HandleUnhoverEvent(Gesture.OnUnhover evt) {
        if (ShouldIgnoreInputs()) return;

        debug.Log($"Unhovered on {gameObject.name}!");
        if (buttonState != ButtonState.Clicked) {
            buttonState.Set(ButtonState.Idle);
        }
    }

    private void HandleClickDownEvent(Gesture.OnPress evt) {
        if (ShouldIgnoreInputs()) return;

        debug.Log($"Click down on {gameObject.name}!");
        buttonState.Set(ButtonState.ClickHeld);
    }

    private void HandleReleaseEvent(Gesture.OnRelease evt) {
        if (ShouldIgnoreInputs()) return;

        debug.Log($"Released on {gameObject.name}!");
        if (evt.Hovering) {
            buttonState.Set(ButtonState.Clicked);
        }
        else if (buttonState.prevState == ButtonState.Clicked) {
            buttonState.Set(ButtonState.Clicked);
        }
        else {
            buttonState.Set(ButtonState.Idle);
        }
    }

    void SubscribeToMouseEvents() {
        novaInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleHoverEvent);
        novaInteractable.UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleUnhoverEvent);
        novaInteractable.UIBlock.AddGestureHandler<Gesture.OnPress>(HandleClickDownEvent);
        novaInteractable.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleReleaseEvent);
    }

    void UnsubscribeFromMouseEvents() {
        novaInteractable.UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleHoverEvent);
        novaInteractable.UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleUnhoverEvent);
        novaInteractable.UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandleClickDownEvent);
        novaInteractable.UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleReleaseEvent);
    }
}
