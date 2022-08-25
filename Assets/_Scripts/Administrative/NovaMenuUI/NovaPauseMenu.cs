using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

public class NovaPauseMenu : Singleton<NovaPauseMenu> {
    private List<NovaButton> children;

    public enum MenuState {
        PauseMenu,
        SettingsMenu,
        SaveMenu,
    }
    
    public bool PauseMenuIsOpen => MenuBackground.Tint.a > 0.05f;
    public CursorLockMode cachedLockMode;

    [Header("Animation Settings")]
    public float menuFadeTime = 0.5f;
    private const float visibleAlphaCutoff = 0.1f;
    private AnimationHandle pauseMenuAnimationHandle;
    private AnimationHandle settingsMenuAnimationHandle;
    private AnimationHandle backgroundAnimationHandle;
    private AnimationHandle backButtonAnimationHandle;

    [Header("Buttons")]
    public NovaButton ResumeButton;
    public NovaButton SaveButton;
    public NovaButton LoadButton;
    public NovaButton SettingsButton;
    public NovaButton ExitGameButton;
    public NovaButton BackButton;
    public UIBlock BackButtonBlock => BackButton.novaInteractable.UIBlock;

    [Header("Other Menus")]
    public ClipMask MenuBackground;
    public ClipMask SettingsMenu;
    public NovaRadioSelection SettingsRadioSelection;
    private ClipMask PauseMenu;
    public DialogWindow ExitGameDialogWindow;
    
    
    private Interactable _backgroundInteractable;
    private Interactable backgroundInteractable {
        get {
            if (_backgroundInteractable == null) {
                _backgroundInteractable = MenuBackground.GetComponent<Interactable>();
            }

            return _backgroundInteractable;
        }
    }

    private void Awake() {
        children = GetComponentsInChildren<NovaButton>().ToList();
        PauseMenu = GetComponent<ClipMask>();
        
        ResetAllButtons();
        SettingsMenu.Tint = SettingsMenu.Tint.WithAlpha(0f);

        InitEvents();
    }

    private void Start() {
        ClosePauseMenu(false, false);
    }

    private void Update() {
        if (PlayerButtonInput.instance.EscapePressed) {
            if (PauseMenuIsOpen) {
                ClosePauseMenu();
            }
            else {
                OpenPauseMenu(false);
            }
        }
    }

    void ResetAllButtons() {
        foreach (var child in children) {
            child.buttonState.Set(NovaButton.ButtonState.Idle);
        }
        BackButton.buttonState.Set(NovaButton.ButtonState.Idle);
    }

    void InitEvents() {
        // Nova events
        backgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleBackgroundHoverEvent);
        
        // My events
        ResumeButton.OnClick += (_) => ClosePauseMenu();
        SaveButton.OnClick += (_) => OpenSaveMenu();
        LoadButton.OnClick += (_) => OpenLoadMenu();
        SettingsButton.OnClick += (_) => OpenSettingsMenu();
        ExitGameButton.OnClick += (_) => ExitGameDialogWindow.Open();
        BackButton.OnClick += (_) => OpenPauseMenu(true);
    }

    private void HandleBackgroundHoverEvent(Gesture.OnHover evt) {
        // When background is hovered, unhighlight the buttons (unless they're being clicked)
        HashSet<NovaButton> allButtons = children.ToHashSet();
        allButtons.UnionWith(new List<NovaButton>() { BackButton });
        foreach (var button in allButtons) {
            if (button.buttonState.state is NovaButton.ButtonState.ClickHeld or NovaButton.ButtonState.Clicked) continue;

            // Don't unhover things that just got hovered
            if (button.buttonState.timeSinceStateChanged < 0.1f) continue;
            
            button.buttonState.Set(NovaButton.ButtonState.Idle);
        }
    }

    void OpenLoadMenu() {
        RunAnimation(PauseMenu, 0f, ref pauseMenuAnimationHandle);
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.LoadSave);
        RunAnimation(BackButtonBlock, 1f, ref backButtonAnimationHandle);
    }

    void OpenSaveMenu() {
        RunAnimation(PauseMenu, 0f, ref pauseMenuAnimationHandle);
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.WriteSave);
        RunAnimation(BackButtonBlock, 1f, ref backButtonAnimationHandle);
    }

    void OpenSettingsMenu() {
        RunAnimation(PauseMenu, 0f, ref pauseMenuAnimationHandle);
        RunAnimation(SettingsMenu, 1f, ref settingsMenuAnimationHandle);
        RunAnimation(BackButtonBlock, 1f, ref backButtonAnimationHandle);

        if (SettingsRadioSelection.selection == null) {
            SettingsRadioSelection.SetSelection(0);
        }
    }

    public void OpenPauseMenu(bool gameIsAlreadyPaused) {
        if (!gameIsAlreadyPaused) {
            Time.timeScale = 0f;
            Cursor.visible = true;
            cachedLockMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.Confined;
        }

        RunAnimation(SettingsMenu, 0f, ref settingsMenuAnimationHandle);
        RunAnimation(PauseMenu, 1f, ref pauseMenuAnimationHandle);
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.Off);
        // Could be called when no menu is open, so fade in the background too
        RunAnimation(MenuBackground, 1f, ref backgroundAnimationHandle);
        RunAnimation(BackButtonBlock, 0f, ref backButtonAnimationHandle);
    }

    public void ClosePauseMenu(bool restoreTimeScale = true, bool restoreLockState = true) {
        // Assumes Time.timeScale is always 1 when we're not paused
        if (restoreTimeScale) {
            Time.timeScale = 1;
        }

        if (restoreLockState) {
            Cursor.visible = false;
            Cursor.lockState = cachedLockMode;
        }

        RunAnimation(SettingsMenu, 0f, ref settingsMenuAnimationHandle);
        RunAnimation(PauseMenu, 0f, ref pauseMenuAnimationHandle);
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.Off);
        // Fade out the background too
        RunAnimation(MenuBackground, 0f, ref backgroundAnimationHandle);
        RunAnimation(BackButtonBlock, 0f, ref backButtonAnimationHandle);
    }
    
    private void RunAnimation(UIBlock target, float targetAlpha, ref AnimationHandle handle) {
        ColorFadeAnimation fadeMenuAnimation = new ColorFadeAnimation {
            UIBlock = target,
            startColor = target.Color,
            endColor = target.Color.WithAlpha(targetAlpha)
        };
        
        handle.Cancel();
        handle = fadeMenuAnimation.Run(menuFadeTime);
    }

    private void RunAnimation(ClipMask target, float targetAlpha, ref AnimationHandle handle) {
        MenuFadeAnimation fadeMenuAnimation = new MenuFadeAnimation() {
            menuToAnimate = target,
            startAlpha = target.Tint.a,
            targetAlpha = targetAlpha
        };

        handle.Cancel();
        handle = fadeMenuAnimation.Run(menuFadeTime);
    }
}
