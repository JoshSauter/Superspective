using System;
using System.Collections.Generic;
using System.Linq;
using DimensionObjectMechanics;
using Saving;
using Sirenix.OdinInspector;
using SuperspectiveUtils;
using UnityEngine;

namespace PortalMechanics {
    public enum PortalRenderMode : byte {
        Normal = 0,    // Render the portal as normal
        Debug = 1,     // Render the grid lines showing the location of portals only
        Invisible = 2, // Don't render the portal surface at all
        Wall = 3       // Render the portal surface as a wall of flat color
    }
    
    public partial class Portal {
        public const RenderTextureFormat DEPTH_NORMALS_TEXTURE_FORMAT = RenderTextureFormat.ARGB32;
        private const string PORTAL_RENDERING_MODE_PROPERTY = "_PortalRenderingMode";
        private const string SHADER_PATH = "Shaders/Suberspective/SuberspectivePortal";
        private const string VOLUMETRIC_PORTAL_NAME = "Volumetric Portal";
        
        public static bool forceDebugRenderMode = Application.isEditor;
        public static bool forceVolumetricPortalsOn = false;
        
        private static Material _sharedPortalMaterial;
        private static Material SharedPortalMaterial => _sharedPortalMaterial ??= new Material(Resources.Load<Shader>(SHADER_PATH));
        
        [SerializeField]
        private PortalRenderMode _renderMode = PortalRenderMode.Normal;
        public PortalRenderMode RenderMode {
            get => _renderMode;
            set {
                _renderMode = value;
                ApplyPortalRenderingModeToRenderers();
            }
        }
        // This is what is actually rendering to the screen, rather than the logical RenderMode		
        private PortalRenderMode EffectiveRenderMode => forceDebugRenderMode ? PortalRenderMode.Debug : RenderMode;
        
        public bool PortalRenderingIsEnabled => otherPortal != null &&
                                                gameObject.activeSelf &&
                                                RenderMode == PortalRenderMode.Normal &&
                                                (!pauseRenderingWhenNotInActiveScene || IsInActiveScene);
        
        private bool VolumetricPortalsShouldBeEnabled => forceVolumetricPortalsOn ||
                                                         PlayerRemainsInPortal ||
                                                         TimeSinceLastTeleport < TIME_TO_WAIT_AFTER_TELEPORT_BEFORE_VP_IS_DISABLED;
        
        [SerializeField]
        private bool renderRecursivePortals = false;
        public bool RenderRecursivePortals => !Application.isEditor && renderRecursivePortals;
        public float VolumetricPortalThickness => volumetricPortalThickness * transform.localScale.z;
        
        
        public bool changeCameraEdgeDetection;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        public BladeEdgeDetection.EdgeColorMode edgeColorMode;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        public Color edgeColor = Color.black;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        public Gradient edgeColorGradient;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        public Texture2D edgeColorGradientTexture;
        
        [SerializeField]
        [ShowIf(nameof(DEBUG))]
        private RecursiveTextures internalRenderTexturesCopy;
        
        // Will be same as SharedPortalMaterial in most cases, but may reference a different material if e.g. the Portal is a DimensionObject
        public Material portalMaterial;
        
        public bool skipIsVisibleCheck = false; // Useful for very large portals where the isVisible check doesn't work well
        public bool pauseRenderingWhenNotInActiveScene = false;
        
        public SuperspectiveRenderer[] renderers;
        [SerializeField]
        private SuperspectiveRenderer[] volumetricPortals;
        
        [SerializeField]
        private float volumetricPortalThickness = 1f;
        private int consecutiveFramesVPShouldBeDisabled = 0;
        
        private void InitializeRenderers() {
            if (!(renderers == null || renderers.Length == 0)) return;
			
            if (compositePortal) {
                renderers = GetComponentsInChildren<Renderer>()
                    .Select(r => r.GetOrAddComponent<SuperspectiveRenderer>()).ToArray();
            }
            else {
                renderers = new SuperspectiveRenderer[] { GetComponents<Renderer>().Select(r => r.GetOrAddComponent<SuperspectiveRenderer>()).FirstOrDefault() };
            }
        }

        private void InitializeVolumetricPortals() {
            // Clean up extra Volumetric Portals
            foreach (var existingVolumetricPortal in transform.GetChildrenMatchingNameRecursively(VOLUMETRIC_PORTAL_NAME)) {
                if (volumetricPortals != null && volumetricPortals.ToList().Exists(vp => vp.transform == existingVolumetricPortal)) continue;
				
                DestroyImmediate(existingVolumetricPortal.gameObject);
            }

            volumetricPortals = volumetricPortals?.Where(vp => vp != null).ToArray();

            if (volumetricPortals != null && volumetricPortals.Length > 0) {
                foreach (var vp in volumetricPortals){
                    vp.gameObject.layer = SuperspectivePhysics.VolumetricPortalLayer;
                }
                return;
            }

            List<SuperspectiveRenderer> volumetricPortalsList = new List<SuperspectiveRenderer>();
            foreach (SuperspectiveRenderer r in renderers) {
                try {
                    SuperspectiveRenderer vp = GenerateExtrudedMesh(r.GetComponent<MeshFilter>(), VolumetricPortalThickness)
                        .GetOrAddComponent<SuperspectiveRenderer>();

                    vp.enabled = false;
                    vp.SetSharedMaterial(portalMaterial);
                    vp.gameObject.layer = SuperspectivePhysics.VolumetricPortalLayer;
                    volumetricPortalsList.Add(vp);
                }
                catch (Exception e) {
                    Debug.LogError($"{ID} in scene {gameObject.scene.name} failed to build volumetric portal, error: {e.StackTrace}");
                }
            }

            volumetricPortals = volumetricPortalsList.ToArray();
        }
        
        private void CreateRenderTexture(int width, int height) {
            // Not sure why but it seems sometimes the Portals don't get OnDisable called when scene unloaded
            if (this == null) {
                OnDisable();
                return;
            }
            debug.Log($"Creating render textures for new resolution {width}x{height}");
            if (internalRenderTexturesCopy != null && (internalRenderTexturesCopy.mainTexture != null || internalRenderTexturesCopy.depthNormalsTexture != null)) {
                internalRenderTexturesCopy.Release();
            }
            internalRenderTexturesCopy = RecursiveTextures.CreateTextures(ID, $"{channel}: {name}");
            SetPropertiesOnMaterial();
        }

        private void SetPropertiesOnMaterial() {
            if (!PortalRenderingIsEnabled) return;
			
            void SetTexturesForRenderers(SuperspectiveRenderer[] portalRenderers) {
                foreach (var r in portalRenderers) {
                    r.SetTexture("_MainTex", internalRenderTexturesCopy.mainTexture);
                    r.SetTexture("_DepthNormals", internalRenderTexturesCopy.depthNormalsTexture);
                }
            }

            SetTexturesForRenderers(renderers);
            SetTexturesForRenderers(volumetricPortals);
        }
        
        // When a Portal is part of a DimensionObject, a new Material will be created with the DIMENSION_OBJECT keyword enabled
        // This updates our Material reference to the new dimension object material
        private void HandleMaterialChanged(Material newMaterial) {
            if (newMaterial.name.EndsWith(DimensionObjectManager.DIMENSION_OBJECT_SUFFIX)) {
                portalMaterial = newMaterial;
            }
        }
        
        // Called before render process begins, either enable or disable the volumetric portals for this frame
        private void LateUpdate() {
            SetEdgeDetectionColorProperties();
            //debug.Log(volumetricPortalsShouldBeEnabled);
            if (VolumetricPortalsShouldBeEnabled) {
                EnableVolumetricPortal();
				
                consecutiveFramesVPShouldBeDisabled = 0;
            }
            else {
                // Replacing with delayed disabling of VP
                // DisableVolumetricPortal();
                if (consecutiveFramesVPShouldBeDisabled > FRAMES_TO_WAIT_BEFORE_DISABLING_VP) {
                    DisableVolumetricPortal();
                }
				
                consecutiveFramesVPShouldBeDisabled++;
            }
        }
        
        public bool IsVolumetricPortalEnabled() {
            return volumetricPortals.Any(vp => vp.enabled && vp.gameObject.layer != SuperspectivePhysics.InvisibleLayer);
        }

        public void SetVolumetricHiddenForPortalRendering(bool hidden) {
            int targetLayer = hidden ? SuperspectivePhysics.InvisibleLayer : SuperspectivePhysics.VolumetricPortalLayer;
            foreach (SuperspectiveRenderer vp in volumetricPortals) {
                vp.gameObject.layer = targetLayer;
            }
        }
        
        public void SetTexture(RenderTexture tex) {
            if (!PortalRenderingIsEnabled) {
                debug.LogWarning($"Attempting to set MainTexture for disabled portal: {gameObject.FullPath()}");
                return;
            }
			
            if (internalRenderTexturesCopy.mainTexture == null) { 
                Debug.LogWarning($"Attempting to set MainTexture for portal w/ null mainTexture: {gameObject.FullPath()}");
                return;
            }

            ApplyPortalRenderingModeToRenderers();

            Graphics.CopyTexture(tex, internalRenderTexturesCopy.mainTexture);
            SetPropertiesOnMaterial();
        }

        public void SetDepthNormalsTexture(RenderTexture tex) {
            if (!PortalRenderingIsEnabled) {
                debug.LogWarning($"Attempting to set DepthNormalsTexture for disabled portal: {gameObject.FullPath()}");
                return;
            }

            if (internalRenderTexturesCopy.depthNormalsTexture == null) { 
                Debug.LogWarning($"Attempting to set DepthNormalsTexture for portal w/ null depthNormalsTexture: {gameObject.FullPath()}");
                return;
            }

            ApplyPortalRenderingModeToRenderers();
			
            Graphics.CopyTexture(tex, internalRenderTexturesCopy.depthNormalsTexture);
            SetPropertiesOnMaterial();
        }
        
        /// <summary>
        /// Applies the current EffectiveRenderMode to the Portal renderers.
        /// </summary>
        public void ApplyPortalRenderingModeToRenderers() {
            int renderMode = (int)EffectiveRenderMode;
            Vector3 intoPortal = IntoPortalVector;
			
            foreach (SuperspectiveRenderer r in renderers) {
                r.SetFloat(PORTAL_RENDERING_MODE_PROPERTY, renderMode);
                r.SetVector(PORTAL_NORMAL_PROPERTY, intoPortal);
            }

            foreach (SuperspectiveRenderer vp in volumetricPortals) {
                vp.SetFloat(PORTAL_RENDERING_MODE_PROPERTY, renderMode);
                vp.SetVector(PORTAL_NORMAL_PROPERTY, intoPortal);
            }
        }
    }
}