using System;
using System.Collections.Generic;
using PortalMechanics;
using SerializableClasses;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    // TODO: Rename to TogglePortalRendering to be consistent with naming conventions that the other actions use
    public class EnableDisablePortalRendering : TriggerAction {
        public bool logicAndRendering = false;
        public SuperspectiveReference<Portal, Portal.PortalSave>[] portalsToEnable;
        public SuperspectiveReference<Portal, Portal.PortalSave>[] portalsToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            PortalRenderingToggle(portalsToEnable, false);
            PortalRenderingToggle(portalsToDisable, true);
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            PortalRenderingToggle(portalsToEnable, true);
            PortalRenderingToggle(portalsToDisable, false);
        }
        
        private void PortalRenderingToggle(IEnumerable<SuperspectiveReference<Portal, Portal.PortalSave>> portals, bool pauseRendering) {
            // TODO: Make this just use the enums instead of booleans
            PortalRenderMode renderMode = pauseRendering ? PortalRenderMode.Invisible : PortalRenderMode.Normal;
            PortalPhysicsMode physicsMode = pauseRendering ? PortalPhysicsMode.None : PortalPhysicsMode.Normal;
            foreach (var portal in portals) {
                portal.Reference.MatchAction(
                loadedPortal => {
                        loadedPortal.RenderMode = renderMode;
                        if (logicAndRendering) {
                            loadedPortal.PhysicsMode = physicsMode;
                        }
                },
                unloadedPortal => {
                    unloadedPortal.renderMode = renderMode;
                    if (logicAndRendering) {
                        unloadedPortal.physicsMode = physicsMode;
                    }
                });
            }
        }
    }
}
