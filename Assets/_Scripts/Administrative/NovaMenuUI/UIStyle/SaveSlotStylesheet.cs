using System;
using UnityEngine;

namespace NovaMenuUI {
    /// <summary>
    /// Stylesheet for a save slot
    /// </summary>
    [Serializable]
    public class SaveSlotStylesheet {
        public Color DefaultBgColor = new Color(182f / 255f, 180f / 255f, 174f / 255f, 209f / 255f);
        public Color HoverBgColor = new Color(1, 252f / 255f, 243.6f / 255f, 245f / 255f);
        public Color ClickHeldBgColor = new Color(0.3f, 0.29f, 0.27f, 1f);
        public Color ClickedBgColor = new Color(0.25f, 0.23f, 0.21f, 1f);

        public Color DefaultTextColor = Color.black;
        public Color ClickHeldTextColor = UIStyleManager.GrayscaleNormalized(.75f, 1);
        public Color ClickedTextColor = Color.white;

        public DeleteStylesheet Delete;
    }
}
