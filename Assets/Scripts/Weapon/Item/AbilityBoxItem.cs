using UnityEngine;

public class AbilityBoxItem : MonoBehaviour
{
    [Header("아이템 획득")]
    public AudioClip audioClip;

    private bool isCollected = false;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isCollected)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GravityPlayerController player =
            other.collider.GetComponentInParent<GravityPlayerController>();

        if (player == null)
            return;

        AbilitySelectionUI selectionUI =
            FindFirstObjectByType<AbilitySelectionUI>();

        if (selectionUI == null)
        {
            Debug.LogWarning("AbilitySelectionUI를 찾을 수 없습니다.");
            return;
        }

        if (!selectionUI.Open(player))
            return;

        isCollected = true;

        if (audioClip != null)
        {
            AudioSource.PlayClipAtPoint(
                audioClip,
                transform.position
            );
        }

        Destroy(gameObject);
        } 
    }
}