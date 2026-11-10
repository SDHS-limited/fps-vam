using TMPro;
using UnityEngine;

public class AbilityPanel : MonoBehaviour
{
    public TMP_Text abilityName;
    public TMP_Text buffText;
    public TMP_Text nuffText;

    public AbilityData currentAbility;
    
    public void Select()
    {
        if (currentAbility == null)
            return;

        GravityPlayerController player = FindAnyObjectByType<GravityPlayerController>();
        if (player != null)
            player.ApplyAbility(currentAbility);
        else
            Debug.LogWarning("AbilityPanel: GravityPlayerController was not found.");
    }

    public void SetAbility(AbilityData ability)
    {
        currentAbility = ability;

        if (abilityName != null) abilityName.text = ability != null ? ability.name : string.Empty;
        if (buffText != null) buffText.text = ability != null ? ability.Buffdescription : string.Empty;
        if (nuffText != null) nuffText.text = ability != null ? ability.Nuffdescription : string.Empty;
    }
}
