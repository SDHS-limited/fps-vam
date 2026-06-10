using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public AbilityManager abilityManager;
    public PhaseManager phaseManager;

    bool abilitySelected;

    private void Awake()
    {
        Instance = this;
    }

    public void FaceHit(CubeFace face)
    {
        switch (face.faceType)
        {
            case FaceType.Ability:

                if (abilitySelected)
                    return;

                abilitySelected = true;

                Debug.Log("능력 선택");

                ApplyAbility();

                break;

            case FaceType.Phase:

                if (!abilitySelected)
                    return;

                Debug.Log("페이즈 시작");

                phaseManager.StartPhase();

                abilitySelected = false;

                abilityManager.GenerateRandomAbility();

                break;
        }
    }

    void ApplyAbility()
    {
        Ability a = abilityManager.currentAbility;

        Debug.Log(a.abilityName);
    }
}