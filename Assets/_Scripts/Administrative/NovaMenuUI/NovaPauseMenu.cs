using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nova;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

public class NovaPauseMenu : NovaMenu<NovaPauseMenu> {
    private List<NovaButton> children;

    public enum MenuState {
        Off,
        PauseMenuOpen,
        SubMenuOpen
    }
    public StateMachine<MenuState> currentMenuState = new StateMachine<MenuState>(MenuState.Off, false, true);
    
    public bool PauseMenuIsOpen => MenuBackground.Tint.a > 0.05f;
    public CursorLockMode cachedLockMode;

    public const float menuFadeTime = 0.5f;
    private const float visibleAlphaCutoff = 0.1f;
    private AnimationHandle pauseMenuAnimationHandle;
    private AnimationHandle settingsMenuAnimationHandle;
    private AnimationHandle backgroundAnimationHandle;

    [Header("Buttons")]
    public NovaButton ResumeButton;
    public NovaButton SaveButton;
    public NovaButton LoadButton;
    public NovaButton SettingsButton;
    public NovaButton ExitGameButton;

    [Header("Other Menus")]
    private readonly List<INovaMenu> allSubMenus = new List<INovaMenu>();
    public ClipMask MenuBackground;
    private ClipMask PauseMenu;
    public DialogWindow ExitGameDialogWindow;

    private void Awake() {
        children = GetComponentsInChildren<NovaButton>().ToList();
        PauseMenu = GetComponent<ClipMask>();

        allSubMenus.Add(SettingsMenu.instance);
        allSubMenus.Add(SaveMenu.instance);
        
        ResetAllButtons();

        InitEvents();
    }

    private void Start() {
        ClosePauseMenu(false, false);
    }

    private void Update() {
        // If we were in a submenu and there's none open now, re-open the Pause Menu
        if (allSubMenus.TrueForAll(menu => !menu.isOpen) && currentMenuState == MenuState.SubMenuOpen) {
            OpenPauseMenu(true);
            currentMenuState.Set(MenuState.PauseMenuOpen);
        }
        
        if (PlayerButtonInput.instance.PausePressed) {
            if (PauseMenuIsOpen) {
                if (allSubMenus.TrueForAll(subMenu => subMenu.canClose)) {
                    ClosePauseMenu();
                }
            }
            else {
                OpenPauseMenu(currentMenuState != MenuState.Off);
            }
        }
    }

    void ResetAllButtons() {
        foreach (var child in children) {
            child.buttonState.Set(NovaButton.ButtonState.Idle);
        }
    }

    void InitEvents() {
        // Nova events
        NovaUIBackground.instance.BackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleBackgroundHoverEvent);
        
        // My events
        ResumeButton.OnClick += (_) => ClosePauseMenu();
        SaveButton.OnClick += (_) => OpenSaveMenu();
        LoadButton.OnClick += (_) => OpenLoadMenu();
        SettingsButton.OnClick += (_) => OpenSettingsMenu();
        ExitGameButton.OnClick += (_) => ExitGameDialogWindow.Open();
    }

    private void HandleBackgroundHoverEvent(Gesture.OnHover evt) {
        // When background is hovered, unhighlight the buttons (unless they're being clicked)
        HashSet<NovaButton> allButtons = children.ToHashSet();
        foreach (var button in allButtons) {
            if (button.buttonState.state is NovaButton.ButtonState.ClickHeld or NovaButton.ButtonState.Clicked) continue;

            // Don't unhover things that just got hovered
            if (button.buttonState.timeSinceStateChanged < 0.1f) continue;
            
            button.buttonState.Set(NovaButton.ButtonState.Idle);
        }
    }

    void OpenLoadMenu() {
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.LoadSave);
        OpenSubMenu(SaveMenu.instance);
    }

    void OpenSaveMenu() {
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.WriteSave);
        OpenSubMenu(SaveMenu.instance);
    }

    void OpenSettingsMenu() {
        OpenSubMenu(SettingsMenu.instance);
    }

    public void OpenPauseMenu(bool gameIsAlreadyPaused) {
        if (!gameIsAlreadyPaused) {
            Time.timeScale = 0f;
            Cursor.visible = true;
            cachedLockMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.Confined;
        }

        foreach (var subMenu in allSubMenus) {
            if (subMenu.isOpen) {
                subMenu.Close();
            }
        }
        
        currentMenuState.Set(MenuState.PauseMenuOpen);

        RunAnimation(PauseMenu, 1f, ref pauseMenuAnimationHandle);
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.Off);
        
        // Could be called when no menu is open, so fade in the background too
        RunAnimation(MenuBackground, 1f, ref backgroundAnimationHandle);
    }

    public void ClosePauseMenu(bool restoreTimeScale = true, bool restoreLockState = true) {
        // Assumes Time.timeScale is always 1 when we're not paused
        if (restoreTimeScale) {
            Time.timeScale = 1;
        }
        
        Debug.Log("ClosePauseMenu called");

        if (restoreLockState) {
            Cursor.visible = false;
            Cursor.lockState = cachedLockMode;
        }

        foreach (var subMenu in allSubMenus) {
            if (subMenu.isOpen) {
                subMenu.Close();
            }
        }
        
        currentMenuState.Set(MenuState.Off);

        RunAnimation(PauseMenu, 0f, ref pauseMenuAnimationHandle);
        SaveMenu.instance.saveMenuState.Set(SaveMenu.SaveMenuState.Off);
        SaveMenu.instance.Close();
        
        // Fade out the background too
        RunAnimation(MenuBackground, 0f, ref backgroundAnimationHandle);
    }

    public void OpenSubMenu(INovaMenu subMenuToOpen) {
        foreach (var subMenu in allSubMenus) {
            if (subMenu == subMenuToOpen) continue;
            if (subMenu.isOpen) {
                subMenu.Close();
            }
        }
        
        subMenuToOpen.Open();
        currentMenuState.Set(MenuState.SubMenuOpen);
        
        RunAnimation(PauseMenu, 0f, ref pauseMenuAnimationHandle);
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
}