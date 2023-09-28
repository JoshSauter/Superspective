using PowerTrailMechanics;
using StateUtils;
using UnityEngine.Events;

namespace PoweredObjects {
    public interface PoweredObject {
        public delegate void PowerSourceAction();
        public delegate void PowerSourceRefAction(PoweredObject poweredObject);
        
        public event PowerSourceAction OnPowerBegin;
        public event PowerSourceAction OnPowerFinish;
        public event PowerSourceAction OnDepowerBegin;
        public event PowerSourceAction OnDepowerFinish;
        public event PowerSourceRefAction OnPowerBeginRef;
        public event PowerSourceRefAction OnPowerFinishRef;
        public event PowerSourceRefAction OnDepowerBeginRef;
        public event PowerSourceRefAction OnDepowerFinishRef;

        public StateMachine<PowerState> PowerStateMachine { get; }
        
        bool PowerIsOn { get; set; }
        bool IsFullyPowered => this.PowerStateMachine.state == PowerState.Powered;
        bool IsFullyDepowered => this.PowerStateMachine.state == PowerState.Depowered;
    }
}
