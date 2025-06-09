using System;
using Saving;

public class SuperspectiveTime : SingletonSuperspectiveObject<SuperspectiveTime, SuperspectiveTime.Save> {
    public static float Time => instance.time;
    private float time;

    public static int FrameCount => instance.frameCount;
    private int frameCount;

    public void Update() {
        time += UnityEngine.Time.deltaTime;
        frameCount++;
    }

    public override void LoadSave(Save save) { }
    
    [Serializable]
    public class Save : SaveObject<SuperspectiveTime> {
        public Save(SuperspectiveTime saveableObject) : base(saveableObject) { }
    }
}
