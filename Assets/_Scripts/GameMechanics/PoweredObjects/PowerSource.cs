using PowerTrailMechanics;
using UnityEngine.Events;

namespace PoweredObjects {
    public interface PowerSource {
        public delegate void PowerSourceAction();
        public delegate void PowerSourceRefAction(PowerSource powerSource);
        
        public event PowerSourceAction OnPowerBegin;
        public event PowerSourceAction OnPowerFinish;
        public event PowerSourceAction OnDepowerBegin;
        public event PowerSourceAction OnDepowerFinish;
        public event PowerSourceRefAction OnPowerBeginRef;
        public event PowerSourceRefAction OnPowerFinishRef;
        public event PowerSourceRefAction OnDepowerBeginRef;
        public event PowerSourceRefAction OnDepowerFinishRef;
        
        bool PowerIsOn { get; }
        bool IsFullyPowered { get; }
        bool IsFullyDepowered { get; }

        void Power();
        void Depower();
    }
}
