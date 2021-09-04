using Saving;
using UnityEngine;

namespace LevelManagement {
    public class LevelRoot : SaveableObject {
        public override string ID => $"LevelRoot_{gameObject.scene.name}";
    }
}