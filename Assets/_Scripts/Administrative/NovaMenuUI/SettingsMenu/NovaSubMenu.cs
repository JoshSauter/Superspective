
using System;
using Nova;
using UnityEngine;

public abstract class NovaSubMenu<T> : NovaMenu<T> where T : MonoBehaviour {
    public NovaButton BackButton;
    private ClipMask BackButtonClipMask;
    private AnimationHandle backButtonAnimationHandle;

    public virtual void Awake() {
        BackButtonClipMask = BackButton.GetComponent<ClipMask>();
        
        BackButton.OnClick += HandleBackButtonClicked;
        OnMenuOpen += FadeInBackButton;
        OnMenuClose += FadeOutBackButton;
        
        Close();
    }

    private void OnDisable() {
        BackButton.OnClick -= HandleBackButtonClicked;
        OnMenuOpen -= FadeInBackButton;
        OnMenuClose -= FadeOutBackButton;
    }

    private void FadeOutBackButton() {
        RunAnimation(BackButtonClipMask, 0f, ref backButtonAnimationHandle);
    }

    private void FadeInBackButton() {
        RunAnimation(BackButtonClipMask, 1f, ref backButtonAnimationHandle);
    }

    private void HandleBackButtonClicked(NovaButton thisbutton) {
        if (isOpen) {
            Close();
        }
    }
}
