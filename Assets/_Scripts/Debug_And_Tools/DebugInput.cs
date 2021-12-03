using UnityEngine;

public static class DebugInput {
    public static bool GetKeyDown(KeyCode keyCode) => Debug.isDebugBuild && Input.GetKeyDown(keyCode);
    public static bool GetKeyDown(string key) => Debug.isDebugBuild && Input.GetKeyDown(key);
    public static bool GetKeyUp(KeyCode keyCode) => Debug.isDebugBuild && Input.GetKeyUp(keyCode);
    public static bool GetKeyUp(string key) => Debug.isDebugBuild && Input.GetKeyUp(key);
    public static bool GetKey(KeyCode keyCode) => Debug.isDebugBuild && Input.GetKey(keyCode);
    public static bool GetKey(string key) => Debug.isDebugBuild && Input.GetKey(key);
}
