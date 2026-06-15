using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int score;

    [SerializeField]
    private PhaseManager phaseManager;

    public void AddScore(int amount)
    {
        score += amount;

        Debug.Log("현재 점수 : " + score);

        phaseManager.CheckPhaseProgress(score);
    }
}