using MagicTriggerMechanics;
using PortalMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Fork {
	public class ToggleEdgeDetectionOnPortal : MonoBehaviour {
		public Portal inPortal;
		public Portal outPortal;
		public MagicTrigger trigger;

		EDColors inPortalEdgeColors;
		EDColors outPortalEdgeColors;

		void Start() {
			inPortalEdgeColors = new EDColors {
				edgeColorMode = inPortal.edgeColorMode,
				edgeColor = inPortal.edgeColor,
				edgeColorGradient = inPortal.edgeColorGradient,
				edgeColorGradientTexture = inPortal.edgeColorGradientTexture
			};
			outPortalEdgeColors = new EDColors {
				edgeColorMode = outPortal.edgeColorMode,
				edgeColor = outPortal.edgeColor,
				edgeColorGradient = outPortal.edgeColorGradient,
				edgeColorGradientTexture = outPortal.edgeColorGradientTexture
			};

			trigger.OnMagicTriggerStayOneTime += ResetInPortalEdgeDetection;
			trigger.OnNegativeMagicTriggerStayOneTime += ResetOutPortalEdgeDetection;
		}

		void ResetInPortalEdgeDetection() {
			Portal.CopyEdgeColors(from: inPortalEdgeColors, inPortal);
		}

		void ResetOutPortalEdgeDetection() {
			Portal.CopyEdgeColors(from: outPortalEdgeColors, outPortal);
		}
	}
}