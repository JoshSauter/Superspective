using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class SuberspectiveLitGUI : SuberspectiveGUI  {
    private MaterialProperty specColor;
    private MaterialProperty shininess;
    private MaterialProperty diffuseMagnitude;
    private MaterialProperty specularMagnitude;
    private MaterialProperty ambientMagnitude;
    private MaterialProperty distanceFadeEffectMagnitude;
    private MaterialProperty distanceFadeEffectStartDistance;
    private const string shininessText = "Shininess";
    private const string specColorText = "Specular Color";
    private const string diffuseMagnitudeText = "Diffuse";
    private const string specularMagnitudeText = "Specular";
    private const string ambientMagnitudeText = "Ambient";
    private const string distanceFadeEffectMagnitudeText = "Distance Fade Effect Magnitude [0-1]";
    private const string distanceFadeEffectStartDistanceText = "Distance Fade Effect Start Distance [0-1]";
    
    protected override void FindProperties(MaterialProperty[] props) {
        base.FindProperties(props);
        
        specColor = FindProperty("_SpecColor", props);
        shininess = FindProperty("_Shininess", props);
        diffuseMagnitude = FindProperty("_DiffuseMagnitude", props);
        specularMagnitude = FindProperty("_SpecularMagnitude", props);
        ambientMagnitude = FindProperty("_AmbientMagnitude", props);
        distanceFadeEffectMagnitude = FindProperty("_DistanceFadeEffectMagnitude", props);
        distanceFadeEffectStartDistance = FindProperty("_DistanceFadeEffectStartDistance", props);
    }

    protected override void ShowGUI(Material material) {
        editor.ColorProperty(specColor, specColorText);
        editor.RangeProperty(shininess, shininessText);
        EditorGUILayout.LabelField("Light settings:");
        EditorGUI.indentLevel += 1;
        EditorGUILayout.LabelField("Lighting magnitudes");
        editor.RangeProperty(diffuseMagnitude, diffuseMagnitudeText);
        editor.RangeProperty(specularMagnitude, specularMagnitudeText);
        editor.RangeProperty(ambientMagnitude, ambientMagnitudeText);
        editor.RangeProperty(distanceFadeEffectMagnitude, distanceFadeEffectMagnitudeText);
        editor.RangeProperty(distanceFadeEffectStartDistance, distanceFadeEffectStartDistanceText);
        EditorGUI.indentLevel -= 1;
        
        AddSeparator();
        
        base.ShowGUI(material);
    }
}
