using System;
using System.Collections.Generic;
using MagicTriggerMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Serialization;

namespace TheEntity {
    public class TheEntity_LocusMaximus : SuperspectiveObject<TheEntity_LocusMaximus, TheEntity_LocusMaximus.TheEntity_LocusMaximusSave> {
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
            [FormerlySerializedAs("invisibleObject")]
            public Transform normalGeometry;
        }
        public List<PotentialLocation> potentialLocations;
        
        public Transform eyeTransform;
        private Transform NormalGeometryAtLocation => Location.normalGeometry;

        public Renderer[] eyeRenderers;

        [SerializeField]
        private MagicTrigger lookAtEyeTrigger;
        [SerializeField]
        private MagicTrigger lookAwayFromEyeTrigger;

        private Coroutine blinkCoroutine;

        protected override void Awake() {
            base.Awake();

            eyeRenderers = eyeTransform.GetComponentsInChildren<Renderer>();
            
            state = this.StateMachine(EyeState.Unnoticed);
            UpdateLocation();
        }

        protected override void Start() {
            base.Start();
            
            state.AddTrigger(EyeState.Despawned, () => UpdateLocation());
            state.AddTrigger(EyeState.Noticed, () => {
                if (blinkCoroutine != null) {
                    StopCoroutine(blinkCoroutine);
                }

                StartCoroutine(TheEntity.BlinkController(this, eyeTransform, () => .25f));
            });
            
            NormalGeometryAtLocation.gameObject.SetActive(false);
            eyeTransform.SetParent(Location.ornamentRoot, false);
            UpdateLocation(locationIndex);
            blinkCoroutine = StartCoroutine(TheEntity.BlinkController(this, eyeTransform, () => 1f));
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
            lookAtEyeTrigger.OnMagicTriggerStayOneTime += SetToNoticed;
            lookAwayFromEyeTrigger.OnMagicTriggerStayOneTime += SetToDespawned;
        }

        private void UnsubscribeEvents() {
            lookAtEyeTrigger.OnMagicTriggerStayOneTime -= SetToNoticed;
            lookAwayFromEyeTrigger.OnMagicTriggerStayOneTime -= SetToDespawned;
        }

        private void UpdateLocation(int locationIndex = -1) {
            if (state == EyeState.Despawned) {
                locationIndex = -1;
            }
            
            for (int i = 0; i < potentialLocations.Count; i++) {
                potentialLocations[i].normalGeometry.gameObject.SetActive(i != locationIndex);
            }

            bool eyeIsVisible = locationIndex >= 0;
            foreach (var r in eyeRenderers) {
                r.enabled = eyeIsVisible;
            }
        }

        private void SetToNoticed() => state.Set(EyeState.Noticed);
        private void SetToDespawned() => state.Set(EyeState.Despawned);
        
#region Saving

        public override void LoadSave(TheEntity_LocusMaximusSave save) {
            UpdateLocation(locationIndex);
        }

        [Serializable]
        public class TheEntity_LocusMaximusSave : SaveObject<TheEntity_LocusMaximus> {
            public TheEntity_LocusMaximusSave(TheEntity_LocusMaximus script) : base(script) { }
        }
#endregion
    }
}
