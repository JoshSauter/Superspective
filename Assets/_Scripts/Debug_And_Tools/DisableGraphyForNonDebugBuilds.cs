using System;
using UnityEngine;

namespace DebugAndTools {
    public class DisableGraphyForNonDebugBuilds : MonoBehaviour {
        public void Start() {
            if (!Debug.isDebugBuild) gameObject.SetActive(false);
        }
    }
}
