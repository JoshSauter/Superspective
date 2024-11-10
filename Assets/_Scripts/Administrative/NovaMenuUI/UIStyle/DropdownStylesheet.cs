using System;
using UnityEngine;

namespace NovaMenuUI {
    /// <summary>
    /// Stylesheet for a dropdown setting
    /// </summary>
    [Serializable]
    public class DropdownStylesheet {
        public Color UnselectedColor = new Color(657f / 1020f, 624f / 1020f, 633f / 1020f, .3f);
        public Color HoverBgColor = new Color(219f / 1020f, 208f / 1020f, 211f / 1020f, 0.45f);
        public Color ClickHeldBgColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        public Color ClickedBgColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    }
}
