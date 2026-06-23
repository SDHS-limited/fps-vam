using UnityEngine;

public class TimeItem : MonoBehaviour
{
    [Header("추가할 시간 (초 단위)")]
    public float bonusTime = 20f; 

    // 플레이어가 아이템에 닿았을 때 실행되는 물리 이벤트
    private void OnCollisionEnter(Collision other)
    {
        // 부딪힌 객체의 태그가 "Player"인지 확인
        if (other.collider.CompareTag("Player"))
        {
            // 씬에 있는 TimerUI 스크립트를 찾아서 AddTime 함수 실행
            TimerUI timerUI = FindObjectOfType<TimerUI>();
            if (timerUI != null)
            {
                // TimerUI로 보너스 시간을 넘겨주면, 그곳에서 애니메이션과 텍스트 변경을 알아서 처리함
                timerUI.AddTime(bonusTime);
            }

            // 아이템 획득 후 오브젝트 삭제 (이펙트나 소리를 넣으려면 이 줄 바로 위에 추가)
            Destroy(gameObject); 
        }
    }
}