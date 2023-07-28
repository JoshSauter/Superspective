using System.Collections.Generic;
using System.Linq;
using PowerTrailMechanics;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class InvisibleObjectHandle {
    private static Dictionary<InvisibleObject, Vector3> invisibleObjects = new Dictionary<InvisibleObject, Vector3>();
    
    static Camera sceneViewCam;
    static SceneView sv;
    private static float handleSize = .375f;
    
    static InvisibleObjectHandle() {
        // Disabled cuz this shit is inefficient as hell
        // SceneView.beforeSceneGui += DrawInvisibleObjectHandles;
        //EditorApplication.update += DrawInvisibleObjectHandles;
        RefreshInvisibleObjects();
    }

    static void DrawInvisibleObjectHandles(SceneView sv) {
        if (sv == null || sceneViewCam == null) {
            sv = EditorWindow.GetWindow<SceneView>();
            sceneViewCam = sv.camera;
        }

        Event current = Event.current;
        if (current != null && (current.type == EventType.MouseDown || current.type == EventType.MouseUp) && current.button == 0) {
            MonoBehaviour invisibleObjectToSelect = InvisibleObjectHandleToSelect(true);
            
            if (invisibleObjectToSelect != null) {
                Selection.objects = new[] { invisibleObjectToSelect.gameObject };
                current.Use();
            }
        }

        foreach (var invisObjPosition in invisibleObjects.Values) {
            DebugExtension.DebugWireSphere(invisObjPosition, new Color(.97f, .2f, .41f), handleSize, 0.1f);
        }
    }

    static void RefreshInvisibleObjects() {
        invisibleObjects = GameObject.FindObjectsOfType<MonoBehaviour>()
            .OfType<InvisibleObject>()
            .ToDictionary(i => i, i => (i as MonoBehaviour).transform.position);
    }

    static MonoBehaviour InvisibleObjectHandleToSelect(bool forceRefresh = false) {
        if (forceRefresh || invisibleObjects.Count == 0) {
            RefreshInvisibleObjects();
        }
        
        Vector2 guiPosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

        foreach (InvisibleObject i in invisibleObjects.Keys) {
            if (HitGizmoSphere(invisibleObjects[i], ray)) return (i as MonoBehaviour);
        }

        return null;
    }

    static bool HitGizmoSphere(Vector3 center, Ray ray) {
        Vector3 oc = ray.origin - center;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2.0f * Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - handleSize * handleSize;
        float discriminant = b * b - 4 * a * c;
        return discriminant > 0;
    }
}