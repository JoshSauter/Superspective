using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewFlipTool {
    private const string FLIPPED_MARKER = "FlippedEvent"; // Unique marker to prevent event-processing infinite recursion
    private static SceneView CurrentSceneView => SceneView.lastActiveSceneView;
    
    private const string IS_FLIPPED_KEY = "SceneViewFlipTool.IsFlipped";
    private static readonly BoolEditorSetting IsFlipped = new BoolEditorSetting(IS_FLIPPED_KEY, false);

    private static Quaternion originalRotation;

    static SceneViewFlipTool() {
        SceneView.duringSceneGui += OnSceneGUI;
        IsFlipped.onValueChanged += () => {
            SceneView sceneView = CurrentSceneView;
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
        DrawFlippedLabel(sceneView);
        
        if (!IsFlipped) return;

        Event e = Event.current;
        if (e == null || e.commandName == FLIPPED_MARKER) return;

        // Invert the mouse delta to simulate a flipped view
        if (e.type == EventType.MouseDrag && e.button == 1) {
            e.delta = new Vector2(-e.delta.x, e.delta.y);
            e.commandName = FLIPPED_MARKER;
        }
    }
    
    // Ahh, ChatGPT, doing the things I have no interest in doing myself:
    private static void DrawFlippedLabel(SceneView sceneView) {
        Handles.BeginGUI(); // Start GUI rendering inside SceneView

        // Get SceneView size
        Rect sceneViewRect = sceneView.position;

        // Define label position (slightly below the compass)
        float labelWidth = IsFlipped ? 60f : 100f;
        float labelHeight = 20f;
        float xOffset = IsFlipped ? -20f : 0f;
        float yOffset = 110f;

        Vector2 labelPosition = new Vector2(sceneViewRect.width - labelWidth + xOffset, yOffset);

        string text = IsFlipped ? "Flipped" : "Not Flipped";
        Color textColor = IsFlipped ? Color.red : Color.green;
        
        // Draw semi-transparent background box
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            normal = { textColor = textColor },
            fontStyle = FontStyle.Bold
        };

        GUI.Box(new Rect(labelPosition.x, labelPosition.y, labelWidth, labelHeight), text, boxStyle);

        Handles.EndGUI(); // End GUI rendering
    }
}
