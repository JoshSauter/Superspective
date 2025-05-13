using Nova;
using Sirenix.OdinInspector;
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

    public enum PositionFormat {
        Pixels,
        Percent
    }
    struct MenuMovementAnimation : IAnimation {
        public UIBlock2D blockMoving;
        public PositionFormat positionType;
        private bool IsUsingPercent => positionType == PositionFormat.Percent;
        
        [ShowIf(nameof(IsUsingPercent))]
        public Vector3 startPercent;
        [ShowIf(nameof(IsUsingPercent))]
        public Vector3 endPercent;
        
        [HideIf(nameof(IsUsingPercent))]
        public Vector3 startPixels;
        [HideIf(nameof(IsUsingPercent))]
        public Vector3 endPixels;

        public void Update(float percentDone) {
            float t = Easing.EaseInOut(percentDone);
            Vector3 start = IsUsingPercent ? startPercent : startPixels;
            Vector3 end = IsUsingPercent ? endPercent : endPixels;
            
            Vector3 newPos = Vector3.Lerp(start, end, t);
            
            if (IsUsingPercent) {
                blockMoving.Position.Percent = newPos;
            }
            else {
                blockMoving.Position.Raw = newPos;
            }
        }
    }
}
