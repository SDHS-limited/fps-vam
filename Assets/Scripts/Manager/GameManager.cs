using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<Ability> selectedAbilities = new List<Ability>();
    public AbilityManager abilityManager;
    public PhaseManager phaseManager;
    public PlayerStatus playerStatus;

    

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
        if(selectedAbilities.Contains(ability))
        {
            Debug.Log("이미 선택한 능력입니다.");
            return;
        }

        selectedAbilities.Add(ability);
        Debug.Log("능력 선택됨: " + ability.abilityName);
        ApplyAbility(ability);
    }

    void ApplyAbility(Ability ability)
    {
        PlayerStatus stats = playerStatus;

    switch (ability.type)
    {
        case AbilityType.DamageUp:

            stats.damage += ability.value1;
            break;

        case AbilityType.FireRateUp:
            
            stats.fireRate -= ability.value1;
            stats.currentHp -= ability.value2;
            break;

        case AbilityType.MaxHpUp:

            stats.maxHp += ability.value1;
            stats.currentHp += ability.value1;
            break;

        case AbilityType.MoveSpeedUp:

            stats.moveSpeed += ability.value1;
            break;

        case AbilityType.DashUp:

            stats.dashDistance += ability.value1;
            break;

        case AbilityType.Berserker:

            stats.damage += ability.value1;
            stats.maxHp -= ability.value2;

            if(stats.currentHp > stats.maxHp)
                stats.currentHp = stats.maxHp;

            break;

        case AbilityType.Vampire:

            stats.lifeSteal += ability.value1;
            stats.maxHp -= ability.value2;

            break;

        case AbilityType.GlassCannon:

            stats.damage += ability.value1;

            stats.maxHp -= ability.value2;

            if(stats.currentHp > stats.maxHp)
                stats.currentHp = stats.maxHp;

            break;

        case AbilityType.GravityBurst:

            // 나중에 구현
            break;
    }

        Debug.Log("능력 적용 : " + ability.abilityName);

        //id 식별 후 실제 능력 적용하게
    }
}