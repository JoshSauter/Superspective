using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NovaMenuUI {
// Consistent coloring for UI components, in the future could have multiple themes to swap colors all together
    public enum UIStyleTheme {
        Default,
    }
    
    /// <summary>
    /// Stylesheet for all UI components
    /// </summary>
    [Serializable]
    public class UIStylesheet {
        public UIStyleTheme Theme;

        public NovaButtonStylesheet NovaButton;
        public SaveSlotStylesheet SaveSlot;
        public SettingsStylesheet Settings;
    }

    public class UIStyleManager : Singleton<UIStyleManager> {
        public UIStyleTheme CurrentTheme = UIStyleTheme.Default;
        public List<UIStylesheet> Stylesheets;
        
        private Dictionary<UIStyleTheme, UIStylesheet> _stylesheetMap;

        private void Awake() {
            _stylesheetMap = Stylesheets.ToDictionary(element => element.Theme, element => element);
        }

        public UIStylesheet CurrentStylesheet {
            get {
                if (_stylesheetMap == null) {
                    _stylesheetMap = Stylesheets.ToDictionary(element => element.Theme, element => element);
                }

                return _stylesheetMap[CurrentTheme];
            }
        }

        public static Color Grayscale(float outOf255, float alpha) {
            return new Color(outOf255 / 255f, outOf255 / 255f, outOf255 / 255f, alpha);
        }

        public static Color GrayscaleNormalized(float normalized, float alpha) {
            return new Color(normalized, normalized, normalized, alpha);
        }
    }
}
