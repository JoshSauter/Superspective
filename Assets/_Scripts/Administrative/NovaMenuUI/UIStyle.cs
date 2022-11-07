using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Consistent coloring for UI components, in the future could have multiple themes to swap colors all together
public static class UIStyle {
    public static HeaderSettingsItem NewHeader(string text) {
        return HeaderSettingsItem.Of(text);
    }
    
    public static SeparatorSettingItem NewSeparator() {
        return new SeparatorSettingItem();
    }
    
    public static SpacerSettingItem NewSpacer() {
        return new SpacerSettingItem();
    }
    
    private static Color Grayscale(float outOf255, float alpha) {
        return new Color(outOf255 / 255f, outOf255 / 255f, outOf255 / 255f, alpha);
    }

    private static Color GrayscaleNormalized(float normalized, float alpha) {
        return new Color(normalized, normalized, normalized, alpha);
    }
    
    public static class NovaButton {
        // NovaButton colors
        public static Color DefaultBgColor = new Color(657f/1020f, 624f/1020f, 633f/1020f, .3f);
        public static Color HoverBgColor = new Color(219f/1020f, 208f/1020f, 211f/1020f, 0.45f);
        public static Color ClickHeldBgColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        public static Color ClickedBgColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    
        public static Color DefaultComponentColor = Color.black;
        public static Color ClickHeldComponentColor = GrayscaleNormalized(.75f, 1);
        public static Color ClickedComponentColor = Color.white;
    }

    public static class SaveSlot {
        // SaveSlot colors
        public static Color DefaultBgColor = new Color(182f/255f, 180f/255f, 174f/255f, 209f/255f);
        public static Color HoverBgColor = new Color(1, 252f/255f, 243.6f/255f, 245f/255f);
        public static Color ClickHeldBgColor = new Color(0.3f, 0.29f, 0.27f, 1f);
        public static Color ClickedBgColor = new Color(0.25f, 0.23f, 0.21f, 1f);
    
        public static Color DefaultTextColor = Color.black;
        public static Color ClickHeldTextColor = GrayscaleNormalized(.75f, 1);
        public static Color ClickedTextColor = Color.white;

        public static class Delete {
            public static Color DeleteDefaultBgColor = Grayscale(16, 1);
            public static Color DeleteHoverBgColor = Grayscale(84, 1);
            public static Color DeleteClickHeldBgColor = Grayscale(126, 1);
            public static Color DeleteClickedBgColor = Grayscale(240, 1);

            public static Color DefaultTextAndIconColor = Grayscale(249, 1);
            public static Color ClickHeldTextAndIconColor = Grayscale(16, 1);
            public static Color ClickedTextAndIconColor = Grayscale(12, 1);

            public static Color DefaultConfirmColor = new Color(63f / 510f, 233f / 510f, 113f / 510f, 1);
            public static Color ClickHeldConfirmColor = new Color(63f/255f, 233f/255f, 113f/255f, 1);
            
            public static Color DefaultCancelColor = new Color(233f/510f, 63f/510f, 63f/510f, 1);
            public static Color ClickHeldCancelColor = new Color(233f/255f, 63f/255f, 63f/255f, 1);
        }
    }

    public static class Settings {
        public static class SmallIntSlider {
            public static Color fillColor = Grayscale(89f, 1f);
            public static Color unfilledColor = Color.white;
        }
        public static class Dropdown {
            public static Color UnselectedColor = new Color(657f / 1020f, 624f / 1020f, 633f / 1020f, .3f);
            public static Color HoverBgColor = new Color(219f/1020f, 208f/1020f, 211f/1020f, 0.45f);
            public static Color ClickHeldBgColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            public static Color ClickedBgColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        }
    }
}
