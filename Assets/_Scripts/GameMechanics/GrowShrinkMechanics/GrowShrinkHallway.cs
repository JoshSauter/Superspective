using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;

namespace GrowShrink {
    [RequireComponent(typeof(UniqueId))]
    public class GrowShrinkHallway : SaveableObject<GrowShrinkHallway, GrowShrinkHallway.GrowShrinkHallwayNewSave> {
        public float scaleFactor = 4;

        public GrowShrinkTransitionTrigger triggerZone;

        // Objects that are or were in the tunnel at some point
        private Dictionary<string, SerializableReference<GrowShrinkObject>> growShrinkObjects = new Dictionary<string, SerializableReference<GrowShrinkObject>>();

        string GetId(Collider c) => GrowShrinkTransitionTrigger.GetId(c);

        protected override void Start() {
            base.Start();

            triggerZone.OnTransitionTrigger += SetLerpValue;
            triggerZone.OnHallwayEnter += ObjectEnter;
            triggerZone.OnHallwayExit += ObjectExit;
        }

        private void ObjectEnter(Collider c, bool enteredSmallSide) {
            string id = GetId(c);
            if (!growShrinkObjects.ContainsKey(id)) {
                GrowShrinkObject growShrinkObj = c.GetComponent<GrowShrinkObject>();
                if (growShrinkObj == null) return;
                growShrinkObjects[id] = growShrinkObj;
            }

            growShrinkObjects[id].GetOrNull()?.EnteredHallway(this, enteredSmallSide);
        }

        private void ObjectExit(Collider c, bool exitededSmallSide) {
            string id = GetId(c);
            if (!growShrinkObjects.ContainsKey(id)) return;

            growShrinkObjects[id].GetOrNull()?.ExitedHallway(this, exitededSmallSide);
        }

        private void SetLerpValue(Collider c, float t) {
            string id = GetId(c);
            if (!growShrinkObjects.ContainsKey(id)) {
                GrowShrinkObject growShrinkObj = c.GetComponent<GrowShrinkObject>();
                if (growShrinkObj == null) return;
                growShrinkObjects[id] = growShrinkObj;
            }

            debug.LogWarning($"t-value for {id}: {t:F2}");
            growShrinkObjects[id].GetOrNull()?.SetScale(this, t);
        }

#region Saving

        [Serializable]
        public class GrowShrinkHallwayNewSave : SerializableSaveObject<GrowShrinkHallway> {
            private float scaleFactor;
            private SerializableDictionary<string, SerializableReference<GrowShrinkObject>> growShrinkObjects;

            public GrowShrinkHallwayNewSave(GrowShrinkHallway script) : base(script) {
                scaleFactor = script.scaleFactor;
                growShrinkObjects = script.growShrinkObjects;
            }

            public override void LoadSave(GrowShrinkHallway script) {
                script.growShrinkObjects = this.growShrinkObjects;
                script.scaleFactor = this.scaleFactor;
            }
        }

#endregion
    }
}