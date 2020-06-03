using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteRoom {
    public class WhiteRoomBlackWhiteFade : MonoBehaviour {
        StaircaseRotate staircaseRotate;
        public float fadeAngle = 45;

        IEnumerator Start() {
            yield return null;
            staircaseRotate = GetComponentInChildren<StaircaseRotate>();
            Shader.SetGlobalVector("_ZeroDegreesVector", -staircaseRotate.endGravityDirection);
            Shader.SetGlobalVector("_NinetyDegreesVector", -staircaseRotate.startGravityDirection);
            Shader.SetGlobalVector("_ColorChangeAxis", Vector3.Cross(staircaseRotate.startGravityDirection, staircaseRotate.endGravityDirection));
            Shader.SetGlobalVector("_ColorChangePoint", staircaseRotate.pivotPoint);
            Shader.SetGlobalFloat("_ColorChangeWidth", fadeAngle/90f);
        }
    }
}
