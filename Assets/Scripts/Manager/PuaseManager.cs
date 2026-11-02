using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuaseManager : MonoBehaviour
{
    [SerializeField] public GameObject pauseMenuUI;
    [SerializeField] public GameObject optionMenuUI;
    

    void Start()
    {
        pauseMenuUI.SetActive(false);
        //optionMenuUI.SetActive(false);
    }

    void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Escape))
        //{
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Cursor.visible = true;
        //    pauseMenuUI.SetActive(true);
        //    Time.timeScale = 0f; // 게임 시간 일시정지
            
        //}
    }
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // 게임 시간 재개   
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        MainMenu.ReloadCurrentScene();
    }
    public void Option()
    {
        optionMenuUI.SetActive(true);
    }
    public void Quit()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainTitle"); // 메인 메뉴 씬으로 이동
    }
}
