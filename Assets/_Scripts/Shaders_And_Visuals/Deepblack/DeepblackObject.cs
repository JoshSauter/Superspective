using System;
using System.Collections;
using System.Collections.Generic;
using Saving;
using SuperspectiveUtils;
using UnityEngine;
using SuperspectiveUtils;

namespace Deepblack {
    [RequireComponent(typeof(UniqueId))]
    public class DeepblackObject : SuperspectiveObject<DeepblackObject, DeepblackObject.DeepblackObjectSave> {
        public float darkness = 1.5f;

        public float falloffFactor = 0.025f;
        
        public Renderer[] renderers;
        public readonly NullSafeDictionary<Renderer, Mesh> rendererMeshes = new NullSafeDictionary<Renderer, Mesh>();

        protected override void Awake() {
            base.Awake();
            
            if (renderers == null || renderers.Length == 0) {
                renderers = GetComponentsInChildren<Renderer>();
            }

            foreach (Renderer r in renderers) {
                rendererMeshes.Add(r, r.GetMesh());
            }
        }
        
        protected override void OnEnable() {
            base.OnEnable();

            StartCoroutine(Co_OnEnable());
        }

        private IEnumerator Co_OnEnable() {
            var wait = new WaitUntil(() => GameManager.instance.gameHasLoaded);
            yield return wait;
            DeepblackEffect.instance.Register(this);
        }
        
        protected override void OnDisable() {
            base.OnDisable();
            if (DeepblackEffect.instance == null) return;
            DeepblackEffect.instance.Unregister(this);
        }

        [Serializable]
        public class DeepblackObjectSave : SaveObject<DeepblackObject> {
            public DeepblackObjectSave(DeepblackObject saveableObject) : base(saveableObject) { }
        }

        public override void LoadSave(DeepblackObjectSave save) { }
    }
}