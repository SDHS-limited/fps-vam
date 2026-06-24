using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    [Header("시간 추가 애니메이션 설정")]
    public Animator bonusTimeAnimator;    // 애니메이션이 들어있는 UI의 Animator
    public TextMeshProUGUI bonusTimeText; // "+20" 같은 글자를 띄워줄 TextMeshPro

    [Header("타임 오버 및 결과 씬 설정")]
    public string resultSceneName = "ResultScene"; // 이동할 씬의 이름
    public float sceneTransitionDelay = 2.5f;      // 씬 전환 전 대기 시간 (Time's Up 애니메이션 길이)\
    public GameObject s;
    public GameObject a;

    // 기본 시간을 120초(2분)로 설정
    public float timer = 120f;

    // 타임 오버가 중복 실행되지 않도록 막는 플래그 (매우 중요)
    private bool isGameOver = false;

    void Update()
    {
        // 시간이 0보다 클 때만 감소하도록 설정
        if (timer > 0)
        {
            timer -= Time.deltaTime; // 시간이 1초씩 줄어듦

            // 시간이 0 이하로 떨어지지 않게 고정
            if (timer < 0) 
            {
                timer = 0;
                TriggerTimesUp();

            }

            int min = (int)(timer / 60);
            int sec = (int)(timer % 60);

            timerText.text = $"{min:00}:{sec:00}";
        }
    }

    // 아이템 획득 시 외부에서 호출할 시간 증가 함수
    public void AddTime(float bonusTime)
    {
        // 1. 함수가 정상적으로 실행되는지 콘솔창에 텍스트 띄우기
        Debug.Log("AddTime 함수가 호출되었습니다! 추가된 시간: " + bonusTime);
        timer += bonusTime;

        if (bonusTimeText != null)
        {
            bonusTimeText.text = "+" + bonusTime.ToString() + " Second"; // 예: "+20s"
        }
        else 
        {
            Debug.LogWarning("보너스 타임 텍스트 UI가 인스펙터에 연결되지 않았습니다!");
        }

        if (bonusTimeAnimator != null)
        {
            // 2. 만약 애니메이션 UI 오브젝트가 꺼져있다면 강제로 켜주기
            bonusTimeAnimator.gameObject.SetActive(true);

            bonusTimeAnimator.SetTrigger("open"); 
            Debug.Log("애니메이터 ShowBonus 트리거 작동 성공!");
        }
        else
        {
            Debug.LogWarning("보너스 타임 애니메이터가 인스펙터에 연결되지 않았습니다!");
        }
    }

    void TriggerTimesUp()
    {
        if (isGameOver) return; // 이미 실행 중이면 중복 방지
        isGameOver = true; // 플래그를 켜서 Update문에서의 중복 실행을 막음
        Debug.Log("타임 오버! 애니메이션 재생 후 결과 씬으로 이동합니다.");
        bonusTimeText.text = "Times Up";

        // 1. 시간 느려지게 만들기 (0.2배속)
        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // 물리 연산도 배속에 맞춰 조절

        if (bonusTimeAnimator != null)
        {
            // 작성해주신 "open2" 트리거로 Time's Up 애니메이션 실행
            bonusTimeAnimator.SetTrigger("open");
        }

        // 애니메이션이 끝날 때까지 기다렸다가 씬을 이동시키는 코루틴 실행
        StartCoroutine(GoToResultScene());
    }

    IEnumerator GoToResultScene()
    {
        // 인스펙터에서 설정한 시간(sceneTransitionDelay)만큼 대기
        yield return new WaitForSeconds(sceneTransitionDelay);

        // 씬 전환 전 시간 복구 (매우 중요!)
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // 지정된 이름의 씬으로 넘어감
        SceneManager.LoadScene(resultSceneName);
    }
}
