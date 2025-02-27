using System;
using System.Collections.Generic;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

namespace Deepblack {
    [RequireComponent(typeof(UniqueId))]
    public class DeepblackObject : SuperspectiveObject<DeepblackObject, DeepblackObject.DeepblackObjectSave> {
        [SerializeField]
        private float darkness = 10f;
        public float Darkness => darkness;
        
        public Renderer[] renderers;
        public readonly Dictionary<Renderer, Mesh> rendererMeshes = new Dictionary<Renderer, Mesh>();

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
            DeepblackEffect.instance.Register(this);
        }
        
        protected override void OnDisable() {
            base.OnDisable();
            DeepblackEffect.instance.Unregister(this);
        }

        [Serializable]
        public class DeepblackObjectSave : SaveObject<DeepblackObject> {
            public float darkness;
            
            public DeepblackObjectSave(DeepblackObject saveableObject) : base(saveableObject) {
                darkness = saveableObject.darkness;
            }
        }

        public override void LoadSave(DeepblackObjectSave save) {
            darkness = save.darkness;
        }
    }
}