using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.ProBuilder;
using EditorUtility = UnityEditor.ProBuilder.EditorUtility;

[InitializeOnLoad]
class UnfuckNewProbuilderObjects {
    static EditorWindow _proBuilderWindow = null;
    static EditorWindow ProBuilderWindow {
        get {
            if (_proBuilderWindow == null) {
                var windows = (EditorWindow[])Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
                foreach (var window in windows) {
                    if (window != null && window.GetType().FullName == nameof(UnityEditor.ProBuilder.PolyShapeTool)) {
                        _proBuilderWindow = window;
                        break;
                    }
                }
            }

            return _proBuilderWindow;
        }
    }

    // The last non-ProBuilder-Preview gameobject selected will be used as the parent for the new ProBuilder object
    static GameObject lastNonProBuilderPreviewObjectSelected;
    // The first ProBuilder-Preview gameobject selected is set to local position (0,0,0) of the above gameobject
    static GameObject firstProBuilderPreviewObjectSelected;
    
    
    static UnfuckNewProbuilderObjects () {
        EditorApplication.update += Update;
        EditorUtility.meshCreated += Unfuck;
    }

    static void Update() {
        GameObject selected = Selection.activeGameObject;

        if (selected != lastNonProBuilderPreviewObjectSelected) {
            if (ProBuilderWindow == null) {
                lastNonProBuilderPreviewObjectSelected = selected;
                firstProBuilderPreviewObjectSelected = null;
            }
            else {
                if (firstProBuilderPreviewObjectSelected == null) {
                    firstProBuilderPreviewObjectSelected = selected;
                    if (lastNonProBuilderPreviewObjectSelected != null) {
                        firstProBuilderPreviewObjectSelected.transform.position = lastNonProBuilderPreviewObjectSelected.transform.position;
                    }
                }
            }
        }
    }

    static void Unfuck(ProBuilderMesh mesh) {
        if (lastNonProBuilderPreviewObjectSelected != null) {
            mesh.gameObject.transform.SetParent(lastNonProBuilderPreviewObjectSelected.transform, true);
            firstProBuilderPreviewObjectSelected = null;
        }
    }
}
