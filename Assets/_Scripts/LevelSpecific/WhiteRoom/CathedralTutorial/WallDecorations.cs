using System;
using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class WallDecorations : MonoBehaviour {
        private ObjectHover[] children;

        private const float minPeriod = 2f;
        private const float maxPeriod = 10f;
        
        private const float minOffset = 0f;
        private const float maxOffset = 3.14f;
        
        private const float minAmplitude = 0.2f;
        private const float maxAmplitude = 0.5f;
    
        private void OnValidate() {
            children = transform.GetComponentsInChildren<ObjectHover>();
        }

        // Start is called before the first frame update
        void Start() {
            children = transform.GetComponentsInChildren<ObjectHover>();
            foreach (var childHover in children) {
                childHover.period = Random.Range(minPeriod, maxPeriod);
                childHover.periodOffset = Random.Range(minOffset, maxOffset);
                childHover.zAmplitude = Random.Range(minAmplitude, maxAmplitude);
            }
        }

        // Update is called once per frame
        void Update() {
        
        }
    }
}
