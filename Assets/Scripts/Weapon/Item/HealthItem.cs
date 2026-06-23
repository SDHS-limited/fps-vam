using UnityEngine;

public class HealthItem : MonoBehaviour
{
    public float healAmount = 30f;

    void OnCollisionEnter(Collision other)
    {
        // 플레이어 태그 확인 ("Player" 태그가 설정되어 있어야 합니다)
        if (other.gameObject.CompareTag("Player"))
        {
            var playerController = other.gameObject.GetComponent<GravityPlayerController>();
            if (playerController != null)
            {
                playerController.Heal(healAmount);
                Destroy(gameObject); // 아이템 획득 후 제거
                Debug.Log("Health item collected! Player healed by " + healAmount);
            }
        }
    }
}