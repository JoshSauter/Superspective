﻿using Saving;
using UnityEngine;

namespace LevelManagement {
    public class LevelRoot : SuperspectiveObject {
        public override string ID => $"LevelRoot_{gameObject.scene.name}";
    }
}