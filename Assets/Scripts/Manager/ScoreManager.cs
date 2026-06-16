using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int score;

    [SerializeField]
    private PhaseManager phaseManager;
    public TextMeshProUGUI scoreText;

    void Awake()
    {
        if (phaseManager == null)
            Debug.Log("ScoreManager.Awake: 'phaseManager' is not assigned in Inspector.");
        else
            Debug.Log("ScoreManager.Awake: 'phaseManager' assigned.");
        scoreText.text = "Score " + score;
    }

    public void AddScore(int amount)
    {
        score += amount;

        scoreText.text = "Score " + score;

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