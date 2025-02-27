using UnityEngine;

namespace SuperspectiveUtils {
    public static class RendererExt {
        public static Mesh GetMesh(this Renderer r) {
            if (r is MeshRenderer) {
                MeshFilter meshFilter = r.GetComponent<MeshFilter>();
                return meshFilter ? meshFilter.sharedMesh : null;
            }
            if (r is SkinnedMeshRenderer skinnedMeshRenderer) {
                return skinnedMeshRenderer.sharedMesh;
            }
            return null;
        }
    }
}