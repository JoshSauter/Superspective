using System;
using System.Collections.Generic;
using PortalMechanics;
using SerializableClasses;
using Sirenix.OdinInspector;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class TogglePortalRendering : TriggerAction {
        [BoxGroup("Physics Settings")]
        [GUIColor(0.95f, 0.55f, .55f)]
        public bool affectsPhysics = false;
        [NaughtyAttributes.HorizontalLine]
        [BoxGroup("Physics Settings")]
        [GUIColor(0.95f, 0.55f, .55f)]
        [ShowIf(nameof(affectsPhysics))]
        public PortalPhysicsMode physicsModeEnabled = PortalPhysicsMode.Normal;
        [BoxGroup("Physics Settings")]
        [GUIColor(0.95f, 0.55f, .55f)]
        [ShowIf(nameof(affectsPhysics))]
        public PortalPhysicsMode physicsModeDisabled = PortalPhysicsMode.None;
        
        [BoxGroup("Rendering Settings")]
        [GUIColor(0.35f, 0.75f, .9f)]
        public bool affectsRendering = true;
        [NaughtyAttributes.HorizontalLine]
        [BoxGroup("Rendering Settings")]
        [GUIColor(0.35f, 0.75f, .9f)]
        [ShowIf(nameof(affectsRendering))]
        public PortalRenderMode renderModeEnabled = PortalRenderMode.Normal;
        [BoxGroup("Rendering Settings")]
        [GUIColor(0.35f, 0.75f, .9f)]
        [ShowIf(nameof(affectsRendering))]
        public PortalRenderMode renderModeDisabled = PortalRenderMode.Invisible;
        
        public SuperspectiveReference<Portal, Portal.PortalSave>[] portalsToEnable;
        public SuperspectiveReference<Portal, Portal.PortalSave>[] portalsToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            PortalModeToggle(portalsToEnable, physicsModeEnabled, renderModeEnabled);
            PortalModeToggle(portalsToDisable, physicsModeDisabled, renderModeDisabled);
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            PortalModeToggle(portalsToEnable, physicsModeDisabled, renderModeDisabled);
            PortalModeToggle(portalsToDisable, physicsModeEnabled, renderModeEnabled);
        }
        
        private void PortalModeToggle(
            IEnumerable<SuperspectiveReference<Portal, Portal.PortalSave>> portals,
            PortalPhysicsMode physicsMode,
            PortalRenderMode renderMode) {
            foreach (var portal in portals) {
                portal.Reference.MatchAction(
                    loadedPortal => {
                        if (affectsRendering) {
                            loadedPortal.RenderMode = renderMode;
                        }
                        if (affectsPhysics) {
                            loadedPortal.PhysicsMode = physicsMode;
                        }
                    },
                    unloadedPortal => {
                        if (affectsRendering) {
                            unloadedPortal.renderMode = renderMode;
                        }
                        if (affectsPhysics) {
                            unloadedPortal.physicsMode = physicsMode;
                        }
                    });
            }
        }
    }
}
