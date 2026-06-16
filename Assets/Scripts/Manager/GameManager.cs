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
        switch(face.faceType)
        {
            case FaceType.Ability1:
    
                SelectAbility(
                    abilityManager.panel1.currentAbility
                );
                break;
    
            case FaceType.Ability2:
    
                SelectAbility(
                    abilityManager.panel2.currentAbility
                );
                break;
    
            case FaceType.Ability3:
    
                SelectAbility(
                    abilityManager.panel3.currentAbility
                );
                break;
    
            case FaceType.Phase:
    
                phaseManager.StartNextPhase(); 
                break;
        }
    }
    public void SelectAbility(Ability ability)
    {
        Debug.Log("능력 선택됨: " + ability.abilityName);
        ApplyAbility(ability);
    }

    void ApplyAbility(Ability ability)
    {
        Debug.Log("능력 적용: " + ability.buffText);
    }
}