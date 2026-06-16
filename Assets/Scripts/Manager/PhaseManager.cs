using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public int currentPhase = 1;

    public Animator anim;

    public AbilityManager abilityManager;
    public EnemySpawner enemySpawner;
    [SerializeField] TextMeshProUGUI text;

    public bool waitingForUpgrade = false;

    void Start()
    {
        text.text = "Phase "+currentPhase;
        Debug.Log("PhaseManager.Start: currentPhase=" + currentPhase + ", waitingForUpgrade=" + waitingForUpgrade);
    }

    public int[] phaseScores =
    {
        100,
        300,
        600,
        1000
    };

    public void CheckPhaseProgress(int score)
    {
        Debug.Log("CheckPhaseProgress called: score=" + score + ", currentPhase=" + currentPhase + ", waitingForUpgrade=" + waitingForUpgrade);

        if (waitingForUpgrade)
        {
            Debug.Log("CheckPhaseProgress: waitingForUpgrade is true, returning");
            return;
        }

        if (currentPhase - 1 >= phaseScores.Length)
        {
            Debug.Log("CheckPhaseProgress: currentPhase exceeds phaseScores, returning");
            return;
        }

        if (score >= phaseScores[currentPhase - 1])
        {
            Debug.Log("실행");
            Debug.Log("CheckPhaseProgress: threshold reached, calling OpenUpgradeWall()");
            OpenUpgradeWall();
        }
    }

    void OpenUpgradeWall()
    {
        waitingForUpgrade = true;

        if (anim == null)
        {
            Debug.LogWarning("PhaseManager.OpenUpgradeWall: 'anim' is not assigned");
        }
        else
        {
            anim.SetBool("clear", false);
        }

        if (abilityManager == null)
        {
            Debug.LogWarning("PhaseManager.OpenUpgradeWall: 'abilityManager' is not assigned");
        }
        else
        {
            abilityManager.GenerateAbilities();
        }

        Debug.Log("능력 선택");
    }

    public void StartNextPhase()
    {
        // if(waitingForUpgrade)
        //     return;

        waitingForUpgrade = false;

        currentPhase++;

        anim.SetBool("clear", true);

        Debug.Log("Phase " + currentPhase);
        
        text.text = "Phase "+currentPhase;

        enemySpawner.SpawnWave();
    }
}