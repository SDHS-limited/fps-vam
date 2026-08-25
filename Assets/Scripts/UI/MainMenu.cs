using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [Header("UI 설정")]
    [SerializeField] private GameObject mainMenuPanel; // 메인 메뉴 패널
    [SerializeField] private GameObject SettingMenuPanel; // 메인 메뉴 패널
    [SerializeField] private GameObject audioSettingsPanel; // 오디오 설정 패널
    [SerializeField] private GameObject videoSettingsPanel; // 비디오 설정 패널
    [SerializeField] private GameObject controlsSettingsPanel; // 컨트롤 설정 패널
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
        SceneManager.LoadScene("MainTitle");
    }

    public void SettingAudio()
    {
        audioSettingsPanel.SetActive(true);
        SettingMenuPanel.SetActive(false);
    }
    public void SettingVideo()
    {
        videoSettingsPanel.SetActive(true);
        SettingMenuPanel.SetActive(false);
    }
    public void SettingControls()
    {
        controlsSettingsPanel.SetActive(true);
        SettingMenuPanel.SetActive(false);
    }
    

    public void CloseSettings()
    {
        audioSettingsPanel.SetActive(false);
        videoSettingsPanel.SetActive(false);
        controlsSettingsPanel.SetActive(false);
        SettingMenuPanel.SetActive(true);
        mainMenuPanel.SetActive(true);
    }
    public void CloseOptions()
    {
        audioSettingsPanel.SetActive(false);
        videoSettingsPanel.SetActive(false);
        controlsSettingsPanel.SetActive(false);
        SettingMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
