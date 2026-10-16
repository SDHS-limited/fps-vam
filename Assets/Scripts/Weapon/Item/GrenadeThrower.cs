using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrenadeThrower : MonoBehaviour
{
    [Header("투척 설정")]
    public GameObject grenadePrefab; // 던질 수류탄 프리팹
    public Transform throwPoint;     // 수류탄이 생성될 위치 (예: 카메라 앞 또는 손)
    public float throwForce = 15f;   // 던지는 힘
    public int maxGrenades = 3;          // 최대 수류탄 개수
    public Image GrenadeUI; // 수류탄 UI 이미지
    public TextMeshProUGUI text; // 수류탄 개수 텍스트

    void Update()
    {
        // G키를 누르면 수류탄 투척
        if (Input.GetKeyDown(KeyManager.instance.keys["Grenade"]) && maxGrenades > 0)
        {
            ThrowGrenade();
            maxGrenades--;
        }
        else
        {
            
        }
        if (text != null)
        {
            text.text = "X " + maxGrenades;
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