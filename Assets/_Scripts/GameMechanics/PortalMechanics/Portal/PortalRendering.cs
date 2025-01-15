using System;
using System.Collections.Generic;
using System.Linq;
using DimensionObjectMechanics;
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
        private const float TIME_TO_WAIT_AFTER_TELEPORT_BEFORE_VP_IS_DISABLED = 1f;
        private const int FRAMES_TO_WAIT_BEFORE_DISABLING_VP = 10;
        
        public static bool forceDebugRenderMode = Application.isEditor;
        public static bool forceVolumetricPortalsOn = false;
        
        private static Material _sharedPortalMaterial;
        private static Material SharedPortalMaterial => _sharedPortalMaterial ??= new Material(Resources.Load<Shader>(SHADER_PATH));
        
        [SerializeField]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
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
                                                         (otherPortal && otherPortal.TimeSinceLastTeleport < TIME_TO_WAIT_AFTER_TELEPORT_BEFORE_VP_IS_DISABLED);
        
        [SerializeField]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        private bool renderRecursivePortals = false;
        public bool RenderRecursivePortals => !Application.isEditor && renderRecursivePortals;
        public float VolumetricPortalThickness => volumetricPortalThickness * transform.localScale.z;

        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public bool turnOnNormalPortalRenderingWhenPlayerTeleports = false;
        
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public bool changeCameraEdgeDetection;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public BladeEdgeDetection.EdgeColorMode edgeColorMode;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public Color edgeColor = Color.black;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public Gradient edgeColorGradient;
        [ShowIf(nameof(changeCameraEdgeDetection))]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public Texture2D edgeColorGradientTexture;
        
        [SerializeField]
        [ShowIf(nameof(DEBUG))]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        private RecursiveTextures internalRenderTexturesCopy;
        
        // Will be same as SharedPortalMaterial in most cases, but may reference a different material if e.g. the Portal is a DimensionObject
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public Material portalMaterial;
        
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public bool skipIsVisibleCheck = false; // Useful for very large portals where the isVisible check doesn't work well
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public bool pauseRenderingWhenNotInActiveScene = false;
        
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        public SuperspectiveRenderer[] renderers;
        [SerializeField]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        private SuperspectiveRenderer[] volumetricPortals;
        
        [SerializeField]
        [TabGroup("Rendering"), GUIColor(0.35f, 0.75f, .9f)]
        private float volumetricPortalThickness = 1f;
        private int consecutiveFramesVPShouldBeDisabled = 0;

        private void RenderingAwake() {
            portalMaterial = SharedPortalMaterial;
            gameObject.layer = SuperspectivePhysics.PortalLayer;

            InitializeRenderers();
            foreach (var r in renderers) {
                r.gameObject.layer = SuperspectivePhysics.PortalLayer;
                r.SetSharedMaterial(portalMaterial);
                r.SetFloat(PORTAL_RENDERING_MODE_PROPERTY, (int)EffectiveRenderMode);
                if (changeScale) {
                    r.SetFloat("_PortalScaleFactor", scaleFactor);
                }
            }

            InitializeVolumetricPortals();
        }
        
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
        
        public bool IsVisibleFrom(Camera cam) {
            if (skipIsVisibleCheck) {
                // Still don't render portals that are very far away
                Vector3 closestPoint = ClosestPoint(Player.instance.PlayerCam.transform.position, true, true);
                return Vector3.Distance(closestPoint, Player.instance.PlayerCam.transform.position) < Player.instance.PlayerCam.farClipPlane;
            }
			
            return renderers.Any(r => r.r.IsVisibleFrom(cam)) || volumetricPortals.Any(vp => vp.r.IsVisibleFrom(cam));
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
        
        public void EnableVolumetricPortal() {
            bool anyVolumetricPortalIsDisabled = volumetricPortals.Any(vp => !vp.enabled);
            if (anyVolumetricPortalIsDisabled) {
                // Don't spam the console when we have the volumetric portal debug setting on
                if (!forceVolumetricPortalsOn) {
                    debug.Log("Enabling Volumetric Portal(s) for " + gameObject.name);
                }
                foreach (var vp in volumetricPortals) {
                    if (!PortalRenderingIsEnabled) continue;
					
                    vp.SetSharedMaterial(portalMaterial);
                    vp.enabled = true;
                }
            }
        }

        public void DisableVolumetricPortal() {
            bool anyVolumetricPortalIsEnabled = volumetricPortals.Any(vp => vp.enabled);
            if (anyVolumetricPortalIsEnabled) {
                debug.Log("Disabling Volumetric Portal(s) for " + gameObject.name);

                foreach (var vp in volumetricPortals) {
                    vp.enabled = false;
                }
            }
        }
        
        // Allocate once to save GC every frame
		readonly float[] floatGradientBuffer = new float[BladeEdgeDetection.GradientArraySize];
		readonly Color[] colorGradientBuffer = new Color[BladeEdgeDetection.GradientArraySize];

		private BladeEdgeDetection EdgeDetection => MaskBufferRenderTextures.instance.edgeDetection;
		BladeEdgeDetection.EdgeColorMode EdgeColorMode => changeCameraEdgeDetection ? edgeColorMode : EdgeDetection.edgeColorMode;
		Color EdgeColor => changeCameraEdgeDetection ? edgeColor : EdgeDetection.edgeColor;
		Gradient EdgeColorGradient => changeCameraEdgeDetection ? edgeColorGradient : EdgeDetection.edgeColorGradient;
		
		void SetEdgeDetectionColorProperties() {
			portalMaterial.SetInt(BladeEdgeDetection.ColorModeID, (int)EdgeColorMode);
			switch (EdgeColorMode) {
				case BladeEdgeDetection.EdgeColorMode.SimpleColor:
					portalMaterial.SetColor(BladeEdgeDetection.EdgeColorID, EdgeColor);
					break;
				case BladeEdgeDetection.EdgeColorMode.Gradient:
					SetEdgeColorGradient();
					break;
				case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
					portalMaterial.SetTexture(BladeEdgeDetection.GradientTextureID, edgeColorGradientTexture);
					break;
			}
		}
		
#region Helper Methods
		/// <summary>
		/// Sets the _GradientKeyTimes and _EdgeColorGradient float and Color arrays, respectively, in the BladeEdgeDetectionShader
		/// Populates _GradientKeyTimes with the times of each colorKey in edgeColorGradient (as well as a 0 as the first key and a series of 1s to fill out the array at the end)
		/// Populates _EdgeColorGradient with the colors of each colorKey in edgeColorGradient (as well as values for the times filled in as described above)
		/// </summary>
		void SetEdgeColorGradient() {
			Color startColor = EdgeColorGradient.Evaluate(0);
			Color endColor = EdgeColorGradient.Evaluate(1);
			float startAlpha = startColor.a;
			float endAlpha = endColor.a;
	
			portalMaterial.SetFloatArray(BladeEdgeDetection.GradientKeyTimesID, GetGradientFloatValues(0f, EdgeColorGradient.colorKeys.Select(x => x.time), 1f));
			portalMaterial.SetColorArray(BladeEdgeDetection.EdgeColorGradientID, GetGradientColorValues(startColor, EdgeColorGradient.colorKeys.Select(x => x.color), endColor));
			portalMaterial.SetFloatArray(BladeEdgeDetection.GradientAlphaKeyTimesID, GetGradientFloatValues(0f, EdgeColorGradient.alphaKeys.Select(x => x.time), 1f));
			portalMaterial.SetFloatArray(BladeEdgeDetection.AlphaGradientID, GetGradientFloatValues(startAlpha, EdgeColorGradient.alphaKeys.Select(x => x.alpha), endAlpha));
	
			portalMaterial.SetInt(BladeEdgeDetection.GradientModeID, EdgeColorGradient.mode == GradientMode.Blend ? 0 : 1);
	
			SetFrustumCornersVector();
		}
	
		void SetFrustumCornersVector() {
			portalMaterial.SetVectorArray(BladeEdgeDetection.FrustumCorners, EdgeDetection.frustumCornersOrdered);
		}
	
		// Actually just populates the float buffer with the values provided, then returns a reference to the float buffer
		float[] GetGradientFloatValues(float startValue, IEnumerable<float> middleValues, float endValue) {
			float[] middleValuesArray = middleValues.ToArray();
			floatGradientBuffer[0] = startValue;
			for (int i = 1; i < middleValuesArray.Length + 1; i++) {
				floatGradientBuffer[i] = middleValuesArray[i - 1];
			}
			for (int j = middleValuesArray.Length + 1; j < BladeEdgeDetection.GradientArraySize; j++) {
				floatGradientBuffer[j] = endValue;
			}
			return floatGradientBuffer;
		}
	
		// Actually just populates the color buffer with the values provided, then returns a reference to the color buffer
		Color[] GetGradientColorValues(Color startValue, IEnumerable<Color> middleValues, Color endValue) {
			Color[] middleValuesArray = middleValues.ToArray();
			colorGradientBuffer[0] = startValue;
			for (int i = 1; i < middleValuesArray.Length + 1; i++) {
				colorGradientBuffer[i] = middleValuesArray[i - 1];
			}
			for (int j = middleValuesArray.Length + 1; j < BladeEdgeDetection.GradientArraySize; j++) {
				colorGradientBuffer[j] = endValue;
			}
			return colorGradientBuffer;
		}

		void SwapEdgeDetectionColors() {
			BladeEdgeDetection playerED = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

			EDColors tempEDColors = new EDColors {
				edgeColorMode = playerED.edgeColorMode,
				edgeColor = playerED.edgeColor,
				edgeColorGradient = playerED.edgeColorGradient,
				edgeColorGradientTexture = playerED.edgeColorGradientTexture
			};

			CopyEdgeColors(from: this, to: playerED);

			otherPortal.changeCameraEdgeDetection = true;
			CopyEdgeColors(from: tempEDColors, to: otherPortal);
		}
        
        private static void CopyEdgeColors(Portal from, BladeEdgeDetection to) {
            to.edgeColorMode = from.edgeColorMode;
            to.edgeColor = from.edgeColor;
            to.edgeColorGradient = from.edgeColorGradient;
            to.edgeColorGradientTexture = from.edgeColorGradientTexture;
        }

        public static void CopyEdgeColors(EDColors from, Portal to) {
            to.edgeColorMode = from.edgeColorMode;
            to.edgeColor = from.edgeColor;
            to.edgeColorGradient = from.edgeColorGradient;
            to.edgeColorGradientTexture = from.edgeColorGradientTexture;
        }
#endregion
    }
}