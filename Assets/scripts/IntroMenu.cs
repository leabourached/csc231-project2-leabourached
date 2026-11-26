
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroMenu : MonoBehaviour
{
    [Header("Menu Screens")]
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject gameInfoScreen;

    [Header("Gameplay Scene")]
    [SerializeField] private string levelSceneName = "level1";

    private void Start()
    {
        Time.timeScale = 1f;
        ShowMainMenu();
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;

        ShrineCheckpoint.ResetProgress();

        SceneManager.LoadScene(levelSceneName);
    }

    public void ShowGameInfo()
    {
        titleScreen.SetActive(false);
        gameInfoScreen.SetActive(true);
    }

    public void ShowMainMenu()
    {
        gameInfoScreen.SetActive(false);
        titleScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Game closed.");

        Application.Quit();
    }
}
