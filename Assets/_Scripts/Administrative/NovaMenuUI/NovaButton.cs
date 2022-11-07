using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
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
    private bool _isEnabled = true;

    public bool isEnabled {
        get => _isEnabled;
        set {
            novaInteractable.enabled = value;
            _isEnabled = value;
        }
    }
    public Interactable novaInteractable;
    public UIBlock[] OtherUIBlocks;
    public Option<string> Text => TextBlock.Map(t => t.Text);

    public const float colorLerpOnTime = .03f;
    public const float colorLerpOffTime = .375f;
    public bool unclickAfterClick = false;
    [ShowIf("unclickAfterClick")]
    public float timeToUnclick = .06f;

    public delegate void OnClickActionSimple();
    public delegate void OnClickAction(NovaButton thisButton);
    public event OnClickAction OnClickReset; // Triggered when we move back to Idle from Clicked
    public event OnClickActionSimple OnClickResetSimple; // Triggered when we move back to Idle from Clicked
    public static event OnClickAction OnAnyNovaButtonClick;
    public event OnClickAction OnClick;
    public event OnClickActionSimple OnClickSimple;
    
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
        if (!isEnabled) return;
        
        clipMask.ForEach(mask => {
            // Only enable the buttons once they're mostly visible
            novaInteractable.enabled = mask.Tint.a > clipMaskInteractThreshold;
        });
    }

    void InitButtonStateMachine() {
        if (unclickAfterClick) {
            buttonState.AddStateTransition(ButtonState.Clicked, ButtonState.Hovered, timeToUnclick);
        }
        
        buttonState.AddTrigger(ButtonState.Idle, () => {
            if (buttonState.prevState == ButtonState.Clicked) {
                OnClickReset?.Invoke(this);
                OnClickResetSimple?.Invoke();
            }
        });
        
        buttonState.AddTrigger((enumValue) => true, (newState) => {
            BackgroundUIBlock.Shadow.Direction = (newState is ButtonState.Clicked or ButtonState.ClickHeld) ?
                ShadowDirection.In :
                ShadowDirection.Out;
        });
        buttonState.OnStateChange += (prevState, unused) => {
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

                bool on = (int)buttonState > (int)prevState;
                buttonColorAnimationHandle = animation.Run(on ? colorLerpOnTime : colorLerpOffTime);
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
        bool someOtherDialogWindowOpen = DialogWindow.anyDialogueIsOpen && (dialogWindow.ForAll(window => DialogWindow.windowsOpen.Peek() != window));
        bool listeningForKeyRebind = Keybind.isListeningForNewKeybind;
        return someOtherDialogWindowOpen || listeningForKeyRebind;
    }
    
    private void HandleHoverEvent(Gesture.OnHover evt) {
        if (ShouldIgnoreInputs()) return;
        
        if (buttonState != ButtonState.Clicked) {
            debug.Log($"Hovered on {gameObject}");
            AudioManager.instance.Play(AudioName.UI_HoverBlip, shouldForcePlay: true);
            buttonState.Set(ButtonState.Hovered);
            evt.Consume();
        }
    }

    private void HandleUnhoverEvent(Gesture.OnUnhover evt) {
        if (ShouldIgnoreInputs()) return;

        debug.Log($"Unhovered on {gameObject.name}!");
        if (buttonState != ButtonState.Clicked) {
            buttonState.Set(ButtonState.Idle);
            evt.Consume();
        }
    }

    private void HandleClickDownEvent(Gesture.OnPress evt) {
        if (ShouldIgnoreInputs()) return;

        debug.Log($"Click down on {gameObject.name}!");
        buttonState.Set(ButtonState.ClickHeld);
        evt.Consume();
    }

    private void HandleReleaseEvent(Gesture.OnRelease evt) {
        if (ShouldIgnoreInputs()) return;

        debug.Log($"Released on {gameObject.name}!");
        if (evt.Hovering) {
            buttonState.Set(ButtonState.Clicked);
            
            OnAnyNovaButtonClick?.Invoke(this);
            OnClick?.Invoke(this);
            OnClickSimple?.Invoke();
        }
        else if (buttonState.prevState == ButtonState.Clicked) {
            buttonState.Set(ButtonState.Clicked);
        }
        else {
            buttonState.Set(ButtonState.Idle);
        }
        evt.Consume();
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
