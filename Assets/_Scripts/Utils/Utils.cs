﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using LevelManagement;
using PortalMechanics;
using Saving;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace SuperspectiveUtils {
    [Serializable]
    public struct IntVector2 {
        public int x, y;

        public IntVector2(int _x, int _y) {
            x = _x;
            y = _y;
        }
    }

    [Serializable]
    public struct TransformInfo {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TransformInfo(Transform t, bool asWorldTransform = true) {
            position = asWorldTransform ? t.position : t.localPosition;
            rotation = asWorldTransform ? t.rotation : t.localRotation;
            scale = asWorldTransform ? t.lossyScale : t.localScale;
        }

        public TransformInfo(Transform t, Transform localToParent) {
            Transform originalParent = t.parent;
            int originalSiblingIndex = t.GetSiblingIndex();
            t.SetParent(localToParent, true);
            position = t.localPosition;
            rotation = t.rotation;
            scale = t.localScale;
            t.SetParent(originalParent);
            t.SetSiblingIndex(originalSiblingIndex);
        }

        public void ApplyToTransform(Transform t, bool asWorldTransform = true) {
            if (asWorldTransform) {
                t.position = position;
                t.rotation = rotation;
                t.localScale = Vector3.one;
                // this simulates what it would be like to set lossyScale considering the way unity treats it
                // From: https://forum.unity.com/threads/solved-why-is-transform-lossyscale-readonly.363594/
                var m = t.worldToLocalMatrix;
                m.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
                t.localScale = m.MultiplyPoint(scale);
                t.localScale = scale;
            }
            else {
                t.localPosition = position;
                t.localRotation = rotation;
                t.localScale = Vector3.one;
            }
        }

        public static TransformInfo Lerp(TransformInfo a, TransformInfo b, float t) {
            return new TransformInfo {
                position = Vector3.Lerp(a.position, b.position, t),
                rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
                scale = Vector3.Lerp(a.scale, b.scale, t)
            };
        }
    }

    public static class ICollectionExt {
        private static readonly Dictionary<int, int> _lastSelectedIndices = new Dictionary<int, int>();
        
        /// <summary>
        /// Remembers the last randomly selected element from a collection and generates a different element
        /// </summary>
        /// <param name="collection">Collection to select random element from</param>
        /// <typeparam name="T">Type of the element of the collection</typeparam>
        /// <returns>A different random value from a collection than what was last randomly generated</returns>
        public static T DifferentRandomElementFrom<T>(this ICollection<T> collection) {
            return collection.ElementAt(DifferentRandomIndexFrom(collection));
        }
        
        public static int DifferentRandomIndexFrom<T>(this ICollection<T> collection) {
            if (collection == null || collection.Count == 0) {
                throw new ArgumentException("Collection is null or empty");
            }

            int collectionHashCode = collection.GetHashCode();
            int count = collection.Count;

            // Get the last selected index for this collection
            int newIndex;
            if (_lastSelectedIndices.TryGetValue(collectionHashCode, out int lastIndex)) {
                // Generate a random index different from the last one
                newIndex = UnityEngine.Random.Range(0, count - 1);
                // We adjusted the range to be one less than the count, so we need to increment the index if it is greater than or equal to the last selected index
                if (newIndex >= lastIndex) {
                    newIndex++;
                }
            } else {
                // If no last index is stored for this collection, generate a random one
                newIndex = UnityEngine.Random.Range(0, count);
            }

            // Update the last selected index for this collection
            _lastSelectedIndices[collectionHashCode] = newIndex;
            return newIndex;
        }
        
        public static int RandomIndexFrom<T>(this ICollection<T> collection) {
            return UnityEngine.Random.Range(0, collection.Count);
        }
        
        public static T RandomElementFrom<T>(this ICollection<T> collection) {
            if (collection == null || collection.Count == 0) {
                throw new ArgumentException("Collection is null or empty");
            }

            return collection.ElementAt(RandomIndexFrom(collection));
        }
    }

    public static class BladeEdgeDetectionExt {
        private static bool EdgesSatisfyPredicate(this BladeEdgeDetection edgeDetection, Func<Color, bool> predicate) {
            switch (edgeDetection.edgeColorMode) {
                case BladeEdgeDetection.EdgeColorMode.SimpleColor:
                    return predicate.Invoke(edgeDetection.edgeColor);
                case BladeEdgeDetection.EdgeColorMode.Gradient:
                    return predicate.Invoke(edgeDetection.edgeColorGradient.Evaluate(0));
                case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
                    return false;
                default:
                    return false;
            }
        }
        
        public static bool EdgesAreWhite(this BladeEdgeDetection edgeDetection) {
            return edgeDetection.EdgesSatisfyPredicate((c) => c.grayscale > .5f);
        }
        
        public static bool EdgesAreBlack(this BladeEdgeDetection edgeDetection) {
            return edgeDetection.EdgesSatisfyPredicate((c) => c.grayscale < .5f);
        }
    }

    public static class DictionaryExt {
        public static Dictionary<K, V2> MapValues<K, V1, V2>(this Dictionary<K, V1> source, Func<V1, V2> transform) {
            Dictionary<K, V2> target = new Dictionary<K, V2>();
            foreach (var kv in source) {
                target[kv.Key] = transform(kv.Value);
            }

            return target;
        }
        
        public static V GetOrNull<K, V>(this Dictionary<K, V> source, K key) {
            return source.TryGetValue(key, out V value) ? value : default(V);
        }
    }

    public static class FloatExt {
        public static bool IsApproximately(this float f, float other) {
            return Math.Abs(f - other) < float.Epsilon;
        }
        
        public static float CloserOfTwo(this float f, float a, float b) {
            return Math.Abs(f - a) < Math.Abs(f - b) ? a : b;
        }
    }
    
    public static class AnimationCurveExtensions {
        public static AnimationCurve Reverse(this AnimationCurve originalCurve) {
            Keyframe[] originalKeyframes = originalCurve.keys;
            int keyframeCount = originalKeyframes.Length;
            Keyframe[] reversedKeyframes = new Keyframe[keyframeCount];
    
            float firstTime = originalKeyframes[0].time;
            float lastTime = originalKeyframes[keyframeCount - 1].time;
    
            for (int i = 0; i < keyframeCount; i++) {
                Keyframe originalKeyframe = originalKeyframes[i];
                reversedKeyframes[keyframeCount - 1 - i] = new Keyframe(
                    lastTime - (originalKeyframe.time - firstTime),
                    originalKeyframe.value,
                    -originalKeyframe.outTangent,
                    -originalKeyframe.inTangent
                );
            }
    
            return new AnimationCurve(reversedKeyframes);
        }
        
        /// <summary>
        /// Inverse evaluate the curve to find the x value that corresponds to the target y value. Up to the user to supply working values.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="targetY"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="tolerance"></param>
        /// <param name="maxIterations"></param>
        /// <returns></returns>
        public static float InverseEvaluate(this AnimationCurve curve, float targetY, float xMin, float xMax, float tolerance = 0.001f, int maxIterations = 10) {
            float low = xMin;
            float high = xMax;
            int iterations = 0;

            while (iterations < maxIterations) {
                float mid = (low + high) / 2f;
                float y = curve.Evaluate(mid);

                if (Mathf.Abs(y - targetY) < tolerance)
                    return mid;

                if (y < targetY)
                    low = mid;
                else
                    high = mid;

                iterations++;
            }

            return (low + high) / 2f; // Best estimate
        }

    }


    public static class Vector3Ext {
        

        // For finite lines:
        public static Vector3 GetClosestPointOnFiniteLine(this Vector3 point, Vector3 start, Vector3 end) {
            Vector3 lineDirection = end - start;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();
            float projectedLength = Mathf.Clamp(Vector3.Dot(point - start, lineDirection), 0f, lineLength);
            return start + lineDirection * projectedLength;
        }
        
        // Scale method that returns itself so that it is composable
        public static Vector3 ScaledWith(this Vector3 v, Vector3 scale) {
            v.Scale(scale);
            return v;
        }
        
        public static Vector3 ScaledBy(this Vector3 v, float x, float y, float z) {
            return v.ScaledWith(new Vector3(x, y, z));
        }
        
        public static Vector3 WithX(this Vector3 v, float x) {
            return new Vector3(x, v.y, v.z);
        }
        public static Vector3 WithY(this Vector3 v, float y) {
            return new Vector3(v.x, y, v.z);
        }
        public static Vector3 WithZ(this Vector3 v, float z) {
            return new Vector3(v.x, v.y, z);
        }

        public static float MaxComponent(this Vector3 v, bool byAbsValue = true) {
            Vector3 test = byAbsValue ? new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)) : v;

            // xyz, xzy, yxz, yzx, zxy, zyx
            // X max
            if (test.x > test.y && test.x > test.z) {
                return v.x;
            }
            // Y max
            else if (test.y > test.x && test.y > test.z) {
                return v.y;
            }
            // Z max
            else if (test.z > test.x && test.z > test.y) {
                return v.z;
            }
            else {
                throw new Exception($"No max value among {test:F2}, original vector {v:F2}");
            }
        }
        // 0 = x, 1 = y, 2 = z
        public static int MaxComponentDirection(this Vector3 v, bool byAbsValue = true) {
            float max = v.MaxComponent(byAbsValue);
            if (Math.Abs(v.x - max) < float.Epsilon) {
                return 0;
            }
            else if (Math.Abs(v.y - max) < float.Epsilon) {
                return 1;
            }
            else return 2;
        }
        
        public static Vector3 ComponentDivide(this Vector3 a, Vector3 b) {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
        
        public static Vector3 ComponentMultiply(this Vector3 a, Vector3 b) {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }
    }

    public static class ColorExt {
        public static Color purple = new Color(0.45f, 0f, 1f);
        
        public static Color WithAlphaFrom(this Color self, Color from) {
            return new Color(self.r, self.g, self.b, from.a);
        }

        public static Color WithAlpha(this Color self, float alpha) {
            return new Color(self.r, self.g, self.b, alpha);
        }

        public static float Distance(this Color a, Color b) {
            return new Vector3(a.r - b.r, a.g - b.g, a.b - b.b).magnitude;
        }
    }
    
    public static class StringExt {
        // Shorthand for converting ID strings to association IDs (the unique part of the ID)
        public static string GetAssociationId(this string id) {
            string lastPart = id.Split('_').Last();
            return lastPart.IsGuid() ? lastPart : id;
        }
        
        public static string ReplaceAt(this string str, int index, char newChar) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }

            if (index < 0 || index >= str.Length) {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the string.");
            }

            char[] chars = str.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }
            
        public static string StripWhitespace(this string s) {
            return new string(s.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }
        
        public static string StripSuffix(this string s, string suffix) {
            if (s.EndsWith(suffix)) {
                return s.Substring(0, s.Length - suffix.Length);
            }
            else {
                return s;
            }
        }
        
        public static string StripPrefix(this string s, string prefix) {
            if (s.StartsWith(prefix)) {
                return s.Substring(prefix.Length, s.Length - prefix.Length);
            }
            else {
                return s;
            }
        }
        
        // Shamelessly stolen from: https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
        public static string SplitCamelCase(this string str) {
            return Regex.Replace( 
                Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), 
                @"(\p{Ll})(\P{Ll})", 
                "$1 $2" 
            );
        }
    }

    public class TrieNode {
        public char Value { get; set; }
        public bool IsEndOfWord { get; set; }
        public Dictionary<char, TrieNode> Children { get; } = new Dictionary<char, TrieNode>();
    }

    public class Trie {
        private readonly TrieNode root = new TrieNode();
        private readonly List<string> words = new List<string>();
        private readonly Dictionary<string, string> lowerToCamelCase = new Dictionary<string, string>();

        public Trie(IEnumerable<string> words) {
            foreach (string word in words) {
                Insert(word);
            }
        }

        public void Insert(string word) {
            TrieNode node = root;
            string lowerInvariantWord = word.ToLowerInvariant();
            foreach (char c in lowerInvariantWord) {
                if (!node.Children.ContainsKey(c)) {
                    node.Children[c] = new TrieNode { Value = c };
                }
                node = node.Children[c];
                
            }
            node.IsEndOfWord = true;
            
            words.Add(lowerInvariantWord);
            words.Sort();

            lowerToCamelCase[word.ToLowerInvariant()] = word;
        }

        public string AutoComplete(string inputText, int matchIndex) {
            if (string.IsNullOrWhiteSpace(inputText)) {
                return lowerToCamelCase[words[matchIndex % words.Count]];
            }
            
            TrieNode node = root;
            string lowercaseInput = inputText.ToLowerInvariant();

            List<string> matches = new List<string>();
            foreach (char c in lowercaseInput) {
                if (node.Children.TryGetValue(c, out TrieNode childNode)) {
                    node = childNode;
                }
                else {
                    node = null;
                    // No prefix-match, look for string.Contains match instead
                    matches = FallbackSearch(lowercaseInput);
                    break;
                }
            }

            if (node != root && node != null) {
                matches = FindAllMatches(node, lowercaseInput);
            }
            return matches.Count > 0 ? lowerToCamelCase[matches[matchIndex % matches.Count]] : inputText;
        }

        private List<string> FallbackSearch(string inputText) {
            List<string> matches = new List<string>();
            foreach (string word in words) {
                if (word.Contains(inputText)) {
                    matches.Add(word);
                }
            }

            return matches;
        }

        private List<string> FindAllMatches(TrieNode node, string currentPrefix) {
            void FindAllMatchesRecursive(TrieNode node, string currentPrefix, List<string> matches) {
                if (node.IsEndOfWord) {
                    matches.Add(currentPrefix);
                }

                foreach (var childNode in node.Children.Values) {
                    FindAllMatchesRecursive(childNode, currentPrefix + childNode.Value, matches);
                }
            }

            List<string> matches = new List<string>();
            FindAllMatchesRecursive(node, currentPrefix, matches);
            return matches;
        }
    }

    public static class TMP_TextExt {
        public static TMP_Text PlaceholderText(this TMP_InputField inputField) {
            var result = inputField.textViewport.transform.Find("Placeholder")?.GetComponent<TMP_Text>();
            if (result != null) {
                return result;
            }
            else {
                Debug.LogError($"No child named 'Placeholder' found under inputField: {inputField.FullPath()}");
                return null;
            }
        }
    }
    
    public static class MonobehaviourExt {
        public static void InvokeRealtime(this MonoBehaviour mb, string methodName, float time) {
            mb.StartCoroutine(WaitRealtimeToInvoke(mb, methodName, time));
        }

        public static IEnumerator WaitRealtimeToInvoke(this MonoBehaviour mb, string methodName, float time) {
            yield return new WaitForSecondsRealtime(time);
            
            mb.Invoke(methodName, 0f);
        }
    }

    public static class ColliderExt {
        public static bool PlayerIsInCollider(this Collider collider) {
            Vector3 playerPosition = Player.instance.PlayerCam.transform.position;
            Vector3 closestPoint = collider.ClosestPoint(playerPosition);

            return closestPoint == playerPosition;
        }
    }

    public static class TransformExt {
        public static int GetDepth(this Transform transform) {
            int depth = 0;
            Transform current = transform;
            while (current.parent != null) {
                depth++;
                current = current.parent;
            }

            return depth;
        }
        
        // Returns a Vector3 localScale that mimics the desired lossyScale for this Transform
        public static Vector3 LossyToLocalScale(this Transform transform, Vector3 targetLossyScale) {
            if (transform.parent != null) {
                // Calculate the local scale based on the lossyScale and parent's lossyScale
                Vector3 parentLossyScale = transform.parent.lossyScale;
                return new Vector3(
                    targetLossyScale.x / parentLossyScale.x,
                    targetLossyScale.y / parentLossyScale.y,
                    targetLossyScale.z / parentLossyScale.z
                );
            }
            else {
                // If there is no parent, localScale is the same as lossyScale
                return targetLossyScale;
            }
        }
        
        public static List<Transform> GetChildren(this Transform t) => t.Cast<Transform>().ToList();
    }

    public static class Utils {
        public static void Clear(this RenderTexture renderTexture) {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = rt;
        }
        
        public static void ForceRefresh(this MeshCollider meshCollider) {
            // Hack to force the MeshCollider to refresh the bounds
            Mesh mesh = meshCollider.sharedMesh;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
        
        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> keyValuePairs) {
            return keyValuePairs.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        
        public static V GetValue<K, V>(this IDictionary<K, V> dict, K key, V defaultValue = default) {
            return dict.TryGetValue(key, out V value) ? value : defaultValue;
        }
        
        public static T CopyFrom<T>(this Component comp, T other) where T : Component {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                 BindingFlags.Default;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (PropertyInfo pinfo in pinfos) {
                object[] attributes = pinfo.GetCustomAttributes(false);
                bool isObsolete = attributes.OfType<ObsoleteAttribute>().Any();
                // Don't copy obsolete properties or the name field
                if (pinfo.CanWrite && !isObsolete && pinfo.Name != "name") {
                    try {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch {
                    } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }

            FieldInfo[] finfos = type.GetFields(flags);
            foreach (FieldInfo finfo in finfos) {
                // Don't copy the name field
                if (finfo.Name == "name") continue;
                
                finfo.SetValue(comp, finfo.GetValue(other));
            }

            return comp as T;
        }

        public static bool IsInActiveScene(this Component c) {
            return c.gameObject.scene.name == LevelManager.instance.activeSceneName;
        }

        public static bool IsInActiveScene(this GameObject o) {
            return o.scene.name == LevelManager.instance.activeSceneName;
        }

        public static bool IsInLoadedScene(this Component c) {
            return LevelManager.instance.loadedLevels.Contains(c.gameObject.scene.name.ToLevel());
        }

        public static bool IsInLoadedScene(this GameObject o) {
            Levels level = o.scene.name.ToLevel();
            return level == Levels.ManagerScene || LevelManager.instance.loadedLevels.Contains(level);
        }

        public static bool TaggedAsPlayer(this Component c) {
            return c.CompareTag("Player");
        }

        public static bool TaggedAsPlayer(this GameObject o) {
            return o.CompareTag("Player");
        }

        public static string FullPath(this GameObject o) {
            if (o == null) {
                return "(null)";
            }
            Transform curNode = o.transform;
            string path = curNode.name;
            curNode = curNode.parent;
            while (curNode != null) {
                path = String.Concat($"{curNode.name}.", path);
                curNode = curNode.parent;
            }

            if (o.scene.IsValid()) {
                path = String.Concat($"{o.scene.name}.", path);
            }

            return path;
        }

        public static string FullPath(this Component c) {
            if (c != null && c.gameObject != null) {
                return FullPath(c.gameObject);
            }
            else {
                return "(null)";
            }
        }

        public static T PasteComponent<T>(this GameObject go, T toAdd) where T : Component {
            return go.AddComponent<T>().CopyFrom(toAdd);
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component {
            return go.TryGetComponent(out T foundComponent) ? foundComponent : go.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(this Component component) where T : Component {
            return GetOrAddComponent<T>(component.gameObject);
        }
        
        public static T[] GetComponentsInChildrenRecursively<T>(this GameObject parent) where T : Component {
            return parent.transform.GetComponentsInChildrenRecursively<T>();
        }
        
        public static T[] GetComponentsInChildrenRecursively<T>(this Transform parent) where T : Component {
            List<T> components = new List<T>();
            GetComponentsInChildrenRecursivelyHelper(parent, ref components);
            return components.ToArray();
        }

        static void GetComponentsInChildrenRecursivelyHelper<T>(Transform parent, ref List<T> componentsSoFar)
            where T : Component {
            T[] maybeComponents = parent.GetComponents<T>();
            componentsSoFar.AddRange(maybeComponents.Where(maybeComponent => maybeComponent != default(T)));

            foreach (Transform child in parent) {
                GetComponentsInChildrenRecursivelyHelper(child, ref componentsSoFar);
            }
        }

        public static T[] GetComponentsInChildrenOnly<T>(this Transform parent) where T : Component {
            T[] all = parent.GetComponentsInChildren<T>();
            T[] children = new T[all.Length - 1];
            int index = 0;
            foreach (T each in all) {
                if (each.transform != parent) {
                    children[index] = each;
                    index++;
                }
            }

            return children;
        }

        public static T GetComponentInChildrenOnly<T>(this Transform parent) where T : Component {
            T[] all = parent.GetComponentsInChildren<T>();
            foreach (T each in all) {
                if (each.transform != parent) return each;
            }

            return null;
        }

        // Recursively search up the transform tree through parents to find a T Component
        public static T FindInParentsRecursively<T>(this Transform child) where T : Component {
            T component = child.GetComponent<T>();
            Transform parent = child.parent;
            if (component != null) {
                return component;
            }

            if (parent != null) {
                return FindInParentsRecursively<T>(parent);
            }

            return null;
        }

        public static T FindInParentsRecursively<T>(this GameObject child) where T : Component =>
            FindInParentsRecursively<T>(child.transform);
        
        public static T FindDimensionObjectRecursively<T>(this Transform go) where T : DimensionObject =>
            FindInParentsRecursively<T>(go);

        public static T FindDimensionObjectRecursively<T>(this Component c) where T : DimensionObject =>
            FindDimensionObjectRecursively<T>(c.transform);

        public static T FindDimensionObjectRecursively<T>(this GameObject o) where T : DimensionObject =>
            FindDimensionObjectRecursively<T>(o.transform);

        public static Transform[] GetChildrenMatchingNameRecursively(this Transform transform, string nameToMatch, bool selectInactive = false) {
            void FindChildrenRecursivelyWithName(Transform curNode, ref List<Transform> selectionSoFar) {
                if (curNode.name.Contains(nameToMatch)) selectionSoFar.Add(curNode);

                foreach (Transform child in curNode.transform.GetComponentsInChildren<Transform>(selectInactive)) {
                    if (child != curNode) FindChildrenRecursivelyWithName(child, ref selectionSoFar);
                }
            }

            List<Transform> matches = new List<Transform>();
            FindChildrenRecursivelyWithName(transform, ref matches);
            return matches.ToArray();
        }

        public static bool IsVisibleFrom(this Renderer r, Camera camera) {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, r.bounds);
        }

        public static Color GetColorFromRenderer(this Renderer r, string colorPropertyName = "_Color") {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            r.GetPropertyBlock(propBlock);
            return propBlock.GetColor(colorPropertyName);
        }
        
        public static Color GetHDRColorFromRenderer(this Renderer r, string colorPropertyName = "_Color") {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            r.GetPropertyBlock(propBlock);
            return (Color)propBlock.GetVector(colorPropertyName);
        }

        public static void SetColorForRenderer(this Renderer r, Color color, string colorPropertyName = "_Color") {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorPropertyName, color);
            r.SetPropertyBlock(propBlock);
        }
        
        public static void SetHDRColorForRenderer(this Renderer r, Color hdrColor, string colorPropertyName = "_Color") {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            r.GetPropertyBlock(propBlock);
            propBlock.SetVector(colorPropertyName, (Vector4)hdrColor);
            r.SetPropertyBlock(propBlock);
        }

        public static void SetLayerRecursively(GameObject obj, int layer) {
            obj.layer = layer;

            foreach (Transform child in obj.transform) {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        public static float Perlin3D(float x, float y, float z) {
            float xy = Mathf.PerlinNoise(x, y);
            float xz = Mathf.PerlinNoise(x, z);
            float yz = Mathf.PerlinNoise(y, z);

            float yx = Mathf.PerlinNoise(y, x);
            float zx = Mathf.PerlinNoise(z, x);
            float zy = Mathf.PerlinNoise(z, y);

            return (xy + xz + yz + yx + zx + zy) / 6f;
        }

        public static float Vector3InverseLerp(Vector3 a, Vector3 b, Vector3 value) {
            Vector3 AB = b - a;
            Vector3 AV = value - a;
            return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
        }
    }
    
    public static class GuidEx {
        public static bool IsGuid(this string value) {
            return Guid.TryParse(value, out _);
        }
    }

    public class PolarCoordinate {
        public Angle angle;
        public float radius;
        public float y;

        public PolarCoordinate(float newRadius, Vector3 cartesianPoint) {
            radius = newRadius;
            angle = Angle.Radians((Mathf.Atan2(cartesianPoint.z, cartesianPoint.x) + 2 * Mathf.PI) % (2 * Mathf.PI));
            y = cartesianPoint.y;
        }

        public PolarCoordinate(float newRadius, Angle newAngle) {
            radius = newRadius;
            angle = newAngle;
        }

        public override string ToString() {
            return "(" + radius.ToString("0.####") + ", " + (angle.radians / Mathf.PI).ToString("0.####") +
                   "π rads)\t(" + y + " y-value)";
        }

        /// <summary>
        ///     Creates a Vector3 from a PolarCoordinate, restoring the Y value if it was created with a Vector3
        /// </summary>
        /// <returns>A Vector3 containing the PolarToCartesian transformation for the X and Z values, and the restored Y value</returns>
        public Vector3 PolarToCartesian() {
            return new Vector3(radius * Mathf.Cos(angle.radians), y, radius * Mathf.Sin(angle.radians));
        }

        /// <summary>
        ///     Creates a PolarCoordinate from a Vector3's X and Z values, and retains the Y value for converting back later
        /// </summary>
        /// <param name="cart"></param>
        /// <returns>A PolarCoordinate based on the X and Z values only.</returns>
        public static PolarCoordinate CartesianToPolar(Vector3 cart) {
            float radius = Mathf.Sqrt(Mathf.Pow(cart.x, 2) + Mathf.Pow(cart.z, 2));
            return new PolarCoordinate(radius, cart);
        }
    }

    public static class ExtDebug {
        public static void DrawPlane(Vector3 point, Vector3 normal, float height, float width, Color color) {
            if (normal == Vector3.zero) return;

            // Calculate two perpendicular vectors to the normal
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            if (right == Vector3.zero) {
                right = Vector3.Cross(normal, Vector3.right).normalized;
            }
            Vector3 up = Vector3.Cross(right, normal).normalized;

            // Construct the transformation matrix directly
            Matrix4x4 trs = Matrix4x4.TRS(point, Quaternion.identity, Vector3.one) * 
                            new Matrix4x4(
                                new Vector4(right.x, right.y, right.z, 0),
                                new Vector4(up.x, up.y, up.z, 0),
                                new Vector4(normal.x, normal.y, normal.z, 0),
                                new Vector4(0, 0, 0, 1)
                            );

            Gizmos.matrix = trs;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
            float depth = 0.0001f;
            Gizmos.DrawCube(Vector3.zero, new Vector3(width, height, depth));
            Gizmos.color = new Color(color.r, color.g, color.b, 1f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, depth));
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }
        
        // Draws a plane with a specified normal, right, and up vector
        // Will always draw with a consistent orientation unlike the method above which tries to infer right and up from the normal
        public static void DrawPlane(Vector3 point, Vector3 normal, Vector3 right, Vector3 up, float height, float width, Color color) {
            if (normal == Vector3.zero || right == Vector3.zero || up == Vector3.zero) return;

            // Ensure that right, up, and normal are orthonormal
            right = right.normalized;
            up = up.normalized;
            normal = normal.normalized;

            // Construct the transformation matrix directly
            Matrix4x4 trs = Matrix4x4.TRS(point, Quaternion.identity, Vector3.one) *
                            new Matrix4x4(
                                new Vector4(right.x, right.y, right.z, 0),
                                new Vector4(up.x, up.y, up.z, 0),
                                new Vector4(normal.x, normal.y, normal.z, 0),
                                new Vector4(0, 0, 0, 1)
                            );

            Gizmos.matrix = trs;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
            float depth = 0.0001f;
            Gizmos.DrawCube(Vector3.zero, new Vector3(width, height, depth));
            Gizmos.color = new Color(color.r, color.g, color.b, 1f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, depth));
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }


        
        //Draws just the box at where it is currently hitting.
        public static void DrawBoxCastOnHit(
            Vector3 origin,
            Vector3 halfExtents,
            Quaternion orientation,
            Vector3 direction,
            float hitInfoDistance,
            Color color
        ) {
            origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
            DrawBox(origin, halfExtents, orientation, color);
        }

        //Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
        public static void DrawBoxCastBox(
            Vector3 origin,
            Vector3 halfExtents,
            Quaternion orientation,
            Vector3 direction,
            float distance,
            Color color
        ) {
            direction.Normalize();
            Box bottomBox = new Box(origin, halfExtents, orientation);
            Box topBox = new Box(origin + direction * distance, halfExtents, orientation);

            Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color);
            Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
            Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
            Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color);
            Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color);
            Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
            Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
            Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color);

            DrawBox(bottomBox, color);
            DrawBox(topBox, color);
        }

        public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color) {
            DrawBox(new Box(origin, halfExtents, orientation), color);
        }

        public static void DrawBox(Box box, Color color) {
            Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color);
            Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color);
            Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color);
            Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color);

            Debug.DrawLine(box.backTopLeft, box.backTopRight, color);
            Debug.DrawLine(box.backTopRight, box.backBottomRight, color);
            Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color);
            Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color);

            Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color);
            Debug.DrawLine(box.frontTopRight, box.backTopRight, color);
            Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color);
            Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color);
        }

        //This should work for all cast types
        public static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance) {
            return origin + direction.normalized * hitInfoDistance;
        }

        static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
            Vector3 direction = point - pivot;
            return pivot + rotation * direction;
        }

        public struct Box {
            public Vector3 localFrontTopLeft { get; private set; }
            public Vector3 localFrontTopRight { get; private set; }
            public Vector3 localFrontBottomLeft { get; private set; }
            public Vector3 localFrontBottomRight { get; private set; }
            public Vector3 localBackTopLeft => -localFrontBottomRight;
            public Vector3 localBackTopRight => -localFrontBottomLeft;
            public Vector3 localBackBottomLeft => -localFrontTopRight;
            public Vector3 localBackBottomRight => -localFrontTopLeft;

            public Vector3 frontTopLeft => localFrontTopLeft + origin;
            public Vector3 frontTopRight => localFrontTopRight + origin;
            public Vector3 frontBottomLeft => localFrontBottomLeft + origin;
            public Vector3 frontBottomRight => localFrontBottomRight + origin;
            public Vector3 backTopLeft => localBackTopLeft + origin;
            public Vector3 backTopRight => localBackTopRight + origin;
            public Vector3 backBottomLeft => localBackBottomLeft + origin;
            public Vector3 backBottomRight => localBackBottomRight + origin;

            public Vector3 origin { get; }

            public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents) {
                Rotate(orientation);
            }

            public Box(Vector3 origin, Vector3 halfExtents) {
                localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
                localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
                localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
                localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

                this.origin = origin;
            }


            public void Rotate(Quaternion orientation) {
                localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
                localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
                localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
                localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
            }
        }
    }

    [Serializable]
    public class Angle {
        public enum Quadrant {
            I, // [0 - 90)
            II, // [90 - 180)
            III, // [180 - 270)
            IV // [270 - 360)
        }

        public static Angle D0 = new Angle(0);
        public static Angle D90 = new Angle(Mathf.PI * 0.5f);
        public static Angle D180 = new Angle(Mathf.PI);
        public static Angle D270 = new Angle(Mathf.PI * 1.5f);
        public static Angle D360 = new Angle(Mathf.PI * 2);

        public float radians;


        // Constructors
        Angle(float _radians) {
            radians = _radians;
        }

        Angle(Angle other) {
            radians = other.radians;
        }

        public float degrees {
            get => Mathf.Rad2Deg * radians;
            set => radians = Mathf.Deg2Rad * value;
        }

        public Quadrant quadrant {
            get {
                float normalizedDegrees = degrees;
                while (normalizedDegrees < 0) {
                    normalizedDegrees += 360;
                }

                while (normalizedDegrees >= 360) {
                    normalizedDegrees -= 360;
                }

                return (Quadrant) ((int) normalizedDegrees / 90);
            }
        }

        /// <summary>
        ///     Gives the same normalized angle on the opposite quadrant of the circle
        /// </summary>
        /// <returns>A new Angle after being added and normalized (does not modify the original Angle)</returns>
        public Angle reversed => new Angle(this).Reverse();

        /// <summary>
        ///     Gives the normalized angle within the [0-2*PI) range
        /// </summary>
        /// <returns>A new angle that is normalized (does not modify the original Angle)</returns>
        public Angle normalized => new Angle(this).Normalize();

        public override string ToString() {
            return degrees + "°";
        }

        public static Angle Radians(float radians) {
            return new Angle(radians);
        }

        public static Angle Degrees(float degrees) {
            return new Angle(Mathf.Deg2Rad * degrees);
        }

        /// <summary>
        ///     Gives the same normalized angle on the opposite quadrant of the circle
        /// </summary>
        /// <returns>This angle after being added and normalized</returns>
        public Angle Reverse() {
            radians += Mathf.PI;
            return Normalize();
        }

        /// <summary>
        ///     Normalizes an angle to [0-2*PI) range
        /// </summary>
        /// <returns>This angle after being modified</returns>
        public Angle Normalize() {
            return Normalize(Radians(0), Radians(2 * Mathf.PI));
        }

        /// <summary>
        ///     Normalizes an angle to [startRange, endRange) range
        /// </summary>
        /// <param name="startRange">Start of the range to normalize to (inclusive)</param>
        /// <param name="endRange">End of the range to normalize to (exclusive)</param>
        /// <returns>This angle after being modified</returns>
        public Angle Normalize(Angle startRange, Angle endRange) {
            Angle width = endRange - startRange;
            Angle offsetValue = this - startRange; // value relative to startRange

            radians = offsetValue.radians - Mathf.Floor(offsetValue.radians / width.radians) * width.radians +
                      startRange.radians;
            if (this == endRange) radians = startRange.radians;
            return this;
        }

        /// <summary>
        ///     Calculates the smallest angle possible between this angle and the given angle
        /// </summary>
        /// <param name="other"></param>
        /// <returns>A new Angle containing the angle between this angle and the input</returns>
        public Angle AngleBetween(Angle other) {
            return AngleBetween(this, other);
        }

        /// <summary>
        ///     Calculates the smallest angle possible between the given angles
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>A new Angle containing the angle between the two inputs</returns>
        public static Angle AngleBetween(Angle a, Angle b) {
            float aMinusB = (a - b).Normalize().radians;
            float bMinusA = (b - a).Normalize().radians;

            return new Angle(Mathf.Min(aMinusB, bMinusA));
        }

        /// <summary>
        ///     Determines whether movement from a to b is clockwise or counter-clockwise.
        ///     Note that clockwise movement generally corresponds to the Angle value increasing.
        /// </summary>
        /// <param name="a">Starting Angle</param>
        /// <param name="b">Ending Angle</param>
        /// <returns>True if movement from a1 to a2 is clockwise, false otherwise</returns>
        public static bool IsClockwise(Angle a, Angle b) {
            float aMinusB = (a - b).Normalize().radians;
            float bMinusA = (b - a).Normalize().radians;

            return aMinusB > bMinusA;
        }

        /// <summary>
        ///     Calculates the difference between this angle and another angle.
        ///     Assumes that any difference over 180° is to be wrapped back around to a value < 180°
        /// </summary>
        /// <param name="other"></param>
        /// <returns>
        ///     Difference between two angles, with magnitude < 180°</returns>
        public Angle WrappedAngleDiff(Angle other) {
            return WrappedAngleDiff(this, other);
        }

        /// <summary>
        ///     Calculates the difference between the given angles.
        ///     Assumes that any difference over 180° is to be wrapped back around to a value < 180°
        /// </summary>
        /// <param name="other"></param>
        /// <returns>
        ///     Difference between two angles, with magnitude < 180°</returns>
        public static Angle WrappedAngleDiff(Angle a, Angle b) {
            Angle angleDiff = a.normalized - b.normalized;
            if (angleDiff.degrees > 180) {
                // Debug.Log("Before: " + angleDiff + "\nAfter: " + -(D360 - angleDiff));
                angleDiff = -(D360 - angleDiff);
            }
            else if (angleDiff.degrees < -180) {
                // Debug.Log("Before: " + angleDiff + "\nAfter: " + (D360 + angleDiff));
                angleDiff = D360 + angleDiff;
            }

            return angleDiff;
        }

        /// <summary>
        ///     Determines whether the test angle falls between angles a and b (clockwise from a to b)
        /// </summary>
        /// <param name="test"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>True if test falls between a and b (clockwise), false otherwise</returns>
        public static bool IsAngleBetween(Angle test, Angle a, Angle b) {
            float testNormalized = test.normalized.radians;
            float aNormalized = a.normalized.radians;
            float bNormalized = b.normalized.radians;

            if (aNormalized < bNormalized)
                return aNormalized <= testNormalized && testNormalized <= bNormalized;
            return aNormalized <= testNormalized || testNormalized <= bNormalized;
        }

        // Operators
        public static Angle operator *(int scalar, Angle b) {
            return new Angle(b.radians * scalar);
        }

        public static Angle operator *(float scalar, Angle b) {
            return new Angle(b.radians * scalar);
        }

        public static Angle operator -(Angle a) {
            return new Angle(-a.radians);
        }

        public static Angle operator +(Angle a, Angle b) {
            return new Angle(a.radians + b.radians);
        }

        public static Angle operator -(Angle a, Angle b) {
            return new Angle(a.radians - b.radians);
        }

        public static Angle operator /(Angle a, Angle b) {
            return new Angle(a.radians / b.radians);
        }

        public static Angle operator /(Angle a, float b) {
            return new Angle(a.radians / b);
        }

        public static Angle operator *(Angle a, Angle b) {
            return new Angle(a.radians * b.radians);
        }

        public static bool operator <(Angle a, Angle b) {
            return a.radians < b.radians;
        }

        public static bool operator >(Angle a, Angle b) {
            return a.radians > b.radians;
        }

        public static bool operator ==(Angle a, Angle b) {
            return a.radians == b.radians;
        }

        public static bool operator !=(Angle a, Angle b) {
            return a.radians != b.radians;
        }

        public override bool Equals(object obj) {
            Angle angleObj = obj as Angle;
            return angleObj != null && angleObj == this;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }

    public static class Base9FontConversions {
        public static Dictionary<int, char> ValueToBase9Char = new Dictionary<int, char> {
            {0, '0'},
            {1, '1'},
            {2, '2'},
            {3, '3'},
            {4, '4'},
            {5, '5'},
            {6, '6'},
            {7, '7'},
            {8, '8'},
            {9, '9'},
            {10, '!'},
            {11, '@'},
            {12, '#'},
            {13, '$'},
            {14, '%'},
            {15, '^'},
            {16, '&'},
            {17, '*'},
            {18, '('},
            {19, ')'},
            {20, 'a'},
            {21, 'b'},
            {22, 'c'},
            {23, 'd'},
            {24, 'e'},
            {25, 'f'},
            {26, 'g'},
            {27, 'h'},
            {28, 'i'},
            {29, 'j'},
            {30, 'k'},
            {31, 'l'},
            {32, 'm'},
            {33, 'n'},
            {34, 'o'},
            {35, 'p'},
            {36, 'q'},
            {37, 'r'},
            {38, 's'},
            {39, 't'},
            {40, 'u'},
            {41, 'v'},
            {42, 'w'},
            {43, 'x'},
            {44, 'y'},
            {45, 'z'},
            {46, 'A'},
            {47, 'B'},
            {48, 'C'},
            {49, 'D'},
            {50, 'E'},
            {51, 'F'},
            {52, 'G'},
            {53, 'H'},
            {54, 'I'},
            {55, 'J'},
            {56, 'K'},
            {57, 'L'},
            {58, 'M'},
            {59, 'N'},
            {60, 'O'},
            {61, 'P'},
            {62, 'Q'},
            {63, 'R'},
            {64, 'S'},
            {65, 'T'},
            {66, 'U'},
            {67, 'V'},
            {68, 'W'},
            {69, 'X'},
            {70, 'Y'},
            {71, 'Z'},
            {72, '['},
            {73, '\\'},
            {74, ']'},
            {75, '{'},
            {76, '|'},
            {77, '}'},
            {78, ','},
            {79, '.'},
            {80, '/'}
        };

        public static Dictionary<char, int> Base9CharToValue =
            ValueToBase9Char.ToDictionary(kv => kv.Value, kv => kv.Key);
    }

    public static class RightAngleRotations {
        static readonly Quaternion[] rightAngleRotations = {
            Quaternion.Euler(0, 0, 0),
            Quaternion.Euler(0, 0, 90),
            Quaternion.Euler(0, 0, 180),
            Quaternion.Euler(0, 0, 270),
            Quaternion.Euler(0, 90, 0),
            Quaternion.Euler(0, 90, 90),
            Quaternion.Euler(0, 90, 180),
            Quaternion.Euler(0, 90, 270),
            Quaternion.Euler(0, 180, 0),
            Quaternion.Euler(0, 180, 90),
            Quaternion.Euler(0, 180, 180),
            Quaternion.Euler(0, 180, 270),
            Quaternion.Euler(0, 270, 0),
            Quaternion.Euler(0, 270, 90),
            Quaternion.Euler(0, 270, 180),
            Quaternion.Euler(0, 270, 270),
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(90, 0, 90),
            Quaternion.Euler(90, 0, 180),
            Quaternion.Euler(90, 0, 270),
            Quaternion.Euler(90, 90, 0),
            Quaternion.Euler(90, 90, 90),
            Quaternion.Euler(90, 90, 180),
            Quaternion.Euler(90, 90, 270),
            Quaternion.Euler(90, 180, 0),
            Quaternion.Euler(90, 180, 90),
            Quaternion.Euler(90, 180, 180),
            Quaternion.Euler(90, 180, 270),
            Quaternion.Euler(90, 270, 0),
            Quaternion.Euler(90, 270, 90),
            Quaternion.Euler(90, 270, 180),
            Quaternion.Euler(90, 270, 270),
            Quaternion.Euler(180, 0, 0),
            Quaternion.Euler(180, 0, 90),
            Quaternion.Euler(180, 0, 180),
            Quaternion.Euler(180, 0, 270),
            Quaternion.Euler(180, 90, 0),
            Quaternion.Euler(180, 90, 90),
            Quaternion.Euler(180, 90, 180),
            Quaternion.Euler(180, 90, 270),
            Quaternion.Euler(180, 180, 0),
            Quaternion.Euler(180, 180, 90),
            Quaternion.Euler(180, 180, 180),
            Quaternion.Euler(180, 180, 270),
            Quaternion.Euler(180, 270, 0),
            Quaternion.Euler(180, 270, 90),
            Quaternion.Euler(180, 270, 180),
            Quaternion.Euler(180, 270, 270),
            Quaternion.Euler(270, 0, 0),
            Quaternion.Euler(270, 0, 90),
            Quaternion.Euler(270, 0, 180),
            Quaternion.Euler(270, 0, 270),
            Quaternion.Euler(270, 90, 0),
            Quaternion.Euler(270, 90, 90),
            Quaternion.Euler(270, 90, 180),
            Quaternion.Euler(270, 90, 270),
            Quaternion.Euler(270, 180, 0),
            Quaternion.Euler(270, 180, 90),
            Quaternion.Euler(270, 180, 180),
            Quaternion.Euler(270, 180, 270),
            Quaternion.Euler(270, 270, 0),
            Quaternion.Euler(270, 270, 90),
            Quaternion.Euler(270, 270, 180),
            Quaternion.Euler(270, 270, 270)
        };

        // Returns a 90 degree rotation relative to the given Transform rather than global axis
        public static Quaternion GetNearestRelativeToTransform(Quaternion comparedTo, Transform relativeTo) {
            float minAngle = float.MaxValue;
            Quaternion returnRotation = Quaternion.identity;

            foreach (Quaternion q in rightAngleRotations) {
                Quaternion localRotation = relativeTo.rotation * q;
                float angleBetween = Quaternion.Angle(localRotation, comparedTo);
                if (angleBetween < minAngle) {
                    minAngle = angleBetween;
                    returnRotation = localRotation;
                }
            }

            return returnRotation;
        }

        public static Quaternion GetNearest(Quaternion comparedTo) {
            float minAngle = float.MaxValue;
            Quaternion returnRotation = Quaternion.identity;

            foreach (Quaternion q in rightAngleRotations) {
                float angleBetween = Quaternion.Angle(q, comparedTo);
                if (angleBetween < minAngle) {
                    minAngle = angleBetween;
                    returnRotation = q;
                }
            }

            return returnRotation;
        }
    }

    public class DebugLogger {
        readonly Object context;
        public Func<bool> enabled;
        readonly string id = "";
        readonly bool idSet;

        public DebugLogger(Object context, string id) : this(context, id, () => true) {}

        public DebugLogger(Object context, string id, Func<bool> enabled) {
            this.enabled = enabled;
            this.context = context;

            if (context is GameObject) {
                ISaveableObject saveableContext = (context as GameObject).GetComponent<ISaveableObject>();
                if (saveableContext != null) {
                    try {
                        this.id = id;
                        idSet = true;
                    }
                    catch {
                    }
                }
            }
        }

        // Forces the debug log to print with the context, use for debugging then remove
        public void ForceDebugLog(object message) {
            string name = (context is Component component) ? component.FullPath() : ((context is GameObject go) ? go.FullPath() : context.name);
            Debug.Log(
                $"{message}\n───────\nGameObject: {name}\nFrame: {Time.frameCount}{(idSet ? $"\nId: {id}" : "")}\n───────",
                context
            );
        }
        
        public void LogWithContext(object message, Object context, bool forceLog = false) {
            if (forceLog || enabled.Invoke()) {
                Debug.Log(MessageWithContext(message, context), context);
            }
        }

        public void Log(object message, bool forceLog = false) {
            if (forceLog || enabled.Invoke()) {
                Debug.Log(MessageWithContext(message, context), context);
            }
        }

        public void LogWarning(object message, bool forceLog = false) {
            if (forceLog || enabled.Invoke()) {
                Debug.LogWarning(MessageWithContext(message, context), context);
            }
        }
        
        public void LogWarningWithContext(object message, Object context, bool forceLog = false) {
            if (forceLog || enabled.Invoke()) {
                Debug.LogWarning(MessageWithContext(message, context), context);
            }
        }

        public void LogError(object message, bool forceLog = false) {
            if (forceLog || enabled.Invoke()) {
                Debug.LogError(MessageWithContext(message, context), context);
            }
        }
        
        public void LogErrorWithContext(object message, Object context, bool forceLog = false) {
            if (forceLog || enabled.Invoke()) {
                Debug.LogError(MessageWithContext(message, context), context);
            }
        }
        
        private string MessageWithContext(object message, Object context) {
            string name = (context is Component component) ? component.FullPath() : ((context is GameObject go) ? go.FullPath() : context.name);
            
            string gameObjectLabel = $"<color=#6a9fb5>GameObject:</color>   ";
            string frameLabel = $"<color=#b59f6a>Frame:</color>        ";
            string idLabel = idSet ? $"\n<color=#6ab597>Id:</color>           " : "";
            
            string gameObjectLine = $"{gameObjectLabel}{name}";
            string frameLine = $"{frameLabel}{Time.frameCount}";
            string idLine = idSet ? $"{idLabel}{id}" : "";
            
            int charCount = 14 + Math.Max(name.Length, Math.Max(Time.frameCount.ToString().Length, id.Length));
            const char M_DASH = '─';

            string SPACER = $"\n<color=white>{new string('\u2550', charCount)}</color>\n";
            
            return $"{message}{SPACER}{gameObjectLine}\n{frameLine}{idLine}{SPACER}";
        }
    }

    namespace ShaderUtils {
        public enum BlendMode {
            Opaque,
            Cutout,
            Fade, // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent, // Physically plausible transparency mode, implemented as alpha pre-multiply
            TransparentWithBorder // Same as above but renders to a different renderType
        }

        public static class ShaderUtils {
            public enum SuberspectiveBlendMode {
                Opaque,
                Transparent,
                CullEverything,
                InvertColors
            }
            
            public enum ShaderPropertyType {
                Color,
                Vector,
                Float,
                Range,
                Texture
            }

            static readonly Dictionary<string, ShaderPropertyType> knownShaderProperties =
                new Dictionary<string, ShaderPropertyType> {
                    {"_Color", ShaderPropertyType.Color},
                    {"_EmissionColor", ShaderPropertyType.Color},
                    {"_MainTex", ShaderPropertyType.Texture},
                    {"_Cutoff", ShaderPropertyType.Range},
                    {"_Glossiness", ShaderPropertyType.Range},
                    {"_GlossMapScale", ShaderPropertyType.Range},
                    {"_SmoothnessTextureChannel", ShaderPropertyType.Float},
                    {"_SpecColor", ShaderPropertyType.Color},
                    {"_SpecGlossMap", ShaderPropertyType.Texture},
                    {"_SpecularHighlights", ShaderPropertyType.Float},
                    {"_GlossyReflections", ShaderPropertyType.Float},
                    {"_BumpScale", ShaderPropertyType.Float},
                    {"_BumpMap", ShaderPropertyType.Texture},
                    {"_Parallax", ShaderPropertyType.Range},
                    {"_ParallaxMap", ShaderPropertyType.Texture},
                    {"_OcclusionStrength", ShaderPropertyType.Range},
                    {"_OcclusionMap", ShaderPropertyType.Texture},
                    {"_EmissionMap", ShaderPropertyType.Texture},
                    {"_DetailMask", ShaderPropertyType.Texture},
                    {"_DetailAlbedoMap", ShaderPropertyType.Texture},
                    {"_DetailNormalMapScale", ShaderPropertyType.Float},
                    {"_DetailNormalMap", ShaderPropertyType.Texture},
                    {"_Metallic", ShaderPropertyType.Range},
                    // Dissolve shader properties
                    {"_Color2", ShaderPropertyType.Color},
                    {"_DissolveAmount", ShaderPropertyType.Range},
                    {"_BurnSize", ShaderPropertyType.Range},
                    {"_BurnRamp", ShaderPropertyType.Texture},
                    {"_BurnColor", ShaderPropertyType.Color},
                    {"_EmissionAmount", ShaderPropertyType.Float},
                    // Water shader properties
                    {"_FlowMap", ShaderPropertyType.Texture},
                    {"_DerivHeightMap", ShaderPropertyType.Texture},
                    {"_HeightScale", ShaderPropertyType.Float},
                    {"_HeightScaleModulated", ShaderPropertyType.Float},
                    {"_Tiling", ShaderPropertyType.Float},
                    {"_Speed", ShaderPropertyType.Float},
                    {"_FlowStrength", ShaderPropertyType.Float},
                    {"_FlowOffset", ShaderPropertyType.Range},
                    {"_WaterFogColor", ShaderPropertyType.Color},
                    {"_WaterFogDensity", ShaderPropertyType.Range},
                    {"_RefractionStrength", ShaderPropertyType.Range},
                    // TextMeshPro properties
                    {"_FaceColor", ShaderPropertyType.Color},
                    {"_FaceDilate", ShaderPropertyType.Range},
                    {"_OutlineColor", ShaderPropertyType.Color},
                    {"_OutlineWidth", ShaderPropertyType.Range},
                    {"_OutlineSoftness", ShaderPropertyType.Range},
                    {"_UnderlayColor", ShaderPropertyType.Color},
                    {"_UnderlayOffsetX", ShaderPropertyType.Range},
                    {"_UnderlayOffsetY", ShaderPropertyType.Range},
                    {"_UnderlayDilate", ShaderPropertyType.Range},
                    {"_UnderlaySoftness", ShaderPropertyType.Range},
                    {"_WeightNormal", ShaderPropertyType.Float},
                    {"_WeightBold", ShaderPropertyType.Float},
                    {"_ShaderFlags", ShaderPropertyType.Float},
                    {"_ScaleRatioA", ShaderPropertyType.Float},
                    {"_ScaleRatioB", ShaderPropertyType.Float},
                    {"_ScaleRatioC", ShaderPropertyType.Float},
                    {"_TextureWidth", ShaderPropertyType.Float},
                    {"_TextureHeight", ShaderPropertyType.Float},
                    {"_GradientScale", ShaderPropertyType.Float},
                    {"_ScaleX", ShaderPropertyType.Float},
                    {"_ScaleY", ShaderPropertyType.Float},
                    {"_PerspectiveFilter", ShaderPropertyType.Range},
                    {"_Sharpness", ShaderPropertyType.Range},
                    {"_VertexOffsetX", ShaderPropertyType.Float},
                    {"_VertexOffsetY", ShaderPropertyType.Float},
                    {"_ClipRect", ShaderPropertyType.Vector},
                    {"_MaskSoftnessX", ShaderPropertyType.Float},
                    {"_MaskSoftnessY", ShaderPropertyType.Float},
                    {"_StencilComp", ShaderPropertyType.Float},
                    {"_Stencil", ShaderPropertyType.Float},
                    {"_StencilOp", ShaderPropertyType.Float},
                    {"_StencilWriteMask", ShaderPropertyType.Float},
                    {"_StencilReadMask", ShaderPropertyType.Float},
                    {"_CullMode", ShaderPropertyType.Float},
                    {"_ColorMask", ShaderPropertyType.Float}
                };

            public static void CopyMatchingPropertiesFromMaterial(this Material copyInto, Material copyFrom) {
                foreach (string propertyName in knownShaderProperties.Keys) {
                    // Skip any property not known to both materials
                    if (!copyInto.HasProperty(propertyName) || !copyFrom.HasProperty(propertyName)) continue;
                    switch (knownShaderProperties[propertyName]) {
                        case ShaderPropertyType.Color:
                            copyInto.SetColor(propertyName, copyFrom.GetColor(propertyName));
                            break;
                        case ShaderPropertyType.Vector:
                            copyInto.SetVector(propertyName, copyFrom.GetVector(propertyName));
                            break;
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            copyInto.SetFloat(propertyName, copyFrom.GetFloat(propertyName));
                            break;
                        case ShaderPropertyType.Texture:
                            copyInto.SetTexture(propertyName, copyFrom.GetTexture(propertyName));
                            copyInto.SetTextureOffset(propertyName, copyFrom.GetTextureOffset(propertyName));
                            copyInto.SetTextureScale(propertyName, copyFrom.GetTextureScale(propertyName));
                            break;
                    }
                }

                string[] oldKeywords = copyInto.shaderKeywords;
                copyInto.shaderKeywords = copyFrom.shaderKeywords;
                if (copyInto.shader.name == "Custom/DimensionShaders/DimensionObjectSpecular" || copyInto.shader.name ==
                    "Custom/DimensionShaders/InverseDimensionObjectSpecular") {
                    switch (copyFrom.GetTag("RenderType", true)) {
                        case "TransparentCutout":
                            SetupMaterialWithBlendMode(ref copyInto, BlendMode.Cutout);
                            break;
                        case "Transparent":
                            SetupMaterialWithBlendMode(ref copyInto, BlendMode.Transparent);
                            break;
                        default:
                            SetupMaterialWithBlendMode(ref copyInto, BlendMode.Opaque);
                            break;
                    }
                }
            }

            public static void SetupMaterialWithBlendMode(ref Material material, BlendMode blendMode) {
                switch (blendMode) {
                    case BlendMode.Opaque:
                        material.SetOverrideTag("RenderType", "");
                        material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = -1;
                        break;
                    case BlendMode.Cutout:
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                        material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int) RenderQueue.AlphaTest;
                        break;
                    case BlendMode.Fade:
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int) RenderQueue.Transparent;
                        break;
                    case BlendMode.Transparent:
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int) RenderQueue.Transparent;
                        break;
                    case BlendMode.TransparentWithBorder:
                        material.SetOverrideTag("RenderType", "TransparentWithBorder");
                        material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int) RenderQueue.Transparent;
                        break;
                }
            }
            
            
            public static void BlitWithDepth(RenderTexture source, RenderTexture destination, Material material) {
                // Set the destination color buffer and the source depth buffer
                Graphics.SetRenderTarget(destination.colorBuffer, source.depthBuffer);

                // Set up orthographic projection
                GL.PushMatrix();
                GL.LoadOrtho();

                // Use the material pass
                material.SetPass(0);

                // Draw a fullscreen quad
                GL.Begin(GL.QUADS);
    
                GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
                GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0);
                GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
                GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
    
                GL.End();
                GL.PopMatrix();
            }
        }
    }
}