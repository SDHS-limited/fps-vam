using TMPro;
using UnityEngine;

public class AbilityPanel : MonoBehaviour
{
    public TMP_Text abilityName;
    public TMP_Text buffText;
    public TMP_Text nuffText;

    public Ability currentAbility;
    
        public void Select()
    {
        GameManager.Instance.SelectAbility(currentAbility);
    }

    public void SetAbility(Ability ability)
    {
        currentAbility = ability;

        abilityName.text = ability.abilityName;
        buffText.text = ability.buffText;
        nuffText.text = ability.nuffText;
    }
}