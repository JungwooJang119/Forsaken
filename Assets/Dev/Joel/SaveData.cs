using System;

[Serializable]
public class SaveData {
    public int currentSceneIndex = 1;
    public bool shootUnlocked = false;
    public bool canDash = false;
    public string lastSaveSpotID = "";
}