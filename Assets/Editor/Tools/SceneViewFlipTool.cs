using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewFlipTool {
    private const string FLIPPED_MARKER = "FlippedEvent"; // Unique marker to prevent event-processing infinite recursion
    private static SceneView CurrentSceneView => SceneView.lastActiveSceneView;
    
    private const string IS_FLIPPED_KEY = "SceneViewFlipTool.IsFlipped";
    private static readonly BoolEditorSetting IsFlipped = new BoolEditorSetting(IS_FLIPPED_KEY, false);
    
    private const string ORIGINAL_PIVOT_KEY = "SceneViewFlipTool.OriginalPivot";
    private static readonly Vector3EditorSetting OriginalPivot = new Vector3EditorSetting(ORIGINAL_PIVOT_KEY, Vector3.zero);

    private static Quaternion originalRotation;

    static SceneViewFlipTool() {
        SceneView.duringSceneGui += OnSceneGUI;
        IsFlipped.onValueChanged += () => {
            SceneView sceneView = CurrentSceneView;
            Debug.Log($"Scene view is flipped: {IsFlipped.Value}");
            if (IsFlipped) {
                // Flip around the forward axis to turn upside-down
                sceneView.rotation = Quaternion.AngleAxis(180f, sceneView.camera.transform.forward) * sceneView.rotation;
            }
            else {
                sceneView.rotation = Quaternion.AngleAxis(-180f, sceneView.camera.transform.forward) * sceneView.rotation;
            }
        };
    }

    [MenuItem("Tools/Flip Scene View Camera _F8")] // Shortcut: F8 (no modifiers)
    private static void ToggleSceneViewFlip() {
        SceneView sceneView = CurrentSceneView;
        if (sceneView == null) return;

        IsFlipped.Value = !IsFlipped;

        sceneView.Repaint();
    }

    private static void OnSceneGUI(SceneView sceneView) {
        if (!IsFlipped) return;

        Event e = Event.current;
        if (e == null || e.commandName == FLIPPED_MARKER) return;

        // Invert the mouse delta to simulate a flipped view
        if (e.type == EventType.MouseDrag && e.button == 1) {
            e.delta = new Vector2(-e.delta.x, e.delta.y);
            e.commandName = FLIPPED_MARKER;

            //SendFlippedEvent(e, CurrentSceneView);
        }

        DrawFlippedLabel(sceneView);
    }
    
    // Ahh, ChatGPT, doing the things I have no interest in doing myself:
    private static void DrawFlippedLabel(SceneView sceneView) {
        Handles.BeginGUI(); // Start GUI rendering inside SceneView

        // Get SceneView size
        Rect sceneViewRect = sceneView.position;

        // Define label position (slightly below the compass)
        float labelWidth = 60f;
        float labelHeight = 20f;
        float xOffset = -20f; // Adjust to align properly under compass
        float yOffset = 110f; // Distance from the compass

        Vector2 labelPosition = new Vector2(sceneViewRect.width - labelWidth + xOffset, yOffset);

        // Draw semi-transparent background box
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };

        GUI.Box(new Rect(labelPosition.x, labelPosition.y, labelWidth, labelHeight), "FLIPPED", boxStyle);

        Handles.EndGUI(); // End GUI rendering
    }
}
