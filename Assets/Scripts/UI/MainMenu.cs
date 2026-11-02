using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name != "MainTitle") return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void RestoreTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    [Header("UI 설정")]
    [SerializeField] private GameObject mainMenuPanel; // 메인 메뉴 패널
    [SerializeField] private GameObject SettingMenuPanel; // 메인 메뉴 패널
    [SerializeField] private GameObject audioSettingsPanel; // 오디오 설정 패널
    [SerializeField] private GameObject videoSettingsPanel; // 비디오 설정 패널
    [SerializeField] private GameObject controlsSettingsPanel; // 컨트롤 설정 패널
    public void startgame()
    {
        RestoreTime();
        SceneManager.LoadScene("Stage 1");
    }

    public void RestartGame()
    {
        ReloadCurrentScene();
    }

    public static void ReloadCurrentScene()
    {
        RestoreTime();
#if UNITY_EDITOR
        if (SceneManager.GetActiveScene().buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Restarting game...");
    }
    
    public void QuitGame()
    {
        RestoreTime();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
