using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using LevelManagement;
using PortalMechanics;
using Saving;
using UnityEngine;
using UnityEngine.Rendering;
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
        public static T RandomElementFrom<T>(this ICollection<T> collection) {
            int count = collection.Count;
            T[] array = new T[count];
            collection.CopyTo(array, 0);
            return array[UnityEngine.Random.Range(0, count)];
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
    }

    public static class FloatExt {
        public static bool IsApproximately(this float f, float other) {
            return Math.Abs(f - other) < float.Epsilon;
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
    }

    public static class ColorExt {
        public static Color WithAlphaFrom(this Color self, Color from) {
            return new Color(self.r, self.g, self.b, from.a);
        }

        public static Color WithAlpha(this Color self, float alpha) {
            return new Color(self.r, self.g, self.b, alpha);
        }
    }
    
    public static class StringExt {
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

    public static class Utils {
        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> keyValuePairs) {
            return keyValuePairs.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        
        public static V GetValue<K, V>(this IDictionary<K, V> dict, K key, V defaultValue = default) {
            return dict.TryGetValue(key, out V value) ? value : defaultValue;
        }
        
        public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                 BindingFlags.Default;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (PropertyInfo pinfo in pinfos) {
                object[] attributes = pinfo.GetCustomAttributes(false);
                bool isObsolete = attributes.OfType<ObsoleteAttribute>().Any();
                if (pinfo.CanWrite && !isObsolete) {
                    try {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch {
                    } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }

            FieldInfo[] finfos = type.GetFields(flags);
            foreach (FieldInfo finfo in finfos) {
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
            return LevelManager.instance.loadedSceneNames.Contains(c.gameObject.scene.name);
        }

        public static bool IsInLoadedScene(this GameObject o) {
            return LevelManager.instance.loadedSceneNames.Contains(o.scene.name);
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
            return go.AddComponent<T>().GetCopyOf(toAdd);
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
            T[] children = new T[parent.childCount];
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

        // Recursively search up the transform tree through parents to find a DimensionObject
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

        public static Transform[] GetChildren(this Transform parent) {
            return parent.GetComponentsInChildrenOnly<Transform>();
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
        
        // Subvectors of Vector3
        public static Vector2 xy(this Vector3 v3) {
            return new Vector2(v3.x, v3.y);
        }

        public static Vector2 xz(this Vector3 v3) {
            return new Vector2(v3.x, v3.z);
        }

        public static Vector2 yz(this Vector3 v3) {
            return new Vector2(v3.y, v3.z);
        }

        // Subvectors of Vector4
        public static Vector2 xy(this Vector4 v4) {
            return new Vector2(v4.x, v4.y);
        }

        public static Vector2 xz(this Vector4 v4) {
            return new Vector2(v4.x, v4.z);
        }

        public static Vector2 xw(this Vector4 v4) {
            return new Vector2(v4.x, v4.w);
        }

        public static Vector2 yz(this Vector4 v4) {
            return new Vector2(v4.y, v4.z);
        }

        public static Vector2 yw(this Vector4 v4) {
            return new Vector2(v4.y, v4.w);
        }

        public static Vector2 zw(this Vector4 v4) {
            return new Vector2(v4.z, v4.w);
        }

        public static Vector3 xyz(this Vector4 v4) {
            return new Vector3(v4.x, v4.y, v4.z);
        }

        public static Vector3 xyw(this Vector4 v4) {
            return new Vector3(v4.x, v4.y, v4.w);
        }

        public static Vector3 xzw(this Vector4 v4) {
            return new Vector3(v4.x, v4.z, v4.w);
        }

        public static Vector3 yzw(this Vector4 v4) {
            return new Vector3(v4.y, v4.z, v4.w);
        }

        public static float Vector3InverseLerp(Vector3 a, Vector3 b, Vector3 value) {
            Vector3 AB = b - a;
            Vector3 AV = value - a;
            return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
        }
    }

    public class RunningAverage {
        readonly int maxSize;
        readonly List<float> samples;

        public RunningAverage(int maxSize) {
            samples = new List<float>();
            this.maxSize = maxSize;
        }

        public void AddValue(float value) {
            if (samples.Count == maxSize) samples.RemoveAt(0);
            samples.Add(value);
        }

        public float Average() {
            return Average(maxSize);
        }

        public float Average(int lastNSamples) {
            float sum = 0;
            int maxIndex = Mathf.Min(lastNSamples, samples.Count);
            for (int i = 0; i < maxIndex; i++) {
                sum += samples[i];
            }

            return sum / maxIndex;
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

        public DebugLogger(Object context, Func<bool> enabled) {
            this.enabled = enabled;
            this.context = context;

            if (context is GameObject) {
                ISaveableObject saveableContext = (context as GameObject).GetComponent<ISaveableObject>();
                if (saveableContext != null) {
                    try {
                        id = saveableContext.ID;
                        idSet = true;
                    }
                    catch {
                    }
                }
            }
        }

        public void Log(object message) {
            string name = (context is Component component) ? component.FullPath() : ((context is GameObject go) ? go.FullPath() : context.name);
            if (enabled.Invoke())
                Debug.Log(
                    $"{message}\n───────\nGameObject: {name}\nFrame: {Time.frameCount}{(idSet ? $"\nId: {id}" : "")}",
                    context
                );
        }

        public void LogWarning(object message) {
            string name = (context is Component component) ? component.FullPath() : ((context is GameObject go) ? go.FullPath() : context.name);
            if (enabled.Invoke())
                Debug.LogWarning(
                    $"{message}\n───────\nGameObject: {name}\nFrame: {Time.frameCount}{(idSet ? $"\nId: {id}" : "")}",
                    context
                );
        }

        public void LogError(object message) {
            string name = (context is Component component) ? component.FullPath() : ((context is GameObject go) ? go.FullPath() : context.name);
            if (enabled.Invoke())
                Debug.LogError(
                    $"{message}\n───────\nGameObject: {name}\nFrame: {Time.frameCount}{(idSet ? $"\nId: {id}" : "")}",
                    context
                );
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
                    {"_DissolveValue", ShaderPropertyType.Range},
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
        }
    }
}