using System;
using System.Collections.Generic;

namespace LevelManagement {
    [Serializable]
    public class Level {
        public Levels level;
        public List<Levels> connectedLevels;
    }
}