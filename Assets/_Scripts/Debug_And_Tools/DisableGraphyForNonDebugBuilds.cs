using System;
using UnityEngine;

namespace DebugAndTools {
    public class DisableGraphyForNonDebugBuilds : MonoBehaviour {
        public void Awake() {
            if (!Debug.isDebugBuild) gameObject.SetActive(false);
        }
    }
}
