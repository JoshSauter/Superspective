using System;
using UnityEngine;

namespace NovaMenuUI {
    /// <summary>
    /// Stylesheet for the delete button on a save slot
    /// </summary>
    [Serializable]
    public class DeleteStylesheet {
        public Color DeleteDefaultBgColor = UIStyleManager.Grayscale(16, 1);
        public Color DeleteHoverBgColor = UIStyleManager.Grayscale(84, 1);
        public Color DeleteClickHeldBgColor = UIStyleManager.Grayscale(126, 1);
        public Color DeleteClickedBgColor = UIStyleManager.Grayscale(240, 1);

        public Color DefaultTextAndIconColor = UIStyleManager.Grayscale(249, 1);
        public Color ClickHeldTextAndIconColor = UIStyleManager.Grayscale(16, 1);
        public Color ClickedTextAndIconColor = UIStyleManager.Grayscale(12, 1);

        public Color DefaultConfirmColor = new Color(63f / 510f, 233f / 510f, 113f / 510f, 1);
        public Color ClickHeldConfirmColor = new Color(63f / 255f, 233f / 255f, 113f / 255f, 1);

        public Color DefaultCancelColor = new Color(233f / 510f, 63f / 510f, 63f / 510f, 1);
        public Color ClickHeldCancelColor = new Color(233f / 255f, 63f / 255f, 63f / 255f, 1);
    }
}