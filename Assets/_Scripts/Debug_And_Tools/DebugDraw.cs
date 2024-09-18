
using System.Collections.Generic;
using Saving;
using UnityEngine;

public class DebugDraw : Singleton<DebugDraw> {
    private static Dictionary<string, (Vector3, float, Color)> spheres = new Dictionary<string, (Vector3, float, Color)>();
    
    public static void Sphere(string id, Vector3 center, float radius, Color color = default) {
        color = color == default ? Color.white : color;
        spheres[id] = (center, radius, color);
    }
    
    void OnDrawGizmos() {
        foreach (var sphereKv in spheres) {
            (Vector3 center, float radius, Color color) = sphereKv.Value;
            Color prevColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawSphere(center, radius);
            Gizmos.color = prevColor;
        }
    }
}

public static class DebugDrawExt {
    public static void Sphere(this SaveableObject saveObj, Vector3 center, float radius, Color color = default) {
        DebugDraw.Sphere(saveObj.ID, center, radius, color);
    }
}