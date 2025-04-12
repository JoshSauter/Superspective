using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ProBuilder;

public class CoplanarFaceSelector : EditorWindow {
    private float angleThreshold = 1.0f; // In degrees
    private float positionThreshold = 0.01f; // In world units

    [MenuItem("Tools/ProBuilder/Select Coplanar Faces")]
    static void Init() {
        CoplanarFaceSelector window = (CoplanarFaceSelector)EditorWindow.GetWindow(typeof(CoplanarFaceSelector));
        window.titleContent = new GUIContent("Coplanar Selector");
        window.Show();
    }

    void OnGUI() {
        GUILayout.Label("Select Coplanar Faces", EditorStyles.boldLabel);
        angleThreshold = EditorGUILayout.Slider("Angle Threshold", angleThreshold, 0f, 10f);
        positionThreshold = EditorGUILayout.Slider("Position Threshold", positionThreshold, 0f, 1f);

        if (GUILayout.Button("Select Coplanar Faces")) {
            SelectCoplanarFaces(angleThreshold);
        }
    }

    void SelectCoplanarFaces(float thresholdDegrees) {
        // Get selected ProBuilder objects
        var selectedMeshes = Selection.transforms;

        foreach (var t in selectedMeshes) {
            var pb = t.GetComponent<ProBuilderMesh>();
            if (pb == null) continue;

            // Get the currently selected faces
            var selectedFaces = pb.GetSelectedFaces();
            if (selectedFaces == null || selectedFaces.Length == 0) {
                Debug.LogWarning("No faces selected in ProBuilder object: " + pb.name);
                continue;
            }

            List<Face> coplanarFaces = new List<Face>();
            Vector3[] vertexPositions = pb.positions.ToArray();

            Dictionary<Face, (Vector3, Vector3[])> faceData = new Dictionary<Face, (Vector3, Vector3[])>();
            
            // Build some data structures to make the selection checks more performant
            foreach (var face in pb.faces) {
                Vector3 faceNormal = Math.Normal(pb, face);
                Vector3[] faceVertices = face.distinctIndexes.Select(i => vertexPositions[i]).ToArray();
                
                faceData[face] = (faceNormal, faceVertices);
            }

            foreach (var face in pb.faces) {
                (Vector3 faceNormal, Vector3[] faceVertices) = faceData[face];
                
                foreach (var selFace in selectedFaces) {
                    if (face == selFace) {
                        coplanarFaces.Add(face);
                        break;
                    }
                    
                    (Vector3 selFaceNormal, Vector3[] selFaceVertices) = faceData[selFace];
                    float angle = Vector3.Angle(faceNormal, selFaceNormal);
                    if (angle > thresholdDegrees) continue;
                    
                    // Check if the vertices of the faces are coplanar
                    if (VerticesAreCoplanar(faceNormal, faceVertices, selFaceVertices)) {
                        coplanarFaces.Add(face);
                        break;
                    }
                }
            }
            
            Debug.Log($"Found {coplanarFaces.Count} faces coplanar to {selectedFaces.Length} selected face in ProBuilder object: {pb.name}");
            pb.ClearSelection();
            pb.SetSelectedFaces(coplanarFaces);
            ProBuilderEditor.Refresh();
        }
    }
        
    private bool VerticesAreCoplanar(Vector3 normal, Vector3[] a, Vector3[] b) {
        foreach (Vector3 vertexA in a) {
            if (b.Any(vertexB => Mathf.Abs(Vector3.Dot(normal, vertexB - vertexA)) > positionThreshold)) {
                return false;
            }
        }

        return true;
    }
}
