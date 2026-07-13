using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{


    public void startgame()
    {
        SceneManager.LoadScene("Stage 1");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Restarting game...");
    }
    
    public void QuitGame()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
