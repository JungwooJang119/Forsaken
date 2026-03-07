using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void StartGame()
    {
        SaveManager.DeleteData();
        SceneManager.LoadScene("Level1.1");
    }

    public void ContinueGame()
    {
        SaveData saveData = SaveManager.Load();
        int sceneIndex = saveData.currentSceneIndex;
        SceneManager.LoadScene(sceneIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting");
        //for debug ^^
    }
}
