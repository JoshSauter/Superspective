using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Level3 {
	public class LaserReceiver : MonoBehaviour {
		public static int numReceiversActivated = 0;
		public SideRoomPanel laserActivation;

		Renderer thisRenderer;

		public float colorLerpTime = 1.1f;

		#region events
		public delegate void LaserReceiverAction();
		public event LaserReceiverAction OnReceiverActivated;
		#endregion

		// Use this for initialization
		void Start() {
			thisRenderer = GetComponent<Renderer>();

			laserActivation.OnLaserActivateFinish += StartColorChange;
		}

		void StartColorChange() {
			StartCoroutine(ColorChange());
		}

		IEnumerator ColorChange() {
			MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
			thisRenderer.GetPropertyBlock(propBlock);
			Color startColor = propBlock.GetColor("_Color");
			Color targetColor = laserActivation.gemColor;

			float timeElapsed = 0;
			while (timeElapsed < colorLerpTime) {
				timeElapsed += Time.deltaTime;
				float t = timeElapsed / colorLerpTime;

				propBlock.SetColor("_Color", Color.Lerp(startColor, targetColor, t));
				thisRenderer.SetPropertyBlock(propBlock);

				yield return null;
			}
			propBlock.SetColor("_Color", targetColor);
			thisRenderer.SetPropertyBlock(propBlock);

			numReceiversActivated++;

			if (OnReceiverActivated != null) OnReceiverActivated();
		}
	}
}