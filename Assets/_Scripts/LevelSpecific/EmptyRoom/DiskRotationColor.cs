using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.EmptyRoom {
	public class DiskRotationColor : MonoBehaviour {
		public Gradient rotationGradient;
		Renderer _renderer;
		MaterialPropertyBlock propBlock;

		void Awake() {
			propBlock = new MaterialPropertyBlock();
			_renderer = GetComponent<Renderer>();
		}

		// Update is called once per frame
		void Update() {
			_renderer.GetPropertyBlock(propBlock);
			float t = 0.5f * Mathf.Sin(transform.rotation.eulerAngles.y / 360 * Mathf.PI) + 0.5f;
			propBlock.SetColor("_Color", rotationGradient.Evaluate(t));
			_renderer.SetPropertyBlock(propBlock);
		}
	}
}