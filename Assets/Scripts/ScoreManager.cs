using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int score;

    [SerializeField]
    private PhaseManager phaseManager;

    void Awake()
    {
        if (phaseManager == null)
            Debug.Log("ScoreManager.Awake: 'phaseManager' is not assigned in Inspector.");
        else
            Debug.Log("ScoreManager.Awake: 'phaseManager' assigned.");
    }

    public void AddScore(int amount)
    {
        score += amount;

        Debug.Log("현재 점수 : " + score);

        if (phaseManager == null)
        {
            Debug.LogWarning("ScoreManager.AddScore: 'phaseManager' is not assigned. Attempting to find PhaseManager in scene.");
            phaseManager = FindObjectOfType<PhaseManager>();

            if (phaseManager == null)
            {
                Debug.LogError("ScoreManager.AddScore: No PhaseManager found in scene. Cannot check phase progress.");
                return;
            }
            else
            {
                Debug.Log("ScoreManager.AddScore: Found PhaseManager via FindObjectOfType.");
            }
        }

        phaseManager.CheckPhaseProgress(score);
    }
}