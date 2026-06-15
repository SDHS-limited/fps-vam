using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    public int currentPhase = 1;

    public Animator anim;

    public AbilityManager abilityManager;
    public EnemySpawner enemySpawner;

    bool waitingForUpgrade = true;

    public int[] phaseScores =
    {
        100,
        300,
        600,
        1000
    };

    public void CheckPhaseProgress(int score)
    {
        if(waitingForUpgrade)
            return;

        if(currentPhase - 1 >= phaseScores.Length)
            return;

        if(score >= phaseScores[currentPhase - 1])
        {
            OpenUpgradeWall();
        }
    }

    void OpenUpgradeWall()
    {
        waitingForUpgrade = true;

        anim.SetBool("clear", false);

        abilityManager.GenerateAbilities();

        Debug.Log("능력 선택");
    }

    public void StartNextPhase()
    {
        if(!waitingForUpgrade)
            return;

        waitingForUpgrade = false;

        currentPhase++;

        anim.SetBool("clear", true);

        Debug.Log("Phase " + currentPhase);

        enemySpawner.SpawnWave();
    }
}