﻿namespace StarlightDirector.App.Gaming {
    internal class LiveMusicRecord {

        public int LiveID { get; set; }
        public int MusicID { get; set; }
        public string MusicName { get; set; }
        public bool[] DifficultyExists { get; internal set; }
        public MusicAttribute Attribute { get; set; }

    }
}
