using System;
using System.Collections.Generic;
using PortalMechanics;
using SerializableClasses;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class EnableDisablePortalRendering : TriggerAction {
        public bool logicAndRendering = false;
        public SerializableReference<Portal, Portal.PortalSave>[] portalsToEnable;
        public SerializableReference<Portal, Portal.PortalSave>[] portalsToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            PortalRenderingToggle(portalsToEnable, false);
            PortalRenderingToggle(portalsToDisable, true);
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            PortalRenderingToggle(portalsToEnable, true);
            PortalRenderingToggle(portalsToDisable, false);
        }
        
        private void PortalRenderingToggle(IEnumerable<SerializableReference<Portal, Portal.PortalSave>> portals, bool pauseRendering) {
            foreach (var portal in portals) {
                portal.Reference.MatchAction(
                loadedPortal => {
                        loadedPortal.pauseRendering = pauseRendering;
                        if (logicAndRendering) {
                            loadedPortal.pauseLogic = pauseRendering;
                        }
                },
                unloadedPortal => {
                    unloadedPortal.pauseRendering = pauseRendering;
                    if (logicAndRendering) {
                        unloadedPortal.pauseLogic = pauseRendering;
                    }
                });
            }
        }
    }
}
