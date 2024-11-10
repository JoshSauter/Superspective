using System;
using UnityEngine;

namespace NovaMenuUI {
    [Serializable]
    public class SettingsStylesheet {
        public Color BackgroundColor;
        public Color HoverColor;
        public Color DisabledOverlayColor = UIStyleManager.Grayscale(156f, 0.65f);

        public SliderStylesheet Slider;
        public SmallIntSliderStylesheet SmallIntSlider;
        public DropdownStylesheet Dropdown;
        // TODO: Add more settings stylesheets
    }
}
