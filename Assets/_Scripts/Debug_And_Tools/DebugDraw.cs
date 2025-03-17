using System.Collections.Generic;
using Saving;
using UnityEngine;

// Hack taking advantage of Unity's Gizmos to draw debug shapes in the editor from any script without that script needing its own OnDrawGizmos method.
// That way I don't have to track state in a script just for drawing debug information.
public class DebugDraw : Singleton<DebugDraw> {
    private struct SphereDraw {
        public float duration;
        public Color color;
        public Vector3 center;
        public float radius;
        public float createdAt;
        
        public SphereDraw(Vector3 center, float radius, Color color, float duration) {
            this.center = center;
            this.radius = radius;
            this.color = color;
            this.duration = duration;
            this.createdAt = Time.time;
        }
    }
    
    private static Dictionary<string, SphereDraw> spheres = new Dictionary<string, SphereDraw>();
    
    public static void Sphere(string id, Vector3 center, float radius, Color color = default, float duration = 0) {
        color = color == default ? Color.white : color;
        spheres[id] = new SphereDraw(center, radius, color, duration);
    }
    
    void OnDrawGizmos() {
        List<string> keysToRemove = new List<string>();
        
        foreach (var sphereKv in spheres) {
            SphereDraw sphere = sphereKv.Value;
            if (Time.time - sphere.createdAt - Time.deltaTime > sphere.duration) {
                keysToRemove.Add(sphereKv.Key);
            }
            
            Color prevColor = Gizmos.color;
            Gizmos.color = sphere.color;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = prevColor;
        }
        
        // Remove expired spheres
        foreach (string key in keysToRemove) {
            spheres.Remove(key);
        }
    }
}
