using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    [Header("시간 추가 애니메이션 설정")]
    public Animator bonusTimeAnimator;    // 애니메이션이 들어있는 UI의 Animator
    public TextMeshProUGUI bonusTimeText; // "+20" 같은 글자를 띄워줄 TextMeshPro
    
    // 기본 시간을 120초(2분)로 설정
    public float timer = 120f; 

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
                // 여기에 시간 초과 시 실행할 코드(게임 오버 등)를 추가할 수 있습니다.
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
}
