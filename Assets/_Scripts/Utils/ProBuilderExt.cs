using System;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProBuilder;
using EditorUtility = UnityEditor.EditorUtility;
#endif

namespace SuperspectiveUtils {
    public static class ProBuilderExt {
        public static void TransformVertices(this ProBuilderMesh pbMesh, Func<Vector3, Vector3> transformVertex) {
            // Record undo for editor
#if UNITY_EDITOR
            Undo.RecordObject(pbMesh, "GrowShrinkHallway: Shrink ProBuilder Mesh");
#endif

            // Access ProBuilder's vertex positions directly
            Vector3[] vertices = pbMesh.positions.ToArray();
            Transform meshTransform = pbMesh.transform;

            for (int i = 0; i < vertices.Length; i++) {
                vertices[i] = meshTransform.InverseTransformPoint(
                    transformVertex(meshTransform.TransformPoint(vertices[i]))
                );
            }

            // Apply transformed vertices back to ProBuilderMesh
            pbMesh.positions = vertices;

            // Refresh mesh and collider
            pbMesh.ToMesh();
            pbMesh.Refresh();

#if UNITY_EDITOR
            pbMesh.Optimize();
            EditorUtility.SetDirty(pbMesh);
            ProBuilderEditor.Refresh();
#endif
        }
    }
}