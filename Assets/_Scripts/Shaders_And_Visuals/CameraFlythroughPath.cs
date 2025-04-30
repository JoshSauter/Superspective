using System.Collections.Generic;
using System.Linq;
using LevelManagement;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This shit is unfinished and I'm not sure I even want to continue with using Unity's animation system for it
// Problems: Can't figure out how to smoothly enter the animation starting from the current position, instead it
// just instantly sets the transform to the first animation node. Also there's no way to edit this outside of the visual
// editor, so you can't fix anything in code. I made a mistake and had all the positions as global instead of local and
// had to go through every single node to update them to fix when I wish I could have just written a script...
public class CameraFlythroughPath : MonoBehaviour {
    public Levels level;

    public bool isPlaying {
        get;
        private set;
    }
    
    float timeElapsed = 0f;
    public bool startOnPlayerCam = true;
    bool hasInsertedPlayerCamTransformAtStart = false;
    
    public SuperspectiveReference levelRootReference;
    Transform levelRoot => levelRootReference.Reference.LeftOrDefault.transform;
    Transform originalCameraParent;
    int originalCameraSiblingIndex;
    
    public List<TransformInfo> nodes;

#if UNITY_EDITOR
    [MenuItem("My Tools/CameraFlythroughPath/Add node at player cam position _#%T")]
    public static void AddNodeAtPlayerCamPosition() {
        if (!Application.isPlaying) return;
        
        // Debug.LogError("Selected gameObjects:\n" + string.Join("\n", Selection.gameObjects.Select(x => x.name)));
        foreach (GameObject selected in Selection.gameObjects) {
            if (selected.TryGetComponent(out CameraFlythroughPath path)) {
                GameObject newNode = new GameObject("Node");
                newNode.transform.SetParent(Player.instance.PlayerCam.transform, false);
                newNode.transform.SetParent(path.transform, true);
                
                path.nodes.Add(new TransformInfo(newNode.transform, false));
                
                Debug.Log($"Added new CameraFlythroughPath node to {path.name}");
            }
        }
    }
#endif
    
    void Init() {
        nodes = new List<TransformInfo>();
        foreach (Transform child in transform) {
            TransformInfo nodeTransform = new TransformInfo(child, levelRoot);
            nodes.Add(nodeTransform);
        }
    }
    
    public void Play() {
        if (levelRoot == null) {
            Debug.LogError($"Can't play CameraFlythroughPath.Play() for {level}, no level root found");
            return;
        }
        
        if (nodes == null || nodes.Count == 0) {
            Init();
        }
        
        // If the camera should start on the player, insert (exactly once) or update the first node to match Player transform
        if (startOnPlayerCam) {
            Transform playerCam = Player.instance.PlayerCam.transform;
            if (!hasInsertedPlayerCamTransformAtStart) {
                nodes.Insert(0, new TransformInfo(playerCam, levelRoot));
                hasInsertedPlayerCamTransformAtStart = true;
            }
            else {
                nodes[0] = new TransformInfo(playerCam);
            }
        }

        Transform cam = CameraFlythrough.instance.flythroughCamera.transform;
        originalCameraParent = cam.parent;
        originalCameraSiblingIndex = cam.GetSiblingIndex();
        cam.SetParent(levelRoot, true);
        isPlaying = true;
        timeElapsed = 0f;
    }

    public void Stop() {
        isPlaying = false;
        timeElapsed = 0f;
        Transform cam = CameraFlythrough.instance.flythroughCamera.transform;
        cam.SetParent(originalCameraParent);
        cam.SetSiblingIndex(originalCameraSiblingIndex);
    }

    void Update() {
        if (!isPlaying) return;
        timeElapsed += Time.deltaTime;
        
        Camera cam = CameraFlythrough.instance.flythroughCamera;
        int floor = Mathf.Clamp(Mathf.FloorToInt(timeElapsed), 0, nodes.Count-1);
        int ceil = Mathf.Clamp(Mathf.CeilToInt(timeElapsed), 0, nodes.Count-1);
        
        float lerpValue = timeElapsed - floor;
        TransformInfo transform1 = nodes[floor];
        TransformInfo transform2 = nodes[ceil];
        
        //TransformInfo.Lerp(transform1, transform2, lerpValue).ApplyToTransform(cam.transform, false);

        // Through iterating all through all the nodes, time to stop
        if (floor == ceil) {
            CameraFlythrough.instance.Stop();
        }
    }
}
