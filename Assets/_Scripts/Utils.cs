using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpitaphUtils {
	[Serializable]
	public struct IntVector2 {
		public int x, y;

		public IntVector2(int _x, int _y) {
			x = _x;
			y = _y;
		}
	}
	
	public static class Utils {
		public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
			Type type = comp.GetType();
			if (type != other.GetType()) return null; // type mis-match
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default;
			PropertyInfo[] pinfos = type.GetProperties(flags);
			foreach (var pinfo in pinfos) {
				if (pinfo.CanWrite) {
					try {
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					}
					catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
				}
			}
			FieldInfo[] finfos = type.GetFields(flags);
			foreach (var finfo in finfos) {
				finfo.SetValue(comp, finfo.GetValue(other));
			}
			return comp as T;
		}

		public static bool TaggedAsPlayer(this Component c) {
			return c.CompareTag("Player");
		}
		public static bool TaggedAsPlayer(this GameObject o) {
			return o.CompareTag("Player");
		}

		public static T PasteComponent<T>(this GameObject go, T toAdd) where T : Component {
			return go.AddComponent<T>().GetCopyOf(toAdd) as T;
		}

		public static T[] GetComponentsInChildrenRecursively<T>(this Transform parent) where T : Component {
			List<T> components = new List<T>();
			GetComponentsInChildrenRecursivelyHelper(parent, ref components);
			return components.ToArray();
		}

		private static void GetComponentsInChildrenRecursivelyHelper<T>(Transform parent, ref List<T> componentsSoFar) {
			T maybeComponent = parent.GetComponent<T>();
			if (maybeComponent != null) {
				componentsSoFar.Add(maybeComponent);
			}

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
				if (each.transform != parent) {
					return each;
				}
			}
			return null;
		}

		// Recursively search up the transform tree through parents to find a DimensionObject
		public static PillarDimensionObject FindDimensionObjectRecursively(Transform go) {
			PillarDimensionObject dimensionObj = go.GetComponent<PillarDimensionObject>();
			Transform parent = go.parent;
			if (dimensionObj != null) {
				return dimensionObj;
			}
			else if (parent != null) {
				return FindDimensionObjectRecursively(parent);
			}
			else return null;
		}

		public static Transform[] GetChildren(this Transform parent) {
			return parent.GetComponentsInChildrenOnly<Transform>();
		}

		public static bool IsVisibleFrom(this Renderer r, Camera camera) {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, r.bounds);
        }

		public static void SetColorForRenderer(Renderer r, Color color, string colorPropertyName = "_Color") {
			MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
			r.GetPropertyBlock(propBlock);
			propBlock.SetColor(colorPropertyName, color);
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

	public class PolarCoordinate {
		public float radius;
		public Angle angle;
		public float y;

		public PolarCoordinate(float newRadius, Vector3 cartesianPoint) {
			radius = newRadius;
			angle = Angle.Radians((Mathf.Atan2(cartesianPoint.z, cartesianPoint.x) + (2 * Mathf.PI)) % (2 * Mathf.PI));
			y = cartesianPoint.y;
		}

		public PolarCoordinate(float newRadius, Angle newAngle) {
			radius = newRadius;
			angle = newAngle;
		}

		public override string ToString() {
			return "(" + radius.ToString("0.####") + ", " + (angle.radians / Mathf.PI).ToString("0.####") + "π rads)\t(" + y + " y-value)";
		}

		/// <summary>
		/// Creates a Vector3 from a PolarCoordinate, restoring the Y value if it was created with a Vector3
		/// </summary>
		/// <returns>A Vector3 containing the PolarToCartesian transformation for the X and Z values, and the restored Y value</returns>
		public Vector3 PolarToCartesian() {
			return new Vector3(radius * Mathf.Cos(angle.radians), y, radius * Mathf.Sin(angle.radians));
		}

		/// <summary>
		/// Creates a PolarCoordinate from a Vector3's X and Z values, and retains the Y value for converting back later
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
		public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color) {
			origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
			DrawBox(origin, halfExtents, orientation, color);
		}

		//Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
		public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color) {
			direction.Normalize();
			Box bottomBox = new Box(origin, halfExtents, orientation);
			Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

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

		public struct Box {
			public Vector3 localFrontTopLeft { get; private set; }
			public Vector3 localFrontTopRight { get; private set; }
			public Vector3 localFrontBottomLeft { get; private set; }
			public Vector3 localFrontBottomRight { get; private set; }
			public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
			public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
			public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
			public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

			public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
			public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
			public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
			public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
			public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
			public Vector3 backTopRight { get { return localBackTopRight + origin; } }
			public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
			public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

			public Vector3 origin { get; private set; }

			public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents) {
				Rotate(orientation);
			}
			public Box(Vector3 origin, Vector3 halfExtents) {
				this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
				this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
				this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
				this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

				this.origin = origin;
			}


			public void Rotate(Quaternion orientation) {
				localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
				localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
				localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
				localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
			}
		}

		//This should work for all cast types
		public static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance) {
			return origin + (direction.normalized * hitInfoDistance);
		}

		static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
			Vector3 direction = point - pivot;
			return pivot + rotation * direction;
		}
	}

	[Serializable]
	public class Angle {
		public enum Quadrant {
			I,		// [0 - 90)
			II,     // [90 - 180)
			III,    // [180 - 270)
			IV      // [270 - 360)
		}

		public float radians;
		public float degrees {
			get { return Mathf.Rad2Deg * radians; }
			set { radians = Mathf.Deg2Rad * value; }
		}
		public Quadrant quadrant {
			get {
				float normalizedDegrees = degrees;
				while (normalizedDegrees < 0) normalizedDegrees += 360;
				while (normalizedDegrees >= 360) normalizedDegrees -= 360;
				return (Quadrant)((int)normalizedDegrees / 90); }
		}

		public override string ToString() {
			return degrees + "°";
		}


		// Constructors
		private Angle(float _radians) {
			radians = _radians;
		}
		private Angle(Angle other) {
			radians = other.radians;
		}

		public static Angle Radians(float radians) {
			return new Angle(radians);
		}

		public static Angle Degrees(float degrees) {
			return new Angle(Mathf.Deg2Rad * degrees);
		}

		/// <summary>
		/// Gives the same normalized angle on the opposite quadrant of the circle
		/// </summary>
		/// <returns>This angle after being added and normalized</returns>
		public Angle Reverse() {
			this.radians += Mathf.PI;
			return Normalize();
		}

		/// <summary>
		/// Gives the same normalized angle on the opposite quadrant of the circle
		/// </summary>
		/// <returns>A new Angle after being added and normalized (does not modify the original Angle)</returns>
		public Angle reversed {
			get { return new Angle(this).Reverse(); }
		}

		/// <summary>
		/// Normalizes an angle to [0-2*PI) range
		/// </summary>
		/// <returns>This angle after being modified</returns>
		public Angle Normalize() {
			return Normalize(Radians(0), Radians(2 * Mathf.PI));
		}

		/// <summary>
		/// Gives the normalized angle within the [0-2*PI) range
		/// </summary>
		/// <returns>A new angle that is normalized (does not modify the original Angle)</returns>
		public Angle normalized {
			get { return new Angle(this).Normalize(); }
		}

		/// <summary>
		/// Normalizes an angle to [startRange, endRange) range
		/// </summary>
		/// <param name="startRange">Start of the range to normalize to (inclusive)</param>
		/// <param name="endRange">End of the range to normalize to (exclusive)</param>
		/// <returns>This angle after being modified</returns>
		public Angle Normalize(Angle startRange, Angle endRange) {
			Angle width = endRange - startRange;
			Angle offsetValue = this - startRange; // value relative to startRange

			this.radians = offsetValue.radians - Mathf.Floor(offsetValue.radians / width.radians) * width.radians + startRange.radians;
			if (this == endRange) {
				this.radians = startRange.radians;
			}
			return this;
		}
		
		/// <summary>
		/// Calculates the smallest angle possible between this angle and the given angle
		/// </summary>
		/// <param name="other"></param>
		/// <returns>A new Angle containing the angle between this angle and the input</returns>
		public Angle AngleBetween(Angle other) {
			return AngleBetween(this, other);
		}

		/// <summary>
		/// Calculates the smallest angle possible between the given angles
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
		/// Determines whether movement from a to b is clockwise or counter-clockwise.
		/// Note that clockwise movement generally corresponds to the Angle value increasing.
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
		/// Calculates the difference between this angle and another angle.
		/// Assumes that any difference over 180° is to be wrapped back around to a value < 180°
		/// </summary>
		/// <param name="other"></param>
		/// <returns>Difference between two angles, with magnitude < 180°</returns>
		public Angle WrappedAngleDiff(Angle other) {
			return WrappedAngleDiff(this, other);
		}

		/// <summary>
		/// Calculates the difference between the given angles.
		/// Assumes that any difference over 180° is to be wrapped back around to a value < 180°
		/// </summary>
		/// <param name="other"></param>
		/// <returns>Difference between two angles, with magnitude < 180°</returns>
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
		/// Determines whether the test angle falls between angles a and b (clockwise from a to b)
		/// </summary>
		/// <param name="test"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>True if test falls between a and b (clockwise), false otherwise</returns>
		public static bool IsAngleBetween(Angle test, Angle a, Angle b) {
			float testNormalized = test.normalized.radians;
			float aNormalized = a.normalized.radians;
			float bNormalized = b.normalized.radians;

			if (aNormalized < bNormalized) {
				return aNormalized <= testNormalized && testNormalized <= bNormalized;
			}
			else {
				return aNormalized <= testNormalized || testNormalized <= bNormalized;
			}
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

		public static Angle D0 = new Angle(0);
		public static Angle D90 = new Angle(Mathf.PI * 0.5f);
		public static Angle D180 = new Angle(Mathf.PI);
		public static Angle D270 = new Angle(Mathf.PI * 1.5f);
		public static Angle D360 = new Angle(Mathf.PI * 2);
	}

    public class DebugLogger {
        public bool enabled;
        private UnityEngine.Object context;
        
        public DebugLogger(UnityEngine.Object context, bool enabled) {
            this.enabled = enabled;
            this.context = context;
        }

        public void Log(object message) {
            if (enabled) Debug.Log(message, context);
        }

        public void LogWarning(object message) {
            if (enabled) Debug.LogWarning(message, context);
        }

        public void LogError(object message) {
            if (enabled) Debug.LogError(message, context);
        }
    }

	namespace ShaderUtils {
		public enum BlendMode {
			Opaque,
			Cutout,
			Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
			Transparent, // Physically plausible transparency mode, implemented as alpha pre-multiply
			TransparentWithBorder // Same as above but renders to a different renderType
		}

		public static class ShaderUtils {


			public enum ShaderPropertyType {
				Color,
				Vector,
				Float,
				Range,
				Texture
			}
			private static readonly Dictionary<string, ShaderPropertyType> knownShaderProperties = new Dictionary<string, ShaderPropertyType> {
				{ "_Color", ShaderPropertyType.Color },
				{ "_EmissionColor", ShaderPropertyType.Color },
				{ "_Dimension", ShaderPropertyType.Float },
				{ "_MainTex", ShaderPropertyType.Texture },
				{ "_Cutoff", ShaderPropertyType.Range },
				{ "_Glossiness", ShaderPropertyType.Range },
				{ "_GlossMapScale", ShaderPropertyType.Range },
				{ "_SmoothnessTextureChannel", ShaderPropertyType.Float },
				{ "_SpecColor", ShaderPropertyType.Color },
				{ "_SpecGlossMap", ShaderPropertyType.Texture },
				{ "_SpecularHighlights", ShaderPropertyType.Float },
				{ "_GlossyReflections", ShaderPropertyType.Float },
				{ "_BumpScale", ShaderPropertyType.Float },
				{ "_BumpMap", ShaderPropertyType.Texture },
				{ "_Parallax", ShaderPropertyType.Range },
				{ "_ParallaxMap", ShaderPropertyType.Texture },
				{ "_OcclusionStrength", ShaderPropertyType.Range },
				{ "_OcclusionMap", ShaderPropertyType.Texture },
				{ "_EmissionMap", ShaderPropertyType.Texture },
				{ "_DetailMask", ShaderPropertyType.Texture },
				{ "_DetailAlbedoMap", ShaderPropertyType.Texture },
				{ "_DetailNormalMapScale", ShaderPropertyType.Float },
				{ "_DetailNormalMap", ShaderPropertyType.Texture },
				{ "_Metallic", ShaderPropertyType.Range },
				// Dissolve shader properties
				{ "_Color2", ShaderPropertyType.Texture },
				{ "_DissolveValue", ShaderPropertyType.Range },
				{ "_BurnSize", ShaderPropertyType.Range },
				{ "_BurnRamp", ShaderPropertyType.Texture },
				{ "_BurnColor", ShaderPropertyType.Color },
				{ "_EmissionAmount", ShaderPropertyType.Float },
				// Water shader properties
				{ "_FlowMap", ShaderPropertyType.Texture },
				{ "_DerivHeightMap", ShaderPropertyType.Texture },
				{ "_HeightScale", ShaderPropertyType.Float },
				{ "_HeightScaleModulated", ShaderPropertyType.Float },
				{ "_Tiling", ShaderPropertyType.Float },
				{ "_Speed", ShaderPropertyType.Float },
				{ "_FlowStrength", ShaderPropertyType.Float },
				{ "_FlowOffset", ShaderPropertyType.Range },
				{ "_WaterFogColor", ShaderPropertyType.Color },
				{ "_WaterFogDensity", ShaderPropertyType.Range },
				{ "_RefractionStrength", ShaderPropertyType.Range }
			};

			public static void CopyMatchingPropertiesFromMaterial(this Material copyInto, Material copyFrom) {
				foreach (var propertyName in knownShaderProperties.Keys) {
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
				var oldKeywords = copyInto.shaderKeywords;
				copyInto.shaderKeywords = copyFrom.shaderKeywords;
				if (copyInto.shader.name == "Custom/DimensionShaders/DimensionObjectSpecular" || copyInto.shader.name == "Custom/DimensionShaders/InverseDimensionObjectSpecular") {
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
						material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
						material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
						material.SetInt("_ZWrite", 1);
						material.DisableKeyword("_ALPHATEST_ON");
						material.DisableKeyword("_ALPHABLEND_ON");
						material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
						material.renderQueue = -1;
						break;
					case BlendMode.Cutout:
						material.SetOverrideTag("RenderType", "TransparentCutout");
						material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
						material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
						material.SetInt("_ZWrite", 1);
						material.EnableKeyword("_ALPHATEST_ON");
						material.DisableKeyword("_ALPHABLEND_ON");
						material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
						material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
						break;
					case BlendMode.Fade:
						material.SetOverrideTag("RenderType", "Transparent");
						material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
						material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
						material.SetInt("_ZWrite", 0);
						material.DisableKeyword("_ALPHATEST_ON");
						material.EnableKeyword("_ALPHABLEND_ON");
						material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
						material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
						break;
					case BlendMode.Transparent:
						material.SetOverrideTag("RenderType", "Transparent");
						material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
						material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
						material.SetInt("_ZWrite", 0);
						material.DisableKeyword("_ALPHATEST_ON");
						material.DisableKeyword("_ALPHABLEND_ON");
						material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
						material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
						break;
					case BlendMode.TransparentWithBorder:
						material.SetOverrideTag("RenderType", "TransparentWithBorder");
						material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
						material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
						material.SetInt("_ZWrite", 0);
						material.DisableKeyword("_ALPHATEST_ON");
						material.DisableKeyword("_ALPHABLEND_ON");
						material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
						material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
						break;
				}
			}
		}
	}
}