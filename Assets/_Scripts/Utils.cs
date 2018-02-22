using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpitaphUtils {
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
}