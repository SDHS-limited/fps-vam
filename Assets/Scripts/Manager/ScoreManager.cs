using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("점수 설정")]
    public int score;
    public TextMeshProUGUI scoreText;

    [Header("콤보 설정")]
    public int comboCount = 0;
    public float comboTimeout = 2.5f; // 콤보가 유지되는 시간 (초)
    private float currentComboTimer = 0f;

    public TextMeshProUGUI comboText; // 콤보를 표시할 UI 텍스트

    void Awake()
    {
        UpdateScoreUI();
        UpdateComboUI(0);
    }

    void Update()
    {
        // 콤보 타이머가 작동 중일 때
        if (comboCount > 0)
        {
            currentComboTimer -= Time.deltaTime;

            // 제한 시간이 지나면 콤보 초기화
            if (currentComboTimer <= 0f)
            {
                ResetCombo();
            }
        }
    }

    public void AddScore(int amount)
    {
        // 적을 처치할 때마다 콤보 1 증가 및 타이머 초기화
        comboCount++;
        currentComboTimer = comboTimeout;

        // 콤보 배율 적용
        int finalAmount = amount * comboCount;
        score += finalAmount;

        UpdateScoreUI();

        // UI를 업데이트할 때 최종 획득한 점수(finalAmount)를 전달
        UpdateComboUI(finalAmount);
    }

    private void ResetCombo()
    {
        comboCount = 0;
        UpdateComboUI(0);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score " + score;
        }
    }

    // addedScore 매개변수를 추가하여 텍스트에 반영
    private void UpdateComboUI(int addedScore)
    {
        if (comboText != null)
        {
            if (comboCount > 1)
            {
                comboText.gameObject.SetActive(true);
                // 요청하신 "Combo! +추가된 값" 형태의 텍스트
                comboText.text = "Combo! +" + addedScore;
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }
}