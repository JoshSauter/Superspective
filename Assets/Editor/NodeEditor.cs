using PowerTrailMechanics;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeSystem))]
public class NodeEditor : UnityEditor.Editor {
    private float timeSinceLastNodeSelected => Time.realtimeSinceStartup - timeWhenLastNodeSelected;
    private float timeWhenLastNodeSelected;
    private const float doubleClickTime = .25f;
    
    Camera sceneViewCam;
    SceneView sv;
    
    void OnSceneGUI() {
        // get the chosen game object
        NodeSystem t = target as NodeSystem;

        if (sv == null || sceneViewCam == null) {
            sv = EditorWindow.GetWindow<SceneView>();
            sceneViewCam = sv.camera;
        }

        if (t == null || t.rootNode == null) return;

        Event current = Event.current;
        int controlID = GUIUtility.GetControlID(t.GetHashCode(), FocusType.Passive);
        Node nodeToSelect = NodeToSelect(t);
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
            if (nodeToSelect != null) {
                // Same Node selected, either ignore or double-click to open modal
                if (t.selectedNode == nodeToSelect) {
                    if (timeSinceLastNodeSelected < doubleClickTime) {
                        NodeEditorWindow.ShowNodeEditorWindow();
                    }
                    else {
                        timeWhenLastNodeSelected = Time.realtimeSinceStartup;
                    }
                }
                // Different Node selected, select it
                else {
                    t.selectedNode = nodeToSelect;
                    timeWhenLastNodeSelected = Time.realtimeSinceStartup;
                    Event.current.Use();
                }
            }
        }

        if (current.type == EventType.Layout) {
            if (nodeToSelect != null) HandleUtility.AddDefaultControl(controlID);
        }

        DrawLinesRecursively(t, t.rootNode);
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = t.transform.TransformPoint(t.selectedNode.pos);
        if (t.selectedNode != null) {
            Quaternion rotation = Tools.pivotRotation == PivotRotation.Global || t.selectedNode.isRootNode ?
                t.transform.rotation :
                Quaternion.LookRotation(t.selectedNode.parent.pos-t.selectedNode.pos);
            newPosition = Handles.PositionHandle(t.transform.TransformPoint(t.selectedNode.pos), rotation);
        }
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(t, "Change node position");
            t.selectedNode.pos = t.transform.InverseTransformPoint(newPosition);
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
        foreach (Node child in curNode.children) {
            if (child != null) {
                Handles.DrawLine(
                    system.transform.TransformPoint(curNode.pos),
                    system.transform.TransformPoint(child.pos)
                );
                DrawLinesRecursively(system, child);
            }
        }
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