using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int score;

    public TextMeshProUGUI scoreText;

    void Awake()
    {
        scoreText.text = "Score " + score;
    }

    public void AddScore(int amount)
    {
        score += amount;

        scoreText.text = "Score " + score;
    }
}