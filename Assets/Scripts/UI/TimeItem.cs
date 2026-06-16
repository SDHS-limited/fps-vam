using UnityEngine;

public class TimeItem : MonoBehaviour
{
    [Header("추가할 시간 (초 단위)")]
    public float bonusTime = 15f; // 아이템을 먹으면 15초 증가

    // 플레이어가 아이템에 닿았을 때 실행되는 물리 이벤트
    private void OnTriggerEnter(Collider other)
    {
        // 부딪힌 객체의 태그가 "Player"인지 확인
        if (other.CompareTag("Player"))
        {
            // 씬에 있는 TimerUI 스크립트를 찾아서 AddTime 함수 실행
            TimerUI timerUI = FindObjectOfType<TimerUI>();
            if (timerUI != null)
            {
                timerUI.AddTime(bonusTime);
            }

            // 아이템 획득 후 오브젝트 삭제
            Destroy(gameObject); 
        }
    }
}
