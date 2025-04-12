using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeSystem))]
public class NodeEditor : OdinEditor {
    private float timeSinceLastNodeSelected => Time.realtimeSinceStartup - timeWhenLastNodeSelected;
    private float timeWhenLastNodeSelected;
    private const float doubleClickTime = .25f;
    
    Camera sceneViewCam;
    SceneView sv;
    
    public void OnSceneGUI() {
        // get the chosen game object
        NodeSystem ns = target as NodeSystem;

        if (sv == null || sceneViewCam == null) {
            sv = EditorWindow.GetWindow<SceneView>();
            sceneViewCam = sv.camera;
        }

        if (ns == null || ns.rootNode == null) return;

        Event current = Event.current;
        bool isMultiSelect = current.shift || current.control;
        int controlID = GUIUtility.GetControlID(ns.GetHashCode(), FocusType.Passive);
        Node nodeToSelect = NodeToSelect(ns);
        if (current.type == EventType.MouseDown && current.button == 0) {
            if (nodeToSelect != null) {
                // Same Node selected, either ignore or double-click to open modal
                if (ns.selectedNodes.Contains(nodeToSelect)) {
                    if (timeSinceLastNodeSelected < doubleClickTime) {
                        // If we double-clicked a Node, deselect all other Nodes and open the NodeEditorWindow
                        ns.selectedNodes = new HashSet<Node>() {nodeToSelect};
                        NodeEditorWindow.ShowNodeEditorWindow();
                    }
                    else {
                        if (isMultiSelect) {
                            ns.selectedNodes.Remove(nodeToSelect);
                        }
                        else {
                            ns.selectedNodes = new HashSet<Node>() {nodeToSelect};
                        }
                        timeWhenLastNodeSelected = Time.realtimeSinceStartup;
                    }
                }
                // Different Node selected, select it
                else {
                    if (isMultiSelect) {
                        ns.selectedNodes.Add(nodeToSelect);
                    }
                    else {
                        ns.selectedNodes = new HashSet<Node>() {nodeToSelect};
                    }
                    timeWhenLastNodeSelected = Time.realtimeSinceStartup;
                    Event.current.Use();
                }
            }
        }
        else if (current.type == EventType.KeyDown && current.keyCode == KeyCode.F) {
            if (ns.selectedNodes != null && ns.selectedNodes.Count > 0) {
                Vector3[] worldPositions = ns.selectedNodes
                    .Select(n => ns.transform.TransformPoint(n.pos))
                    .ToArray();

                // Create bounds around all selected node positions
                Bounds bounds = new Bounds(worldPositions[0], Vector3.zero);
                foreach (var pos in worldPositions.Skip(1)) {
                    bounds.Encapsulate(pos);
                }

                // Focus the Scene view on the bounds
                SceneView.lastActiveSceneView.Frame(bounds, instant: false);
                current.Use(); // Consume the event
            }
        }

        if (current.type == EventType.Layout) {
            if (nodeToSelect != null) HandleUtility.AddDefaultControl(controlID);
        }

        DrawLinesRecursively(ns, ns.rootNode);
        
        EditorGUI.BeginChangeCheck();
        foreach (Node selectedNode in ns.selectedNodes) {
            Vector3 newPosition = ns.transform.TransformPoint(selectedNode.pos);
            if (selectedNode != null) {
                Quaternion rotation = Tools.pivotRotation == PivotRotation.Global || selectedNode.IsRootNode ?
                    ns.transform.rotation :
                    Quaternion.LookRotation(selectedNode.parent.pos-selectedNode.pos);
                newPosition = Handles.PositionHandle(ns.transform.TransformPoint(selectedNode.pos), rotation);
            }
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(ns, "Change node position");
                Vector3 offset = ns.transform.InverseTransformPoint(newPosition) - selectedNode.pos;
                foreach (Node allSelectedNodes in ns.selectedNodes) {
                    allSelectedNodes.pos += offset;
                }
            }
        }
        
        /*
        Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
            //Get a direction from the pivot to the point
            Vector3 dir = point - pivot;
            //Rotate vector around pivot
            dir = rotation * dir; 
            //Calc the rotated vector
            point = dir + pivot; 
            //Return calculated vector
            return point; 
        } */
    }

    void DrawLinesRecursively(NodeSystem system, Node curNode) {
        Color handleColor = Handles.color;
        bool curNodeSelected = system.selectedNodes.Contains(curNode);
        foreach (Node child in curNode.children) {
            if (child != null) {
                bool childSelected = system.selectedNodes.Contains(child);
                
                Handles.color = curNodeSelected || childSelected ? Color.yellow : Color.white;
                Handles.DrawLine(
                    system.transform.TransformPoint(curNode.pos),
                    system.transform.TransformPoint(child.pos)
                );
                Handles.color = handleColor;
                DrawLinesRecursively(system, child);
            }
        }
        
        Handles.color = handleColor;
    }

    Node NodeToSelect(NodeSystem t) {
        Vector2 guiPosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

        foreach (Node node in t.GetAllNodes()) {
            if (HitGizmoSphere( t.transform.TransformPoint(node.pos), ray)) return node;
        }

        return null;
    }

    bool HitGizmoSphere(Vector3 center, Ray ray) {
        Vector3 oc = ray.origin - center;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2.0f * Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - NodeSystem.gizmoSphereSize * NodeSystem.gizmoSphereSize;
        float discriminant = b * b - 4 * a * c;
        return discriminant > 0;
    }
}