using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewFlipTool {
    private static bool isFlipped = false;
    private static Quaternion originalRotation;

    static SceneViewFlipTool() {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/Flip Scene View Camera _F8")] // Shortcut: F8 (no modifiers)
    private static void ToggleSceneViewFlip() {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        bool wasFlipped = isFlipped;
        isFlipped = !isFlipped;

        if (isFlipped && !wasFlipped) {
            // Flip around the forward axis to turn upside-down
            sceneView.rotation = Quaternion.AngleAxis(180f, sceneView.camera.transform.forward) * sceneView.rotation;
        } else if (wasFlipped && !isFlipped) {
            sceneView.rotation = Quaternion.AngleAxis(-180f, sceneView.camera.transform.forward) * sceneView.rotation;
        }

        sceneView.Repaint();
    }

    private static void OnSceneGUI(SceneView sceneView) {
        if (!isFlipped) return;

        Event e = Event.current;
        if (e == null) return;

        if (e.type == EventType.MouseDrag) {
            if (e.button == 1) { // Right Mouse Button (Orbit)
                float rotationSpeed = GetRotationSpeed(sceneView);
                sceneView.rotation = Quaternion.AngleAxis(-e.delta.x * rotationSpeed, Vector3.up) * sceneView.rotation;
                sceneView.rotation = Quaternion.AngleAxis(e.delta.y * rotationSpeed, sceneView.camera.transform.right) * sceneView.rotation;
                e.Use();
            } 
            else if (e.button == 2) { // Middle Mouse Button (Panning)
                float panSpeed = GetPanSpeed(sceneView);
                Vector3 right = sceneView.camera.transform.right;
                Vector3 up = -sceneView.camera.transform.up; // Invert vertical panning

                sceneView.pivot -= (right * e.delta.x + up * e.delta.y) * panSpeed;
                e.Use();
            }
        }

        // Prevent Unity from applying its default panning behavior after release
        if (e.type == EventType.MouseUp && e.button == 2) {
            GUIUtility.hotControl = 0;
            e.Use();
            SimulateRightClick();
        }
    }

    private const float ROTATION_SPEED_MULTIPLIER = 0.006f;
    private static float GetRotationSpeed(SceneView sceneView) {
        return ROTATION_SPEED_MULTIPLIER * sceneView.size; // Scales rotation with zoom distance, similar to Unity's default behavior
    }

    private const float PAN_SPEED_MULTIPLIER = 0.0025f;
    private static float GetPanSpeed(SceneView sceneView) {
        return PAN_SPEED_MULTIPLIER * sceneView.size; // Matches Unity's default panning speed
    }
    
    private static void SimulateRightClick() {
        Event mouseDown = new Event {
            type = EventType.MouseDown,
            button = 1, // Right-click
            mousePosition = Vector2.zero
        };
        Event mouseUp = new Event {
            type = EventType.MouseUp,
            button = 1, // Right-click
            mousePosition = Vector2.zero
        };

        SceneView.lastActiveSceneView.SendEvent(mouseDown);
        SceneView.lastActiveSceneView.SendEvent(mouseUp);
    }
}
