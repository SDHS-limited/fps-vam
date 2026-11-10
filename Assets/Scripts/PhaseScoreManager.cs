using UnityEngine;
using UnityEngine.Events;

public class PhaseScoreManager : MonoBehaviour
{
    public int score;

    [SerializeField] private UnityEvent<int> onScoreChanged = new UnityEvent<int>();

    public void AddScore(int amount)
    {
        score += amount;

        Debug.Log("현재 점수 : " + score);

        onScoreChanged?.Invoke(score);
    }
}
