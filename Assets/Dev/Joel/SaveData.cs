using System;

[Serializable]
public class SaveData {
    public int currentStage = 1;
    public bool shootUnlocked = false;
    public bool canDash = false;
    public string lastSaveSpotID = "";
}