using UnityEngine;
using System.IO;

public static class SaveManager {
    private static string savePath = Path.Combine(Application.persistentDataPath, "saveFile.json");

    public static void Save(SaveData data){
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("[SaveManager] File saved to: " + savePath);
        Debug.Log("[SaveManager] Save contents:\n" + json);
    }

    public static SaveData Load(){
        if (!File.Exists(savePath)){
            Debug.Log("[SaveManager] No save file found at: " + savePath + " — loading defaults");
            return new SaveData();
        }
        string json = File.ReadAllText(savePath);
        Debug.Log("[SaveManager] File loaded from: " + savePath);
        Debug.Log("[SaveManager] Load contents:\n" + json);
        return JsonUtility.FromJson<SaveData>(json);
    }
}