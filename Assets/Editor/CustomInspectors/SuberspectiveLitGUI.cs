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
    private const string shininessText = "Shininess";
    private const string specColorText = "Specular Color";
    private const string diffuseMagnitudeText = "Diffuse";
    private const string specularMagnitudeText = "Specular";
    private const string ambientMagnitudeText = "Ambient";
    
    protected override void FindProperties(MaterialProperty[] props) {
        base.FindProperties(props);
        
        specColor = FindProperty("_SpecColor", props);
        shininess = FindProperty("_Shininess", props);
        diffuseMagnitude = FindProperty("_DiffuseMagnitude", props);
        specularMagnitude = FindProperty("_SpecularMagnitude", props);
        ambientMagnitude = FindProperty("_AmbientMagnitude", props);
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
        EditorGUI.indentLevel -= 1;
        
        AddSeparator();
        
        base.ShowGUI(material);
    }
}
