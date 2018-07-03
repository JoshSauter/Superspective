using System.Collections;
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
	
	public class Utils {
		public static T[] GetComponentsInChildrenOnly<T>(Transform parent) where T : Component {
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
		public static Vector2 xy(Vector3 v3) {
			return new Vector2(v3.x, v3.y);
		}
		public static Vector2 xz(Vector3 v3) {
			return new Vector2(v3.x, v3.z);
		}
		public static Vector2 yz(Vector3 v3) {
			return new Vector2(v3.y, v3.z);
		}

		// Subvectors of Vector4
		public static Vector2 xy(Vector4 v4) {
			return new Vector2(v4.x, v4.y);
		}
		public static Vector2 xz(Vector4 v4) {
			return new Vector2(v4.x, v4.z);
		}
		public static Vector2 xw(Vector4 v4) {
			return new Vector2(v4.x, v4.w);
		}
		public static Vector2 yz(Vector4 v4) {
			return new Vector2(v4.y, v4.z);
		}
		public static Vector2 yw(Vector4 v4) {
			return new Vector2(v4.y, v4.w);
		}
		public static Vector2 zw(Vector4 v4) {
			return new Vector2(v4.z, v4.w);
		}

		public static Vector3 xyz(Vector4 v4) {
			return new Vector3(v4.x, v4.y, v4.z);
		}
		public static Vector3 xyw(Vector4 v4) {
			return new Vector3(v4.x, v4.y, v4.w);
		}
		public static Vector3 xzw(Vector4 v4) {
			return new Vector3(v4.x, v4.z, v4.w);
		}
		public static Vector3 yzw(Vector4 v4) {
			return new Vector3(v4.y, v4.z, v4.w);
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
			return "(" + radius.ToString("0.####") + ", " + (angle.radians / Mathf.PI).ToString("0.####") + "*PI rads)\t(" + y + " y-value)";
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

	[Serializable]
	public class Angle {
		public float radians;
		public float degrees {
			get { return Mathf.Rad2Deg * radians; }
			set { radians = Mathf.Deg2Rad * value; }
		}

		public override string ToString() {
			return degrees + "°";
		}

		// Constructors
		private Angle(float _radians) {
			radians = _radians;
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
		/// Normalizes an angle to [0-2*PI) range
		/// </summary>
		/// <returns>This angle after being modified</returns>
		public Angle Normalize() {
			return Normalize(Radians(0), Radians(2 * Mathf.PI));
		}

		/// <summary>
		/// Normalizes an angle to [startRange, endRange) range
		/// </summary>
		/// <param name="startRange">Start of the range to normalize to</param>
		/// <param name="endRange">End of the range to normalize to</param>
		/// <returns>This angle after being modified</returns>
		public Angle Normalize(Angle startRange, Angle endRange) {
			Angle width = endRange - startRange;
			Angle offsetValue = this - startRange; // value relative to startRange

			this.radians = offsetValue.radians - Mathf.Floor(offsetValue.radians / width.radians) * width.radians + startRange.radians;
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
		/// Determines whether the test angle falls between angles a and b (counter-clockwise from a to b)
		/// </summary>
		/// <param name="test"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>True if test falls between a and b (counter-clockwise), false otherwise</returns>
		public static bool IsAngleBetween(Angle test, Angle a, Angle b) {
			float testNormalized = new Angle(test.radians).Normalize().radians;
			float aNormalized = new Angle(a.radians).Normalize().radians;
			float bNormalized = new Angle(b.radians).Normalize().radians;

			if (aNormalized < bNormalized) {
				return aNormalized <= testNormalized && testNormalized <= bNormalized;
			}
			else {
				return aNormalized <= testNormalized || testNormalized <= bNormalized;
			}
		}

		// Operators
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
		public static Angle operator *(Angle a, Angle b) {
			return new Angle(a.radians * b.radians);
		}
		public static bool operator <(Angle a, Angle b) {
			return (a - b).radians < 0;
		}
		public static bool operator >(Angle a, Angle b) {
			return (b - a).radians < 0;
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
	}
}