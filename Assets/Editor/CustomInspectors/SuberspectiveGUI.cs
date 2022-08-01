// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using SuperspectiveUtils;
using SuperspectiveUtils.ShaderUtils;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using BlendMode = UnityEngine.Rendering.BlendMode;
using Object = UnityEngine.Object;

namespace UnityEditor {
    internal class SuberspectiveGUI : ShaderGUI, ISuberspectiveGUI {
        private ShaderVariantCollection shaderVariants;
        protected HashSet<string> keywordsEnabled = new HashSet<string>();
        private Shader _shader;

        public virtual Shader GetShader() {
            if (_shader == null) {
                _shader = Shader.Find("Suberspective/SuberspectiveUnlit");
            }

            return _shader;
        }

        public virtual PassType[] GetPassTypes() => new[] { PassType.Normal };

        // We use MaterialProperty here instead of enum to make use of hasMixedValue property on MaterialProperty
        private MaterialProperty blendMode;
        private const string blendModeText = "Blend Mode";
        private static readonly string[] blendNames = Enum.GetNames(typeof(ShaderUtils.SuberspectiveBlendMode));

        private MaterialProperty blendOp;
        private const string blendOpText = "Blend Op";
        private static readonly string[] blendOpNames = Enum.GetNames(typeof(BlendOp));

        private MaterialProperty srcBlend;
        private MaterialProperty dstBlend;
        private const string srcBlendText = "From Shader";
        private const string dstBlendText = "From Screen";
        private static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

        private MaterialProperty cullMode;
        private const string cullModeText = "Cull Mode";
        private static readonly string[] cullNames = Enum.GetNames(typeof(CullMode));
        
        private const string dimensionObjectKeyword = "DIMENSION_OBJECT";
        private const string dimensionPropertyText = "Dimension Object";
        private const string dissolveObjectKeyword = "DISSOLVE_OBJECT";
        private const string dissolvePropertyText = "Dissolve Object";
        private const string powerTrailObjectKeyword = "POWER_TRAIL_OBJECT";
        private const string powerTrailPropertyText = "PowerTrail Object";
        private const string shutteredObjectKeyword = "SHUTTERED_OBJECT";
        private const string shutteredPropertyText = "Shuttered Object (BlackRoom3)";
        private const string portalCopyKeyword = "PORTAL_COPY_OBJECT";
        private const string portalCopyText = "Portal Copy Object";
        private const string renderInZoneKeyword = "RENDER_IN_ZONE_OBJECT";
        private const string renderInZoneText = "Render In Zone Object";

        private MaterialProperty mainTex;
        private const string mainTexText = "Main Texture";
        private MaterialProperty color;
        private const string colorText = "Main Color";
        private MaterialProperty emissionEnabled;
        private const string emissionEnabledText = "Emission";
        private MaterialProperty emissionMap;
        private const string emissionMapText = "Emission Map";
        private MaterialProperty emissionColor;
        private const string emissionColorText = "Emission Color";
        public static readonly GUIContent mainTexturePropertyText = EditorGUIUtility.TrTextContent(
            "Main Texture",
            "Main Texture and Color"
        );
        public static readonly GUIContent emissionTexturePropertyText = EditorGUIUtility.TrTextContent(
            "Emission Map",
            "Emission Map and Color"
        );
        
        // DimensionObject
        private MaterialProperty dimensionInverse;
        private MaterialProperty dimensionChannel;
        private const string dimensionInverseText = "Invert";
        private const string dimensionChannelText = "Channel";
        // DissolveObject
        private MaterialProperty dissolveTex;
        private MaterialProperty dissolveValue;
        private MaterialProperty dissolveBurnColor;
        private MaterialProperty dissolveBurnSize;
        private MaterialProperty dissolveBurnRamp;
        private MaterialProperty dissolveEmissionAmount;
        private const string dissolveTexText = "Dissolve Tex";
        private const string dissolveValueText = "Dissolve Value";
        private const string dissolveBurnColorText = "Burn Color";
        private const string dissolveBurnSizeText = "Burn Size";
        private const string dissolveBurnRampText = "Burn Ramp";
        private const string dissolveEmissionAmountText = "Emission";
        // PowerTrail
        private MaterialProperty powerTrailCapsuleRadius;
        private MaterialProperty powerTrailInverse;
        private const string powerTrailCapsuleRadiusText = "SDF Capsule Radius";
        private const string powerTrailInverseText = "Invert";
        // Other effects:
        private bool otherEffectsEnabled = false;
        private const string otherEffectsEnabledText = "Other effects";
        private static readonly HashSet<string> otherEffectsKeywords = new HashSet<string>() {
            shutteredObjectKeyword,
            portalCopyKeyword,
            renderInZoneKeyword
        };
        // AffectedByShutters
        private MaterialProperty shutterNoise;
        private MaterialProperty shutterInverse;
        private const string shutterNoiseText = "Noise texture";
        private const string shutterInverseText = "Inverse";
        // PortalCopy
        private MaterialProperty portalPos;
        private MaterialProperty portalNormal;
        private MaterialProperty fudgeDistance;
        private const string portalPosText = "Portal Position";
        private const string portalNormalText = "Portal Normal";
        private const string fudgeDistanceText = "Fudge position";
        // RenderInZone
        private MaterialProperty minRenderZone;
        private MaterialProperty maxRenderZone;
        private const string minRenderZoneText = "Min Render Zone";
        private const string maxRenderZoneText = "Max Render Zone";

        protected MaterialEditor editor;

        protected virtual void FindProperties(MaterialProperty[] props) {
            if (shaderVariants == null) {
                shaderVariants = Resources.Load<ShaderVariantCollection>(
                    "Materials/Suberspective/SuberspectiveVariantCollection");
            }
            
            blendMode = FindProperty("__SuberspectiveBlendMode", props);
            blendOp = FindProperty("__BlendOp", props);
            srcBlend = FindProperty("__SrcBlend", props);
            dstBlend = FindProperty("__DstBlend", props);
            cullMode = FindProperty("__CullMode", props);

            mainTex = FindProperty("_MainTex", props);
            color = FindProperty("_Color", props);
            emissionEnabled = FindProperty("_EmissionEnabled", props, false);
            emissionMap = FindProperty("_EmissionMap", props);
            emissionColor = FindProperty("_EmissionColor", props);
            
            dimensionInverse = FindProperty("_Inverse", props, false);
            dimensionChannel = FindProperty("_Channel", props, false);
            
            dissolveTex = FindProperty("_DissolveTex", props, false);
            dissolveValue = FindProperty("_DissolveValue", props, false);
            dissolveBurnSize = FindProperty("_DissolveBurnSize", props, false);
            dissolveBurnRamp = FindProperty("_DissolveBurnRamp", props);
            dissolveBurnColor = FindProperty("_DissolveBurnColor", props, false);
            dissolveEmissionAmount = FindProperty("_DissolveEmissionAmount", props, false);

            powerTrailCapsuleRadius = FindProperty("_CapsuleRadius", props, false);
            powerTrailInverse = FindProperty("_ReverseVisibility", props, false);

            shutterNoise = FindProperty("_ShutterNoise", props, false);
            shutterInverse = FindProperty("_ShutterInverse", props, false);

            portalPos = FindProperty("_PortalPos", props, false);
            portalNormal = FindProperty("_PortalNormal", props, false);
            fudgeDistance = FindProperty("_FudgeDistance", props, false);
            
            minRenderZone = FindProperty("_MinRenderZone", props, false);
            maxRenderZone = FindProperty("_MaxRenderZone", props, false);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props) {
            // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            FindProperties(props);
            Init(materialEditor);
            Material material = materialEditor.target as Material;

            HashSet<string> prevKeywordsEnabled = new HashSet<string>(keywordsEnabled);
            
            SuberspectiveLabel(material);
            ShowGUI(material);

            if (!keywordsEnabled.SetEquals(prevKeywordsEnabled)) {
                KeywordsChanged();
            }
        }

        protected virtual void ShowGUI(Material material) {
            ShaderPropertiesGUI(material);
        }

        protected void Init(MaterialEditor materialEditor) {
            editor = materialEditor;
        }

        void ShaderPropertiesGUI(Material material) {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                SuberspectiveBlendModePopup(material);
                SuberspectiveCullModePopup(material);

                AddSeparator();

                SuberspectiveCustomBlendOpPopup(material);
                SuberspectiveCustomSrcBlendModes(material);
                SuberspectiveCustomDstBlendModes(material);
                
                AddSeparator();
                
                ColorProperty();

                EmissionProperty();
                
                SuberspectiveEffects(material);
                
                editor.RenderQueueField();
            }
        }

        private void SuberspectiveLabel(Material material) {
            // 27 for 27 characters in "Suberspective/Suberspective"
            EditorGUILayout.LabelField($"Shader type: {material.shader.name.Substring(27)}");
        }

        protected void SuberspectiveEffects(Material material) {
            AddSeparator();
            DimensionObjectProperties(material);
            AddSeparator();
            DissolveProperties(material);
            AddSeparator();
            PowerTrailProperties(material);
            AddSeparator();
            OtherEffectsProperties(material);
            AddSeparator();
        }

        void SuberspectiveBlendModePopup(Material material) {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            ShaderUtils.SuberspectiveBlendMode mode = (ShaderUtils.SuberspectiveBlendMode) blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (ShaderUtils.SuberspectiveBlendMode) EditorGUILayout.Popup(blendModeText, (int) mode, blendNames);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(blendModeText);
                blendMode.floatValue = (float) mode;
                SetupMaterialWithBlendMode(material, mode);
            }

            EditorGUI.showMixedValue = false;
        }

        void SuberspectiveCullModePopup(Material material) {
            EditorGUI.showMixedValue = cullMode.hasMixedValue;
            CullMode mode = (CullMode) cullMode.floatValue;
            
            EditorGUI.BeginChangeCheck();
            mode = (CullMode) EditorGUILayout.Popup(cullModeText, (int) mode, cullNames);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(cullModeText);
                cullMode.floatValue = (float) mode;
                SetupMaterialWithCullMode(material, mode);
            }

            EditorGUI.showMixedValue = false;
        }

        void SuberspectiveCustomBlendOpPopup(Material material) {
            EditorGUI.showMixedValue = blendOp.hasMixedValue;
            BlendOp mode = (BlendOp) blendOp.floatValue;
            
            EditorGUI.BeginChangeCheck();
            mode = (BlendOp) EditorGUILayout.Popup(blendOpText, (int) mode, blendOpNames);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(blendOpText);
                blendOp.floatValue = (float) mode;
            }

            EditorGUI.showMixedValue = false;
        }

        void SuberspectiveCustomSrcBlendModes(Material material) {
            EditorGUI.showMixedValue = srcBlend.hasMixedValue;
            BlendMode mode = (BlendMode)srcBlend.floatValue;
            
            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(srcBlendText, (int)mode, blendModeNames);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(srcBlendText);
                srcBlend.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }
        
        void SuberspectiveCustomDstBlendModes(Material material) {
            EditorGUI.showMixedValue = dstBlend.hasMixedValue;
            BlendMode mode = (BlendMode)dstBlend.floatValue;
            
            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(dstBlendText, (int)mode, blendModeNames);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(dstBlendText);
                dstBlend.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        #region MainProperties
        void ColorProperty() {
            editor.TexturePropertySingleLine(mainTexturePropertyText, mainTex, color);
        }

        void EmissionProperty() {
            EditorGUI.showMixedValue = emissionEnabled.hasMixedValue;
            bool enabled = emissionEnabled.floatValue > 0;

            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.Toggle(emissionEnabledText, enabled);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(emissionEnabledText);
                emissionEnabled.floatValue = enabled ? 1 : 0;
            }

            if (enabled) {
                DoEmissionArea();
            }
        }
        
        protected void DoEmissionArea() {
            bool hadEmissionTexture = emissionMap.textureValue != null;

            // Texture and HDR color controls
            editor.TexturePropertyWithHDRColor(emissionTexturePropertyText, emissionMap, emissionColor, false);

            // If texture was assigned and color was black set color to white
            float brightness = emissionColor.colorValue.maxColorComponent;
            if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f) {
                emissionColor.colorValue = Color.white;
            }
            
            // change the GI flag and fix it up with emissive as black if necessary
            editor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
        }

        #endregion

        #region Dimension Properties
        void DimensionObjectProperties(Material material) {
            bool dimensionPropertyIsEnabled = IsSuberspectiveKeywordEnabled(material, dimensionObjectKeyword);
            
            EditorGUI.BeginChangeCheck();
            dimensionPropertyIsEnabled = EditorGUILayout.Toggle($"{dimensionPropertyText}:", dimensionPropertyIsEnabled);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(dimensionPropertyText);
                SetSuberspectiveKeyword(material, dimensionObjectKeyword, dimensionPropertyIsEnabled);
            }

            EditorGUI.showMixedValue = false;

            EditorGUI.indentLevel += 1;
            if (dimensionPropertyIsEnabled) {
                DimensionInvertProperty();
                DimensionChannelProperty();
            }

            EditorGUI.indentLevel -= 1;
        }
        
        void DimensionInvertProperty() {
            bool inverted = dimensionInverse.floatValue > 0;
            
            EditorGUI.BeginChangeCheck();
            inverted = EditorGUILayout.Toggle($"{dimensionInverseText}:", inverted);

            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(dimensionInverseText);
                dimensionInverse.floatValue = inverted ? 1 : 0;
            }
        }

        void DimensionChannelProperty() {
            int channel = (int)dimensionChannel.floatValue;
            
            EditorGUI.BeginChangeCheck();
            channel = EditorGUILayout.IntSlider(dimensionChannelText, channel, 0, DimensionObject.NUM_CHANNELS);

            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(dimensionChannelText);
                dimensionChannel.floatValue = channel;
            }
        }
        #endregion

        #region Dissolve Properties
        void DissolveProperties(Material material) {
            bool dissolvePropertyIsEnabled = IsSuberspectiveKeywordEnabled(material, dissolveObjectKeyword);

            EditorGUI.BeginChangeCheck();
            dissolvePropertyIsEnabled = EditorGUILayout.Toggle($"{dissolvePropertyText}:", dissolvePropertyIsEnabled);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(dissolvePropertyText);
                SetSuberspectiveKeyword(material, dissolveObjectKeyword, dissolvePropertyIsEnabled);
            }

            EditorGUILayout.Space();
            if (dissolvePropertyIsEnabled) {
                DissolveTextureProperty();
                DissolveValueProperty();
                EditorGUILayout.Space();
                DissolveBurnSizeProperty();
                DissolveBurnRampProperty();
                DissolveBurnColorProperty();
                DissolveBurnEmissionAmountProperty();
            }

            EditorGUI.showMixedValue = false;
        }

        void DissolveTextureProperty() {
            editor.ShaderProperty(dissolveTex,dissolveTexText, MaterialEditor.kMiniTextureFieldLabelIndentLevel);
            
            if (dissolveTex.textureValue == null) {
                dissolveTex.textureValue = Resources.Load<Texture>("Materials/Suberspective/SuberspectiveDissolveTextureDefault");
            }
        }
        
        void DissolveValueProperty() {
            editor.ShaderProperty(dissolveValue, dissolveValueText, MaterialEditor.kMiniTextureFieldLabelIndentLevel);
        }

        void DissolveBurnSizeProperty() {
            editor.ShaderProperty(dissolveBurnSize, dissolveBurnSizeText, MaterialEditor.kMiniTextureFieldLabelIndentLevel);
        }

        void DissolveBurnRampProperty() {
            editor.ShaderProperty(dissolveBurnRamp, dissolveBurnRampText, MaterialEditor.kMiniTextureFieldLabelIndentLevel);
            
            if (dissolveBurnRamp.textureValue == null) {
                dissolveBurnRamp.textureValue = Resources.Load<Texture>("Materials/Suberspective/SuberspectiveDissolveBurnRampDefault");
            }
        }

        void DissolveBurnColorProperty() {
            editor.ShaderProperty(dissolveBurnColor, dissolveBurnColorText, MaterialEditor.kMiniTextureFieldLabelIndentLevel);
        }

        void DissolveBurnEmissionAmountProperty() {
            editor.ShaderProperty(dissolveEmissionAmount, dissolveEmissionAmountText, MaterialEditor.kMiniTextureFieldLabelIndentLevel);
        }
        #endregion
        
        #region Power Trail Properties
        void PowerTrailProperties(Material material) {
            bool powerTrailPropertyIsEnabled = IsSuberspectiveKeywordEnabled(material, powerTrailObjectKeyword);
            
            EditorGUI.BeginChangeCheck();
            powerTrailPropertyIsEnabled = EditorGUILayout.Toggle($"{powerTrailPropertyText}:", powerTrailPropertyIsEnabled);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(powerTrailPropertyText);
                SetSuberspectiveKeyword(material, powerTrailObjectKeyword, powerTrailPropertyIsEnabled);
            }

            EditorGUI.showMixedValue = false;

            EditorGUI.indentLevel += 1;
            if (powerTrailPropertyIsEnabled) {
                PowerTrailCapsuleRadiusProperty();
                PowerTrailInverseProperty();
            }

            EditorGUI.indentLevel -= 1;
        }

        void PowerTrailCapsuleRadiusProperty() {
            editor.ShaderProperty(powerTrailCapsuleRadius, powerTrailCapsuleRadiusText);
        }

        void PowerTrailInverseProperty() {
            bool inverted = powerTrailInverse.floatValue > 0;
            
            EditorGUI.BeginChangeCheck();
            inverted = EditorGUILayout.Toggle($"{powerTrailInverseText}:", inverted);

            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(powerTrailInverseText);
                powerTrailInverse.floatValue = inverted ? 1 : 0;
            }
        }
        #endregion
        
        #region Other Effects

        void OtherEffectsProperties(Material material) {
            otherEffectsEnabled = EditorGUILayout.Toggle(otherEffectsEnabledText, otherEffectsEnabled);

            otherEffectsEnabled |= otherEffectsKeywords.Any(material.IsKeywordEnabled);

            EditorGUI.indentLevel++;
            if (otherEffectsEnabled) {
                ShuttersProperties(material);
                PortalCopyProperties(material);
                RenderInZoneProperties(material);
            }
            EditorGUI.indentLevel--;
        }
        
        #region Shutters Properties
        void ShuttersProperties(Material material) {
            bool shuttersPropertyIsEnabled = SuberspectiveKeywordProperties(material, shutteredObjectKeyword, shutteredPropertyText);

            EditorGUI.indentLevel += 1;
            if (shuttersPropertyIsEnabled) {
                ShutterNoiseProperty();
                ShutterInverseProperty();
            }
            EditorGUI.indentLevel -= 1;
        }

        void ShutterNoiseProperty() {
            editor.ShaderProperty(shutterNoise,shutterNoiseText, MaterialEditor.kMiniTextureFieldLabelIndentLevel);
            
            if (shutterNoise.textureValue == null) {
                shutterNoise.textureValue = Resources.Load<Texture>("Materials/Suberspective/SuberspectiveShutterNoiseDefault");
            }
        }

        void ShutterInverseProperty() {
            bool inverted = shutterInverse.floatValue > 0;
            
            EditorGUI.BeginChangeCheck();
            inverted = EditorGUILayout.Toggle($"{shutterInverseText}:", inverted);

            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(shutterInverseText);
                shutterInverse.floatValue = inverted ? 1 : 0;
            }
        }
        #endregion
        
        #region Portal Copy Properties
        void PortalCopyProperties(Material material) {
            bool portalCopyPropertyIsEnabled = SuberspectiveKeywordProperties(material, portalCopyKeyword, portalCopyText);

            EditorGUI.indentLevel += 1;
            if (portalCopyPropertyIsEnabled) {
                PortalPosProperty();
                PortalNormalProperty();
                PortalFudgeDistanceProperty();
            }
            EditorGUI.indentLevel -= 1;
        }

        void PortalPosProperty() {
            editor.ShaderProperty(portalPos, portalPosText);
        }

        void PortalNormalProperty() {
            editor.ShaderProperty(portalNormal, portalNormalText);
        }

        void PortalFudgeDistanceProperty() {
            editor.ShaderProperty(fudgeDistance, fudgeDistanceText);
        }
        #endregion

        #region Render In Zone Properties

        void RenderInZoneProperties(Material material) {
            bool renderZonePropertyIsEnabled = SuberspectiveKeywordProperties(material, renderInZoneKeyword, renderInZoneText);

            EditorGUI.indentLevel += 1;
            if (renderZonePropertyIsEnabled) {
                editor.ShaderProperty(minRenderZone, minRenderZoneText);
                editor.ShaderProperty(maxRenderZone, maxRenderZoneText);
            }
            EditorGUI.indentLevel -= 1;
        }

        #endregion
        #endregion

        static void SetupMaterialWithBlendMode(Material material, ShaderUtils.SuberspectiveBlendMode blendMode) {
            void SetMaterialSettings(
                string renderType,
                string portalTag,
                BlendOp blendOp,
                BlendMode srcBlend,
                BlendMode dstBlend,
                bool zWrite,
                bool alphaPremultiplyOn,
                bool alphaTestOn,
                int renderQueue) {
                material.SetOverrideTag("RenderType", renderType);
                material.SetOverrideTag("PortalTag", portalTag);
                material.SetInt("__BlendOp", (int)blendOp);
                material.SetInt("__SrcBlend", (int)srcBlend);
                material.SetInt("__DstBlend", (int)dstBlend);
                material.SetInt("__ZWrite", zWrite ? 1 : 0);
                if (alphaPremultiplyOn) material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                else material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                if (alphaTestOn) material.EnableKeyword("_ALPHATEST_ON");
                else material.DisableKeyword("_ALPHATEST_ON");
                material.renderQueue = renderQueue;
            }

            switch (blendMode) {
                case ShaderUtils.SuberspectiveBlendMode.Opaque:
                    SetMaterialSettings("", "",
                        BlendOp.Add, BlendMode.One, BlendMode.Zero,
                        true, false, true,
                        -1);
                    break;
                case ShaderUtils.SuberspectiveBlendMode.Transparent:
                    SetMaterialSettings("Transparent", "",
                        BlendOp.Multiply, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha,
                        false, true, false,
                        (int)RenderQueue.Transparent);
                    break;
                case ShaderUtils.SuberspectiveBlendMode.CullEverything:
                    SetMaterialSettings("CullEverything", "CullEverything",
                        BlendOp.Add, BlendMode.One, BlendMode.Zero,
                        true, false, true,
                        (int)RenderQueue.Geometry + 1);
                    break;
                case ShaderUtils.SuberspectiveBlendMode.InvertColors:
                    SetMaterialSettings("Transparent", "",
                        BlendOp.Subtract, BlendMode.One, BlendMode.One,
                        false, false, false,
                        (int)RenderQueue.Transparent);
                    break;
            }
        }

        static void SetupMaterialWithCullMode(Material material, CullMode cullMode) {
            material.SetFloat("__CullMode", (float)cullMode);
        }
        
        bool SuberspectiveKeywordProperties(Material material, string keyword, string label) {
            bool keywordIsEnabled = IsSuberspectiveKeywordEnabled(material, keyword);
            
            EditorGUI.BeginChangeCheck();
            keywordIsEnabled = EditorGUILayout.Toggle($"{label}:", keywordIsEnabled);
            if (EditorGUI.EndChangeCheck()) {
                editor.RegisterPropertyChangeUndo(label);
                SetSuberspectiveKeyword(material, keyword, keywordIsEnabled);

                // If this was the last "other effects" property enabled, close the other effects menu
                if (otherEffectsKeywords.Contains(keyword) && !keywordIsEnabled) {
                    otherEffectsEnabled = otherEffectsKeywords.Any(material.IsKeywordEnabled);
                }
            }

            return keywordIsEnabled;
        }

        public virtual void SetSuberspectiveKeyword(Material m, string keyword, bool state) {
            SetKeyword(m, keyword, state);
        }

        public virtual bool IsSuberspectiveKeywordEnabled(Material material, string keyword) {
            return material.IsKeywordEnabled(keyword);
        }

        public void SetKeyword(Material m, string keyword, bool state) {
            if (state) {
                m.EnableKeyword(keyword);
                if (!keywordsEnabled.Contains(keyword)) {
                    keywordsEnabled.Add(keyword);
                }
            }
            else {
                m.DisableKeyword(keyword);
                if (keywordsEnabled.Contains(keyword)) {
                    keywordsEnabled.Remove(keyword);
                }
            }
        }

        protected void KeywordsChanged() {
            foreach (PassType passType in GetPassTypes()) {
                try {
                    ShaderVariantCollection.ShaderVariant shaderVariant =
                        new ShaderVariantCollection.ShaderVariant(GetShader(), passType, keywordsEnabled.ToArray());
                    if (!shaderVariants.Contains(shaderVariant)) {
                        shaderVariants.Add(shaderVariant);
                    }
                }
                catch (Exception _) {
                    // ignored
                }
            }
        }
        
        protected void AddSeparator() {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
    }
} // namespace UnityEditor