using SuperspectiveUtils;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.TransitionWhiteRoom_Fork {
	public class GenerateRotatedSquaresStructure : MonoBehaviour {
		public GameObject copy;
		public float rotationAngle = 15f;
		public float startScale = 15f;
		public int iterations = 15;
		public float heightOfEachStep = 0.5f;

		[Button("Generate structure")]
		void GenerateStructure() {
			DestroyChildren();
			Vector3 startPos = transform.position;
			float scaleFactor = 1.025f / (Mathf.Sin(rotationAngle * Mathf.Deg2Rad) + Mathf.Cos(rotationAngle * Mathf.Deg2Rad));
			float scale = startScale * scaleFactor;
			float startRotation = 0f;

			for (int i = 1; i < iterations; i++) {
				Vector3 curPos = startPos - i * heightOfEachStep * transform.up;
				float curRotation = startRotation + i * rotationAngle;
				scale *= scaleFactor;

				Debug.LogError(scale);

				GameObject go = Instantiate(copy);
				go.transform.SetParent(transform);
				go.transform.position = curPos;
				go.transform.rotation = Quaternion.Euler(0, curRotation, 0);
				go.transform.localScale = new Vector3(scale, heightOfEachStep, scale);
			}
		}

		[Button("Destroy all children")]
		void DestroyChildren() {
			Transform[] children = transform.GetChildren();
			foreach (Transform child in children) {
				if (child.gameObject != copy) {
					DestroyImmediate(child.gameObject);
				}
			}
		}
	}
}