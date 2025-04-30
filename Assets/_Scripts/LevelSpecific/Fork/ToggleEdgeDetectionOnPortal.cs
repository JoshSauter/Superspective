using System;
using MagicTriggerMechanics;
using PortalMechanics;
using Saving;

namespace LevelSpecific.Fork {
	public class ToggleEdgeDetectionOnPortal : SuperspectiveObject<ToggleEdgeDetectionOnPortal, ToggleEdgeDetectionOnPortal.ToggleEdgeDetectionOnPortalSave> {
		public Portal inPortal;
		public Portal outPortal;
		public MagicTrigger trigger;

		EDColors inPortalEdgeColors;
		EDColors outPortalEdgeColors;

		public bool portalEdgesAreWhite {
			get {
				switch (inPortal.edgeColorMode) {
					case BladeEdgeDetection.EdgeColorMode.SimpleColor:
						return inPortal.edgeColor.grayscale > .5f;
					case BladeEdgeDetection.EdgeColorMode.Gradient:
						return inPortal.edgeColorGradient.Evaluate(0).grayscale > .5f;
					case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
						return false;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		protected override void Start() {
			base.Start();
			
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
		
		public override void LoadSave(ToggleEdgeDetectionOnPortalSave save) { }
		
		[Serializable]
		public class ToggleEdgeDetectionOnPortalSave : SaveObject<ToggleEdgeDetectionOnPortal> {
			public ToggleEdgeDetectionOnPortalSave(ToggleEdgeDetectionOnPortal script) : base(script) { }
		}

	}
}