using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    
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
    public void AddTime(float amount)
    {
        timer += amount;
    }
}
