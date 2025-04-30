using System;
using System.Collections.Generic;
using Audio;
using NaughtyAttributes;
using Nova;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace NovaMenuUI {
    [RequireComponent(typeof(Interactable))]
    public class NovaButton : MonoBehaviour {
        private static UIStylesheet Style => UIStyleManager.instance.CurrentStylesheet;
        
        private Color BackgroundColor => backgroundColorOverride != Color.clear ? backgroundColorOverride : Style.NovaButton.DefaultBgColor;
        private Color HoverBgColor => hoverColorOverride != Color.clear ? hoverColorOverride : Style.NovaButton.HoverBgColor;
        
        public Color backgroundColorOverride = Color.clear;
        public Color hoverColorOverride = Color.clear;
        
        public bool DEBUG;
        public DebugLogger debug;

        public enum HoverState {
            NotHovered,
            Hovered
        }

        public enum ClickState {
            Idle,
            ClickHeld,
            Clicked
        }

        public StateMachine<ClickState> clickState;
        public StateMachine<HoverState> hoverState;

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

        public bool onlyHover = false;

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
                    if (uiBlock == null) {
                        continue;
                    }

                    uiBlock.Color = Color.Lerp(startComponentColor, endComponentColor, t);
                }
            }
        }

        // Start is called before the first frame update
        void Awake() {
            clickState = this.StateMachine(ClickState.Idle, false, true);
            hoverState = this.StateMachine(HoverState.NotHovered, false, true);

            debug = new DebugLogger(gameObject, gameObject.FullPath(), () => DEBUG);

            novaInteractable = GetComponent<Interactable>();
            if (BackgroundUIBlock == null) {
                BackgroundUIBlock = GetComponentInChildren<UIBlock2D>();
            }

            TextBlock = Option<TextBlock>.Of(GetComponentInChildren<TextBlock>());
            clipMask = Option<ClipMask>.Of(gameObject.FindInParentsRecursively<ClipMask>());
            dialogWindow = Option<DialogWindow>.Of(gameObject.FindInParentsRecursively<DialogWindow>());

            InitButtonStateMachine();

            TextBlock.ForEach(t => t.Color = Style.NovaButton.DefaultComponentColor);
        }

        void Start() {
            GetComponentInChildren<UIBlock2D>().Color = BackgroundColor;
            if (TryGetComponent(out TextBlock text)) {
                text.Color = Style.NovaButton.DefaultComponentColor;
            }
            BackgroundUIBlock.Color = BackgroundColor;
        }

        private void Update() {
            if (!isEnabled) return;

            clipMask.ForEach(mask => {
                // Only enable the buttons once they're mostly visible
                novaInteractable.enabled = mask.Tint.a > clipMaskInteractThreshold;
            });
        }

        void InitButtonStateMachine() {
            // Delayed hover SFX
            hoverState.AddTrigger(HoverState.Hovered, 0.05f, () => {
                if (!GameManager.instance.gameHasLoaded) return;
                if (hoverState.PrevState == HoverState.NotHovered) {
                    AudioManager.instance.Play(AudioName.UI_HoverBlip, shouldForcePlay: true);
                }
            });

            if (unclickAfterClick) {
                clickState.AddStateTransition(ClickState.Clicked, ClickState.Idle, timeToUnclick);
            }

            // NovaButton event triggers
            clickState.AddTrigger(ClickState.Idle, () => {
                OnClickReset?.Invoke(this);
                OnClickResetSimple?.Invoke();
            });
            clickState.AddTrigger(ClickState.Clicked, () => {
                OnAnyNovaButtonClick?.Invoke(this);
                OnClick?.Invoke(this);
                OnClickSimple?.Invoke();
            });

            clickState.OnStateChangeSimple += () => { BackgroundUIBlock.Shadow.Direction = (clickState.State is ClickState.Clicked or ClickState.ClickHeld) ? ShadowDirection.In : ShadowDirection.Out; };

            void RunAnimation(Color endBgColor, Color endComponentColor, float lerpTime) {
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

                buttonColorAnimationHandle = animation.Run(lerpTime);
            }

            clickState.OnStateChange += (prevState, _) => {
                buttonColorAnimationHandle.Cancel();
                Color endBgColor, endTextColor;
                switch (clickState.State) {
                    case ClickState.Idle:
                        bool hovered = hoverState == HoverState.Hovered;
                        endBgColor = hovered ? HoverBgColor : BackgroundColor;
                        endTextColor = Style.NovaButton.DefaultComponentColor;
                        break;
                    case ClickState.ClickHeld:
                        endBgColor = Style.NovaButton.ClickHeldBgColor;
                        endTextColor = Style.NovaButton.ClickHeldComponentColor;
                        break;
                    case ClickState.Clicked:
                        endBgColor = Style.NovaButton.ClickedBgColor;
                        endTextColor = Style.NovaButton.ClickedComponentColor;
                        break;
                    default:
                        return;
                }

                bool on = (int)clickState > (int)prevState;
                RunAnimation(endBgColor, endTextColor, on ? colorLerpOnTime : colorLerpOffTime);
            };

            hoverState.OnStateChange += (prevState, _) => {
                buttonColorAnimationHandle.Cancel();
                Color endBgColor, endTextColor;
                if (clickState != ClickState.Idle) return;
                switch (hoverState.State) {
                    case HoverState.NotHovered:
                        endBgColor = BackgroundColor;
                        endTextColor = Style.NovaButton.DefaultComponentColor;
                        break;
                    case HoverState.Hovered:
                        endBgColor = HoverBgColor;
                        endTextColor = Style.NovaButton.DefaultComponentColor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                bool on = (int)hoverState > (int)prevState;
                RunAnimation(endBgColor, endTextColor, on ? colorLerpOnTime : colorLerpOffTime);
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
            bool listeningForKeyRebind = Keybind.IsListeningForNewKeybind;
            return someOtherDialogWindowOpen || listeningForKeyRebind;
        }

        private void HandleHoverEvent(Gesture.OnHover evt) {
            if (ShouldIgnoreInputs()) return;

            debug.Log($"Hovered on {gameObject}");
            hoverState.Set(HoverState.Hovered);
            evt.Consume();
        }

        private void HandleUnhoverEvent(Gesture.OnUnhover evt) {
            if (ShouldIgnoreInputs()) return;

            debug.Log($"Unhovered on {gameObject.name}!");
            hoverState.Set(HoverState.NotHovered);
            evt.Consume();
        }

        private void HandleClickDownEvent(Gesture.OnPress evt) {
            if (ShouldIgnoreInputs() || onlyHover) return;

            debug.Log($"Click down on {gameObject.name}!");
            clickState.Set(ClickState.ClickHeld);
            evt.Consume();
        }

        private void HandleReleaseEvent(Gesture.OnRelease evt) {
            if (ShouldIgnoreInputs() || onlyHover) return;

            debug.Log($"Released on {gameObject.name}!");
            // Mouse is over button
            if (evt.Hovering) {
                // Button was already in ClickHeld state
                if (clickState.State == ClickState.ClickHeld) {
                    // Button used to be Clicked, toggle it to Idle
                    if (clickState.PrevState == ClickState.Clicked) {
                        clickState.Set(ClickState.Idle);
                    }
                    // Button used to be not Clicked, toggle it to Clicked
                    else {
                        clickState.Set(ClickState.Clicked);
                    }
                }
            }
            // Not hovering button, set click state to prevState
            else {
                clickState.Set(clickState.PrevState);
            }

            evt.Consume();
        }

        public void Click() {
            switch (clickState.State) {
                case ClickState.Idle:
                    clickState.Set(ClickState.Clicked);
                    break;
                case ClickState.ClickHeld:
                    // Ignore input on ClickHeld
                    break;
                case ClickState.Clicked:
                    // Unclick if already clicked and it is a toggle
                    if (!unclickAfterClick) {
                        clickState.Set(ClickState.Idle);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
}
