﻿using System;
using System.Collections.Generic;
using MagicTriggerMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace TheEntity {
    public class TheEntity_GrowShrink2 : SuperspectiveObject<TheEntity_GrowShrink2, TheEntity_GrowShrink2.TheEntity_GrowShrink2Save> {
        public enum EyeState : byte {
            Unnoticed,
            Noticed,
            Despawned
        }

        public StateMachine<EyeState> state;

        public PotentialLocation Location {
            get {
                if (locationIndex < 0) {
                    locationIndex = UnityEngine.Random.Range(0, potentialLocations.Count);
                }

                return potentialLocations[locationIndex];
            }
        }
        public int locationIndex = -1;

        [Serializable]
        public struct PotentialLocation {
            public Transform ornamentRoot;
            public Transform invisibleObject;
        }
        public List<PotentialLocation> potentialLocations;
        
        public Transform eyeTransform;
        private Transform InvisibleObject {
            get {
                if (locationIndex < 0) {
                    locationIndex = UnityEngine.Random.Range(0, potentialLocations.Count);
                }
                
                return potentialLocations[locationIndex].invisibleObject;
            }
        }

        private MagicTrigger lookAtEyeTrigger;
        private MagicTrigger lookAwayFromEyeTrigger;

        protected override void Awake() {
            base.Awake();

            state = this.StateMachine(EyeState.Unnoticed);
            RestoreAllInvisibleObjects();
        }

        protected override void Start() {
            base.Start();
            
            state.AddTrigger(EyeState.Despawned, () => RestoreAllInvisibleObjects());
            InvisibleObject.gameObject.SetActive(false);
            eyeTransform.SetParent(Location.ornamentRoot, false);
            RestoreAllInvisibleObjects(locationIndex);
            StartCoroutine(TheEntity.BlinkController(this, eyeTransform, () => state == EyeState.Noticed ? 0.5f : 1f));
        }
        
        protected override void OnEnable() {
            base.OnEnable();
            
            SubscribeEvents();
        }

        protected override void OnDisable() {
            base.OnDisable();
            UnsubscribeEvents();
        }

        private void SubscribeEvents() {
            lookAtEyeTrigger = eyeTransform.GetComponentInChildrenOnly<MagicTrigger>();
            lookAwayFromEyeTrigger = eyeTransform.GetComponent<GlobalMagicTrigger>();
            
            lookAtEyeTrigger.OnMagicTriggerStayOneTime += SetToNoticed;
            lookAwayFromEyeTrigger.OnMagicTriggerStayOneTime += SetToDespawned;
        }

        private void UnsubscribeEvents() {
            lookAtEyeTrigger.OnMagicTriggerStayOneTime -= SetToNoticed;
            lookAwayFromEyeTrigger.OnMagicTriggerStayOneTime -= SetToDespawned;
        }

        private void RestoreAllInvisibleObjects(int indexToSkip = -1) {
            for (int i = 0; i < potentialLocations.Count; i++) {
                if (i == indexToSkip) {
                    continue;
                }
                
                potentialLocations[i].invisibleObject.gameObject.SetActive(true);
            }
        }

        private void SetToNoticed() => state.Set(EyeState.Noticed);
        private void SetToDespawned() => state.Set(EyeState.Despawned);
        
#region Saving

        public override void LoadSave(TheEntity_GrowShrink2Save save) {
            state.LoadFromSave(save.state);
            locationIndex = save.locationIndex;
        }

        [Serializable]
        public class TheEntity_GrowShrink2Save : SaveObject<TheEntity_GrowShrink2> {
            public StateMachine<EyeState>.StateMachineSave state;
            public int locationIndex;

            public TheEntity_GrowShrink2Save(TheEntity_GrowShrink2 script) : base(script) {
                state = script.state.ToSave();
                locationIndex = script.locationIndex;
            }
        }
#endregion
    }
}
