using UnityEngine;

public class GrenadeThrower : MonoBehaviour
{
    [Header("투척 설정")]
    public GameObject grenadePrefab; // 던질 수류탄 프리팹
    public Transform throwPoint;     // 수류탄이 생성될 위치 (예: 카메라 앞 또는 손)
    public float throwForce = 15f;   // 던지는 힘
    

    void Update()
    {
        // G키를 누르면 수류탄 투척
        if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowGrenade();
        }
    }

    void ThrowGrenade()
    {
        if (grenadePrefab == null || throwPoint == null) return;

        // 수류탄 생성
        GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, throwPoint.rotation);

        // Rigidbody를 이용해 전방으로 물리적인 힘을 가함
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(throwPoint.forward * throwForce, ForceMode.VelocityChange);
        }
    }
}