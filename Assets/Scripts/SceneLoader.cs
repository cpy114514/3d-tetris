using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    public void StartGame()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("no such scene");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("exit");
        Application.Quit();
    }
}