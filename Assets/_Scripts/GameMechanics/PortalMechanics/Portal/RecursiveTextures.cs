using System;
using UnityEngine;

namespace PortalMechanics {
    /// <summary>
    /// RecursiveTextures contains the mainTexture (what the camera sees)
    /// as well as the depthNormalsTexture (used for image effects)
    /// </summary>
    [Serializable]
    public class RecursiveTextures {
        public string portalName;
        public RenderTexture mainTexture;
        public RenderTexture depthNormalsTexture;

        public static RecursiveTextures CreateTextures(string name, string associatedPortalName) {
            int width = SuperspectiveScreen.instance.currentPortalWidth;
            int height = SuperspectiveScreen.instance.currentPortalHeight;
			
            RecursiveTextures recursiveTextures = new RecursiveTextures {
                mainTexture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR),
                depthNormalsTexture = new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, Portal.DEPTH_NORMALS_TEXTURE_FORMAT, RenderTextureReadWrite.Linear)
            };
            recursiveTextures.mainTexture.name = $"{name}_MainTex";
            recursiveTextures.depthNormalsTexture.name = $"{name}_DepthNormals";
            recursiveTextures.portalName = associatedPortalName;
            return recursiveTextures;
        }

        public void Release() {
            if (mainTexture != null) {
                mainTexture.Release();
                GameObject.Destroy(mainTexture);
            }

            if (depthNormalsTexture != null) {
                depthNormalsTexture.Release();
                GameObject.Destroy(depthNormalsTexture);
            }
        }
    }
}