using Nova;
using SuperspectiveUtils;
using UnityEngine;

namespace NovaMenuUI {
    public class RootMenuSize : MonoBehaviour {
        public Camera UICamera;
        public UIBlock root;

        public float scaleFactor = 100;

        void Start() {
            UpdateMenuSizing(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);
            SuperspectiveScreen.instance.OnScreenResolutionChanged += UpdateMenuSizing;
        }

        private void UpdateMenuSizing(int newWidth, int newHeight) {
            float aspectRatio = (float)newWidth / newHeight;
            root.Size.Value = root.Size.Value.WithX(scaleFactor * aspectRatio).WithY(scaleFactor);
        }
    }
}
