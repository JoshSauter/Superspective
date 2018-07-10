using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(ColorfulFog))]
public class ColorfulFogInspector : Editor
{
    SerializedProperty heightFog;
    SerializedProperty heightDensity;
    SerializedProperty height;
    SerializedProperty startDistance;

    SerializedProperty distanceFog;
    SerializedProperty fogMode;
    SerializedProperty fogDensity;
    SerializedProperty fogStart, fogEnd;
    SerializedProperty useRadialDistance;

    SerializedProperty useCustomDepthTexture;

    SerializedProperty coloringMode;
    SerializedProperty fogCube;
    SerializedProperty solidColor;
    SerializedProperty skyColor, equatorColor, groundColor;
    SerializedProperty gradient;
    SerializedProperty gradientTexture;

    void OnEnable()
    {
        heightFog = serializedObject.FindProperty("heightFog");
        heightDensity = serializedObject.FindProperty("heightDensity");
        height = serializedObject.FindProperty("height");
        startDistance = serializedObject.FindProperty("startDistance");

        distanceFog = serializedObject.FindProperty("distanceFog");
        fogMode = serializedObject.FindProperty("fogMode");
        fogDensity = serializedObject.FindProperty("fogDensity");
        fogStart = serializedObject.FindProperty("fogStart");
        fogEnd = serializedObject.FindProperty("fogEnd");
        useRadialDistance = serializedObject.FindProperty("useRadialDistance");

        useCustomDepthTexture = serializedObject.FindProperty("useCustomDepthTexture");

        coloringMode = serializedObject.FindProperty("coloringMode");
        fogCube = serializedObject.FindProperty("fogCube");
        solidColor = serializedObject.FindProperty("solidColor");
        skyColor = serializedObject.FindProperty("skyColor");
        equatorColor = serializedObject.FindProperty("equatorColor");
        groundColor = serializedObject.FindProperty("groundColor");

        gradient = serializedObject.FindProperty("gradient");
        gradientTexture = serializedObject.FindProperty("gradientTexture");

    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        useCustomDepthTexture.boolValue = EditorGUILayout.Toggle(new GUIContent("Generate Depth Texture?",
            "Should be checked on platforms that doesn't supply a depth texture by default. For example Android & IOS"),
            useCustomDepthTexture.boolValue);
        if (!useCustomDepthTexture.boolValue)
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (buildTarget == BuildTarget.Android ||
                buildTarget == BuildTarget.iOS ||
                buildTarget == BuildTarget.WP8Player ||
                buildTarget == BuildTarget.BlackBerry)
            {
                EditorGUILayout.HelpBox("When targeting platform: " + buildTarget.ToString() +
                    " there's a high chance that Colorful Fog needs a generated depth texture",
                    MessageType.Warning);
            }
        }
        distanceFog.boolValue = EditorGUILayout.Toggle("Distance Based Fog?", distanceFog.boolValue);
        heightFog.boolValue = EditorGUILayout.Toggle("Height Based Fog?", heightFog.boolValue);
        //height fog settings.
        if (heightFog.boolValue)
        {
            EditorGUI.indentLevel++;
            //height, Y coord.
            height.floatValue = EditorGUILayout.FloatField("Height(Y coordinate)", height.floatValue);
            //density.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Height Fog Density");
            heightDensity.floatValue = EditorGUILayout.Slider(heightDensity.floatValue, 0.001f, 10f);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        //global settings & coloring.
        if (distanceFog.boolValue || heightFog.boolValue)
        {
            //EditorGUI.indentLevel++;
            //fog mode
            EditorGUILayout.PropertyField(fogMode);
            //density
            if ((UnityEngine.FogMode)Enum.GetValues(typeof(UnityEngine.FogMode)).GetValue(fogMode.enumValueIndex)
                != UnityEngine.FogMode.Linear) //exp & exp2
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Fog Density");
                fogDensity.floatValue = EditorGUILayout.Slider(fogDensity.floatValue, 0.0f, 1f);
                EditorGUILayout.EndHorizontal();
            }
            else //linear
            {
                Camera cam = GameObject.Find(serializedObject.targetObject.name).GetComponent<Camera>();
                float min, max;
                min = cam.nearClipPlane;
                max = cam.farClipPlane;
                float fs = fogStart.floatValue;
                float fe = fogEnd.floatValue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Fog Span");
                EditorGUILayout.MinMaxSlider(ref fs, ref fe, min, max);
                EditorGUILayout.EndHorizontal();
                fogStart.floatValue = fs;
                fogEnd.floatValue = fe;
                fogStart.floatValue = EditorGUILayout.FloatField("Fog Start", fogStart.floatValue);
                fogEnd.floatValue = EditorGUILayout.FloatField("Fog End", fogEnd.floatValue);
            }

            //radial distance
            useRadialDistance.boolValue = EditorGUILayout.Toggle("Radial Distance?", useRadialDistance.boolValue);
            //start distance
            startDistance.floatValue = EditorGUILayout.FloatField("Fog Start Distance", startDistance.floatValue);
            //coloring
            EditorGUILayout.PropertyField(coloringMode);
            ColorfulFog.ColoringMode cm =
                (ColorfulFog.ColoringMode)Enum.GetValues(typeof(ColorfulFog.ColoringMode)).GetValue(coloringMode.enumValueIndex);
            if (cm == ColorfulFog.ColoringMode.Cube)
            {
                EditorGUILayout.PropertyField(fogCube);
                if (fogCube.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Choose a cubemap or change coloring mode!", MessageType.Warning);
            }
            else if (cm == ColorfulFog.ColoringMode.SimpleGradient)
            {
                EditorGUILayout.PropertyField(skyColor);
                EditorGUILayout.PropertyField(equatorColor);
                EditorGUILayout.PropertyField(groundColor);
            }
            else if (cm == ColorfulFog.ColoringMode.Solid)
            {
                EditorGUILayout.PropertyField(solidColor);
            }
            else if (cm == ColorfulFog.ColoringMode.Gradient)
            {
                EditorGUILayout.PropertyField(gradient);
                var m_Object = new SerializedObject(serializedObject.targetObjects);

                System.Object[] currentSelection = (System.Object[])m_Object.targetObjects;

                foreach (System.Object obj in currentSelection)
                {
                    ((ColorfulFog)(obj)).NullTmpGradTex();//ApplyGradientChanges();
                }
            }
            else if (cm == ColorfulFog.ColoringMode.GradientTexture)
            {
                EditorGUILayout.PropertyField(gradientTexture);
            }

        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
