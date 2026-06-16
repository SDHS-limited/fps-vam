using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class hitUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image flashImage; // 전체 화면을 덮는 UI Image

    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.4f); // 피격 색상 (빨간색, 최대 알파값 0.4)
    [SerializeField] private float fadeSpeed = 5f; // 원래대로 돌아오는 속도

    private Coroutine flashCoroutine;

    // 외부(예: PlayerHealth 스크립트)에서 피격 시 호출할 함수
    public void TriggerFlash()
    {
        // 이미 실행 중인 플래시 코루틴이 있다면 중지하여 꼬임 방지
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        // 플래시 효과 시작
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 1. 순간적으로 지정한 피격 색상과 알파값으로 변경
        flashImage.color = flashColor;

        // 2. 알파값을 점진적으로 0(투명)까지 줄임
        while (flashImage.color.a > 0.01f)
        {
            Color currentColor = flashImage.color;
            // Lerp를 이용해 부드럽게 감소
            currentColor.a = Mathf.Lerp(currentColor.a, 0f, Time.deltaTime * fadeSpeed);
            flashImage.color = currentColor;

            yield return null; // 다음 프레임까지 대기
        }

        // 3. 완전히 투명하게 초기화
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }
}
