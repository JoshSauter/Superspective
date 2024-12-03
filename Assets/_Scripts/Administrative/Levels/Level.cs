using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelManagement {
    [Serializable]
    public class Level {
        public Levels level;
        [SerializeField]
        string _displayName;
        public string displayName => String.IsNullOrWhiteSpace(_displayName) ? level.ToString() : _displayName;
        public List<Levels> connectedLevels;
    }
}
