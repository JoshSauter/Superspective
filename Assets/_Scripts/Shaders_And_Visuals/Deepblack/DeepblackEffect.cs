using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using SuperspectiveUtils;

// Inverse Bloom effect darkening space around certain objects
namespace Deepblack {
    /// <summary>
    /// Applies an inverse bloom effect around objects that are marked as DeepblackObjects,
    /// as long as they have the Deepblack material applied to them which flags these objects in the color buffer with a pixel value of no color at all.
    ///
    /// Here is a brief overview of how the effect works:
    /// 0) A simple CommandBuffer is used to render a _DarknessMask texture which is used to pass the "darkness" value of DeepblackObjects to later steps.
    /// 1) The normal rendering process is performed, and DeepblackObjects are rendered to the main camera's color buffer with RGB(0,0,0) as a flag.
    ///    - This is a workaround to get DeepblackObjects to respect the depth buffer, because I cannot for the life of me figure out how to pass a depth buffer around.
    /// 2) The Deepblack shader is applied to the main camera's color buffer, which filters for these pitch black pixels,
    ///    and inverts their color into HDR white range with a brightness correlating to their "darkness" amount.
    /// 3) The output of the Deepblack shader is then passed through a bloom shader to create a bloom effect around these objects (this is why the HDR range values are important).
    /// 4) The inverse of the bloom-affected image is then multiplied with the original image to create the final effect.
    /// </summary>
    public class DeepblackEffect : Singleton<DeepblackEffect> {
        private const string DARKNESS_MASK_SHADER = "Hidden/DarknessMask";
        private const string DEEPBLACK_MASK_SHADER = "Hidden/DeepblackMask";
        private const string INVERSE_BLOOM_SHADER = "Hidden/InverseBloomComposite";

        private const string DARKNESS_MASK_TEXTURE = "_DarknessMask";
        private const string BLOOMED_DARKNESS_MASK_TEXTURE = "_BloomedDarknessMask";
        private const string DARKENING_INTENSITY_KEYWORD = "_DarkeningIntensity";
        
        private const string DARKNESS_VALUE_KEYWORD = "_Darkness";
        private const string FALLOFF_FACTOR_KEYWORD = "_FalloffFactor";

        public float intensityMultiplier = 1;
#if UNITY_EDITOR
        public bool enableInEditor = false;
#endif

        public bool AllowedToRender => true
#if UNITY_EDITOR
            && enableInEditor;
#else
            ;
#endif

        [Serializable]
        private class RenderTextures {
            public RenderTexture darknessMaskTexture;
            public RenderTexture deepblackTexture;
            public RenderTexture bloomedTexture;
            
            public bool NeedsReinit => 
                !darknessMaskTexture || !darknessMaskTexture.IsCreated() ||
                !deepblackTexture || !deepblackTexture.IsCreated() ||
                !bloomedTexture || !bloomedTexture.IsCreated();

            public void RecreateRenderTextures(int width, int height) {
                Debug.LogWarning($"Recreating render textures with size {width}x{height}");
                // Darkness mask texture
                if (darknessMaskTexture != null) {
                    darknessMaskTexture.Release();
                }
                darknessMaskTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf) {
                    name = "Darkness Mask",
                    useMipMap = false
                };
                darknessMaskTexture.Create();
                
                // Deepblack texture
                if (deepblackTexture != null) {
                    deepblackTexture.Release();
                }
                deepblackTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf) {
                    name = "Deepblack",
                    useMipMap = false
                };
                deepblackTexture.Create();
            
                // Bloomed texture
                if (bloomedTexture != null) {
                    bloomedTexture.Release();
                }
                bloomedTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf) {
                    name = "Bloomed Deepblack",
                    useMipMap = false
                };
                bloomedTexture.Create();
            }

            public void Release() {
                // Darkness mask texture
                if (darknessMaskTexture != null) {
                    darknessMaskTexture.Release();
                    darknessMaskTexture = null;
                }
                
                // Deepblack texture
                if (deepblackTexture != null) {
                    deepblackTexture.Release();
                    deepblackTexture = null;
                }
                // Bloomed texture
                if (bloomedTexture != null) {
                    bloomedTexture.Release();
                    bloomedTexture = null;
                }
            }
        }
        
        private readonly NullSafeHashSet<DeepblackObject> deepblackObjects = new NullSafeHashSet<DeepblackObject>();
        
#if UNITY_EDITOR
        // Show the HashSet as a List for debugging in the editor
        [ShowInInspector]
        private List<DeepblackObject> deepblackObjectsList => deepblackObjects.ToList();
#endif

        [ShowInInspector]
        private RenderTextures renderTextures = new RenderTextures();
        // We use this CommandBuffer to render the _DarknessMask texture
        private CommandBuffer darknessMaskCommandBuffer;

        private Shader darknessMaskShader;
        private Shader deepblackShader;
        private Shader inverseBloomShader;
        
        private Material darknessMaskMaterial;
        private Material deepblackMaterial;
        private Material inverseBloomMaterial;
        
        private new Camera camera;
        private FastBloom bloom;
        
        void Awake() {
            camera = SuperspectiveScreen.instance.playerCamera;
            bloom = camera.GetComponent<FastBloom>();
            renderTextures.RecreateRenderTextures(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);

            if (deepblackShader == null) {
                deepblackShader = Shader.Find(DEEPBLACK_MASK_SHADER);
            }
            deepblackMaterial = new Material(deepblackShader);
            
            if (inverseBloomShader == null) {
                inverseBloomShader = Shader.Find(INVERSE_BLOOM_SHADER);
            }
            inverseBloomMaterial = new Material(inverseBloomShader);
            
            if (darknessMaskShader == null) {
                darknessMaskShader = Shader.Find(DARKNESS_MASK_SHADER);
            }
            darknessMaskMaterial = new Material(darknessMaskShader);
            
            SuperspectiveScreen.instance.OnScreenResolutionChanged += renderTextures.RecreateRenderTextures;
        }

        private void OnDisable() {
            if (darknessMaskCommandBuffer != null) {
                darknessMaskCommandBuffer.Dispose();
                darknessMaskCommandBuffer = null;
            }
        }

        private CommandBuffer CreateDarknessMaskCommandBuffer() {
            CommandBuffer commandBuffer = new CommandBuffer();
            commandBuffer.name = "Darkness Mask";
            commandBuffer.SetRenderTarget(renderTextures.darknessMaskTexture);
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            foreach (var deepblackObject in deepblackObjects) {
                // Update the darkness value
                // Note that we have to use a global float in order to update it from the CommandBuffer
                commandBuffer.SetGlobalFloat(DARKNESS_VALUE_KEYWORD, deepblackObject.darkness);
                commandBuffer.SetGlobalFloat(FALLOFF_FACTOR_KEYWORD, deepblackObject.falloffFactor);
                
                // Render all the meshes of the DeepblackObject to the _DarknessMask texture with the specified darkness value as the brightness
                foreach (var meshRenderer in deepblackObject.rendererMeshes.Keys) {
                    commandBuffer.DrawRenderer(meshRenderer, darknessMaskMaterial);
                }
            }
            commandBuffer.SetGlobalTexture(DARKNESS_MASK_TEXTURE, renderTextures.darknessMaskTexture);

            return commandBuffer;
        }

        public void Register(DeepblackObject obj) {
            if (!obj) return;
            
            deepblackObjects.Add(obj);
        }

        public void Unregister(DeepblackObject obj) {
            deepblackObjects.Remove(obj);
        }

        [ImageEffectOpaque]
        void OnRenderImage(RenderTexture src, RenderTexture dest) {
            // If the game is loading, skip the effect
            if (GameManager.instance.IsCurrentlyLoading || !AllowedToRender) {
                Graphics.Blit(src, dest);
                return;
            }
            // Renders the darkness mask according to the darkness values of the DeepblackObjects, and blooms the resulting image
            RenderDeepblackObjectMask(src);
            
            // Passes the bloomed darkness mask to the inverse bloom composite shader
            inverseBloomMaterial.SetTexture(BLOOMED_DARKNESS_MASK_TEXTURE, renderTextures.bloomedTexture);
            inverseBloomMaterial.SetFloat(DARKENING_INTENSITY_KEYWORD, intensityMultiplier);
            
            // Applies the inverse bloom effect to the main camera's color buffer
            Graphics.Blit(src, dest, inverseBloomMaterial);
        }
        
        private void RenderDeepblackObjectMask(RenderTexture src) {
            // Recreate anything missing:
            if (renderTextures.NeedsReinit) {
                renderTextures.RecreateRenderTextures(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);
            }
            
            // This looks inefficient, but it's necessary to recreate the CommandBuffer every frame to update the darkness values of the DeepblackObjects
            // Otherwise, the values captured in a closure when the CommandBuffer is first created are used for that CommandBuffer forever
            darknessMaskCommandBuffer?.Clear();
            darknessMaskCommandBuffer = CreateDarknessMaskCommandBuffer();
            
            // Renders Darkness values into the _DarknessMask texture
            Graphics.ExecuteCommandBuffer(darknessMaskCommandBuffer);
            
            // Looks for flagged 0-color pixels in the color buffer and inverts them into HDR white range with a brightness correlating to their "darkness" amount
            Graphics.Blit(src, renderTextures.deepblackTexture, deepblackMaterial);
            
            // Passes the Deepblack texture through a bloom shader to create a bloom effect around these objects
            bloom.PerformBloom(renderTextures.deepblackTexture, renderTextures.bloomedTexture);
        }
    }
}
