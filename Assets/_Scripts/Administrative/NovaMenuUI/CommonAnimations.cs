using Nova;
using SuperspectiveUtils;
using UnityEngine;

namespace NovaMenuUI {
    struct MenuFadeAnimation : IAnimation {
        public ClipMask menuToAnimate;
        public float startAlpha;
        public float targetAlpha;
        public float targetZ;
        
        public void Update(float percentDone) {
            if (menuToAnimate == null) return;

            float t = Easing.EaseInOut(percentDone);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            menuToAnimate.Tint = menuToAnimate.Tint.WithAlpha(alpha);

            if (targetAlpha > 0f) {
                menuToAnimate.transform.localPosition = menuToAnimate.transform.localPosition.WithZ(targetZ);
            }
            else if (percentDone >= 1 && targetAlpha <= 0f) {
                menuToAnimate.transform.localPosition = menuToAnimate.transform.localPosition.WithZ(10);
            }
        }
    }

    struct ColorFadeAnimation : IAnimation {
        public UIBlock UIBlock;
        public Color startColor;
        public Color endColor;

        public void Update(float percentDone) {
            float t = Easing.EaseInOut(percentDone);
            UIBlock.Color = Color.Lerp(startColor, endColor, t);
        }
    }
}
