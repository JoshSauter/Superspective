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
		public float angle;  //In Radians
		public float y;

		public PolarCoordinate(float newRadius, Vector3 cartesianPoint) {
			radius = newRadius;
			angle = Mathf.Atan2(cartesianPoint.z, cartesianPoint.x);
			y = cartesianPoint.y;
		}

		public PolarCoordinate(float newRadius, float newAngle) {
			radius = newRadius;
			angle = newAngle;
		}

		public override string ToString() {
			return "(" + radius.ToString("0.####") + ", " + (angle / Mathf.PI).ToString("0.####") + "*PI rads)\t(" + y + " y-value)";
		}

		/// <summary>
		/// Creates a Vector3 from a PolarCoordinate, restoring the Y value if it was created with a Vector3
		/// </summary>
		/// <returns>A Vector3 containing the PolarToCartesian transformation for the X and Z values, and the restored Y value</returns>
		public Vector3 PolarToCartesian() {
			return new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle));
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
}