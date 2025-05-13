using Nova;
using UnityEngine;

namespace NovaMenuUI {
    public class SuperspectiveLogo : MonoBehaviour {
        public Texture2D logoTextureLight;
        public Texture2D logoTextureDark;

        public NovaButton logoButton;
        public UIBlock2D logoBlock;

        public bool isUsingDark = false;
        
        private void Start() {
            UpdateLogoTexture();

            logoButton.OnClick += _ => {
                isUsingDark = !isUsingDark;
                UpdateLogoTexture();
            };
        }
        
        public void UpdateLogoTexture() {
            if (logoBlock == null || logoButton == null) return;

            if (isUsingDark) {
                logoBlock.SetImage(logoTextureDark);
                logoBlock.Border.Color = Color.black;
                //UIStyleManager.instance.CurrentTheme = UIStyleTheme.Dark;
            }
            else {
                logoBlock.SetImage(logoTextureLight);
                logoBlock.Border.Color = Color.white;
                //UIStyleManager.instance.CurrentTheme = UIStyleTheme.Default;
            }
        }
    }
}
