using SuperspectiveAttributes;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ScenePositionAttribute))]
public class ScenePositionDrawer : PropertyDrawer {
    private static Texture2D _focusIcon;

    private static Texture2D FocusIcon {
        get {
            if (_focusIcon == null) {
                _focusIcon = Resources.Load<Texture2D>("EditorIcons/FindIcon");
            }
            return _focusIcon;
        }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        float iconSize = EditorGUIUtility.singleLineHeight;
        Rect iconRect = new Rect(position.xMax - iconSize, position.y, iconSize, iconSize);
        Rect fieldRect = new Rect(position.x, position.y, position.width - iconSize - 4f, position.height);

        EditorGUI.PropertyField(fieldRect, property, label);

        if (GUI.Button(iconRect, new GUIContent(FocusIcon, "Focus in Scene View"), GUIStyle.none)) {
            Vector3 pos = property.vector3Value;
            SceneViewExtensions.FocusPoint(pos);
            SceneViewExtensions.FlashPoint(pos, 0.25f, 5f);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

[InitializeOnLoad]
public static class SceneViewExtensions {
    private static Vector3? flashPosition = null;
    private static float flashRadius = 0.25f;
    private static double flashEndTime = 0;

    static SceneViewExtensions() {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    public static void FocusPoint(Vector3 point) {
        SceneView.lastActiveSceneView.LookAt(point, SceneView.lastActiveSceneView.rotation, 5f);
    }

    public static void FlashPoint(Vector3 point, float radius, float duration) {
        flashPosition = point;
        flashRadius = radius;
        flashEndTime = EditorApplication.timeSinceStartup + duration;
        EditorApplication.update += Update;
        SceneView.RepaintAll();
    }

    private static void Update() {
        if (EditorApplication.timeSinceStartup > flashEndTime) {
            flashPosition = null;
            EditorApplication.update -= Update;
            SceneView.RepaintAll();
        }
    }

    private static void OnSceneGUI(SceneView view) {
        if (flashPosition.HasValue) {
            Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
            Handles.SphereHandleCap(0, flashPosition.Value, Quaternion.identity, flashRadius, EventType.Repaint);
        }
    }
}
