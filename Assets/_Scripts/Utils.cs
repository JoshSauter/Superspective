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
		
	}
}