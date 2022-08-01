/*
 
 Name:  Cut and paste without changing shit
 Release Date:  2/25/2020
 Version: 1.0
 Credits: Written by Seth A. Robinson except where otherwise noted
 License: No rights reserved, do whatever with it

 Description:

 In the Unity editor, if you drag gameobjects around in the hierarchy, their local position and rotation will be modified so they end up in
 the same final rotation/world position as they had before.  This adds an option so you can do a "pure" cut and paste without that silliness.

 To use:

 Make a folder called "Editor" somewhere in your assets folder (or a subfolder of it) and put this file in it.

 If you right click a gameobject in the editor hierarchy, you should now see two new options "Cut without changing shit" and
 "Paste without changing shit".  Using those you can move an object without Unity modifying its local transform like it normally does.

 Notes:

- No multi-select, only works on a single object (which can contain sub-objects)
- Nothing actually happens until you paste a gameobject (it isn't actually moved until then)
- If you paste without an object selected, it will be moved to the hierarchy root
- Undo doesn't work for this cut and paste
- (barely) Tested with Unity 2019.3.2f1

 www.rtsoft.com
 www.codedojo.com

*/

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class NoBSCutAndPaste {
    static bool _cut;
    static GameObject[] objsToBePasted;

    [MenuItem("GameObject/Cut without changing shit (Shift-Ctrl-X) %#x", false, 0)]
    static void CutWithoutChangingShit() {
        GameObject[] go = Selection.gameObjects;

        if (go == null) {
            EditorUtility.DisplayDialog("Woah!", "First click on a gameobject in the hierarchy!", "Ok");
            return;
        }

        string s = EditorWindow.focusedWindow.ToString();

        if (EditorWindow.focusedWindow.ToString() != " (UnityEditor.SceneHierarchyWindow)") {
            EditorUtility.DisplayDialog(
                "Woah!",
                "Don't use the 3D window, click on the gameobject in the hierarchy tree instead before doing cut/paste.",
                "Ok"
            );
            objsToBePasted = null;
            return;
        }

        objsToBePasted = go;
        _cut = true;

        //Debug.Log("Cutting" + string.Join<GameObject>(", ", _tempObjs) + ", now choose Paste without changing shit");
    }

    [MenuItem("GameObject/Copy without changing shit (Shift-Ctrl-C) %#x", false, 0)]
    static void CopyWithoutChangingShit() {
        GameObject[] gameObjectsSelected = Selection.gameObjects;

        if (gameObjectsSelected == null) {
            EditorUtility.DisplayDialog("Woah!", "First click on a gameobject in the hierarchy!", "Ok");
            return;
        }

        string s = EditorWindow.focusedWindow.ToString();

        if (EditorWindow.focusedWindow.ToString() != " (UnityEditor.SceneHierarchyWindow)") {
            EditorUtility.DisplayDialog(
                "Woah!",
                "Don't use the 3D window, click on the gameobject in the hierarchy tree instead before doing cut/paste.",
                "Ok"
            );
            objsToBePasted = null;
            return;
        }

        objsToBePasted = gameObjectsSelected;
        _cut = false;

        //Debug.Log("Copying" + string.Join<GameObject>(", ", _tempObjs) + ", now choose Paste without changing shit");
    }

    //This part by Jlpeebles taken from https://answers.unity.com/questions/656869/foldunfold-gameobject-from-code.html
    public static void SetExpandedRecursive(GameObject go, bool expand) {
        Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        MethodInfo methodInfo = type.GetMethod("SetExpandedRecursive");

        EditorWindow window = EditorWindow.focusedWindow;

        methodInfo.Invoke(window, new object[] {go.GetInstanceID(), expand});
    }

    [MenuItem("GameObject/Paste without changing shit (Shift-Ctrl-V) %#v", false, 0)]
    static void PasteWithoutChangingShit() {
        if (Selection.objects.Length <= 0) return;
        
        if (objsToBePasted == null) {
            EditorUtility.DisplayDialog(
                "Woah!",
                "Nothing to paste.  Highlight an object, right click, and choose 'Paste without changing shit' first.",
                "Ok"
            );
            return;
        }

        if (EditorWindow.focusedWindow.ToString() != " (UnityEditor.SceneHierarchyWindow)") {
            EditorUtility.DisplayDialog(
                "Woah!",
                "Don't use the 3D window, click on objects in the hierarchy tree instead before doing cut/paste.",
                "Ok"
            );
            objsToBePasted = null;
            return;
        }

        foreach (Transform go in Selection.transforms) {
            if (go == null || go.gameObject == null) {
                Debug.Log(
                    "Pasting " + string.Join<GameObject>(", ", objsToBePasted) +
                    " without changing its local transform stuff.  (Pasted to root as a gameobject wasn't highlighted to parent it to)"
                );

                foreach (GameObject objToBePasted in objsToBePasted) {
                    Transform newObj = DuplicateObject(objToBePasted);
                    newObj.transform.SetParent(null, false);
                }
            }
            else {
                Debug.Log(
                    "Pasting " + string.Join<GameObject>(", ", objsToBePasted) + " under " + go.gameObject.name +
                    " without changing its local transform stuff."
                );
                foreach (GameObject _tempObj in objsToBePasted) {
                    Transform newObj = DuplicateObject(_tempObj);
                    newObj.transform.SetParent(go.transform, false);
                    newObj.transform.SetAsFirstSibling();
                    newObj.name = _tempObj.name;
                }

                SetExpandedRecursive(go.gameObject, true);
            }
        }

        if (_cut) {
            foreach (var objPasted in objsToBePasted) {
                Object.DestroyImmediate(objPasted);
            }

            objsToBePasted = null;
        }

        // Only execute once per selection of gameobjects
        Selection.objects = null;
    }

    public static Transform DuplicateObject(GameObject obj) {
        Object prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(obj);

        if (prefabRoot != null)
            return (PrefabUtility.InstantiatePrefab(prefabRoot) as GameObject).transform;
        return Object.Instantiate(obj).transform;
    }


    /*
     //In theory this would grey out the paste option when it wasn't valid, but due to Unity weirdness it only works in the "GameObject" drop down, not the right
     //click context menu on the hierarchy.  Better to not have it on as it just looks like it doesn't work when using from there.

    // Note that we pass the same path, and also pass "true" to the second argument.
    [MenuItem("GameObject/Paste without changing shit (Shift-Ctrl-V) %#v", true)]
    static bool PasteWithoutChangingShitValidation()
    {
        // This returns true when the selected object is a Texture2D (the menu item will be disabled otherwise).
        return _tempObj != null;
    }

    */
}