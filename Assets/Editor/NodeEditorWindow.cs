using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

public class NodeEditorWindow : EditorWindow {

    public static void ShowNodeEditorWindow() {
        NodeEditorWindow window = ScriptableObject.CreateInstance(typeof(NodeEditorWindow)) as NodeEditorWindow;
        CenterOnMainWin(window);
        window.title = "Node Properties";
        window.ShowModalUtility();
    }

    void MoveToParentsPosition(Node node) {
        node.pos = node.parent.pos;
    }

    void MarkStaircase() {
        NodeSystem.MarkStaircase();
    }

    private void OnGUI() {
        foreach (var selected in Selection.transforms) {
            if (selected.TryGetComponent<NodeSystem>(out NodeSystem ns)) {
                Node node = ns.selectedNode;

                float GetDistanceToRoot(Node from, float acc) {
                    if (from.isRootNode) return acc;

                    return GetDistanceToRoot(from.parent, acc + Vector3.Distance(from.pos, from.parent.pos));
                }

                float distance = GetDistanceToRoot(node, 0);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.FloatField("Distance", distance);
                EditorGUI.EndDisabledGroup();
                node.pos = EditorGUILayout.Vector3Field("Position", node.pos);
                node.zeroDistanceToChildren = EditorGUILayout.Toggle("Zero Distance To Children", node.zeroDistanceToChildren);
                node.staircaseSegment = EditorGUILayout.Toggle("Staircase Segment", node.staircaseSegment);

                string staircaseLabel = ns.startOfStaircaseIndex < 0 ? "Mark Start Of Staircase" : "Mark End Of Staircase";
                if (GUILayout.Button(staircaseLabel)) {
                    MarkStaircase();
                }

                if (node.parent != null) {
                    if (GUILayout.Button("Move To Parent's Position")) {
                        MoveToParentsPosition(node);
                    }

                    AddSeparator();

                    if (GUILayout.Button("Select Parent")) {
                        ns.selectedNode = node.parent;
                        return;
                    }
                }
                else {
                    AddSeparator();
                }

                if (node.children != null && node.children.Count > 0) {
                    foreach (var child in node.children) {
                        if (GUILayout.Button($"Select Child: {child.pos - node.pos:F3}")) {
                            ns.selectedNode = child;
                            return;
                        }
                    }
                }
            }
        }
    }
    
    public static void CenterOnMainWin(EditorWindow window) {
        Rect main = EditorGUIUtility.GetMainWindowPosition();
        Rect pos = window.position;
        float centerWidth = (main.width - pos.width) * 0.5f;
        float centerHeight = (main.height - pos.height) * 0.5f;
        pos.x = main.x + centerWidth;
        pos.y = main.y + centerHeight;
        window.position = pos;
    }
    
    private void AddSeparator() {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
}
