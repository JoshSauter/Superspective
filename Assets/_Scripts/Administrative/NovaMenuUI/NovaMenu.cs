using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Nova;
using UnityEngine;
using UnityEngine.Serialization;

public delegate void MenuEvent();
public interface INovaMenu {
    public bool isOpen { get; }
    public bool canClose { get; }

    public event MenuEvent OnMenuOpen;
    public event MenuEvent OnMenuClose;
    
    public void Open();
    public void Close();
}

public abstract class NovaMenu<T> : Singleton<T>, INovaMenu where T : MonoBehaviour {
    [FormerlySerializedAs("clipMaskForMenu")]
    public ClipMask[] clipMasksForMenu;

    private Dictionary<ClipMask, AnimationHandle> menuFadeAnimationHandles = new Dictionary<ClipMask, AnimationHandle>();

    [ShowNativeProperty]
    public bool isOpen { get; private set; } = false;
    // If evaluates to false, will prevent menu from closing when escape/back is pressed
    public virtual bool canClose => true;

    public event MenuEvent OnMenuOpen;
    public event MenuEvent OnMenuClose;

    public void Open() {
        foreach (ClipMask clipMask in clipMasksForMenu) {
            RunAnimation(clipMask, 1f);
        } 
        isOpen = true;
        OnMenuOpen?.Invoke();
    }

    public void Close() {
        if (!canClose) return;
        
        foreach (ClipMask clipMask in clipMasksForMenu) {
            RunAnimation(clipMask, 0f);
        } 
        isOpen = false;
        OnMenuClose?.Invoke();
    }
    
    protected void RunAnimation(ClipMask target, float targetAlpha) {
        if (!menuFadeAnimationHandles.ContainsKey(target)) {
            menuFadeAnimationHandles.Add(target, new AnimationHandle());
        }
        
        MenuFadeAnimation fadeMenuAnimation = new MenuFadeAnimation() {
            menuToAnimate = target,
            startAlpha = target.Tint.a,
            targetAlpha = targetAlpha
        };

        menuFadeAnimationHandles[target].Cancel();
        menuFadeAnimationHandles[target] = fadeMenuAnimation.Run(NovaPauseMenu.menuFadeTime);
    }
    
    protected void RunAnimation(ClipMask target, float targetAlpha, ref AnimationHandle handle) {
        MenuFadeAnimation fadeMenuAnimation = new MenuFadeAnimation() {
            menuToAnimate = target,
            startAlpha = target.Tint.a,
            targetAlpha = targetAlpha
        };

        handle.Cancel();
        handle = fadeMenuAnimation.Run(NovaPauseMenu.menuFadeTime);
    }
}
