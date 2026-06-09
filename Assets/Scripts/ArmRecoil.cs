// ArmRecoil.cs — 팔 + 총 같이 반동
using UnityEngine;
 
public class ArmRecoil : MonoBehaviour
{
    [Header("── 반동 설정 ──")]
    public float recoilX      = -5f;
    public float recoilSpeed  = 20f;
    public float recoverSpeed = 8f;
 
    [Header("── 같이 반동할 총 오브젝트 ──")]
    public Transform gun;   // Pistol_D 또는 Pistol_D (1) 드래그
 
    private Vector3 baseRot;
    private Vector3 currentRot;
    private Vector3 targetRot;
 
    private Vector3 gunBaseRot;
    private Vector3 gunCurrentRot;
    private Vector3 gunTargetRot;
 
    void Start()
    {
        baseRot    = transform.localEulerAngles;
        currentRot = baseRot;
        targetRot  = baseRot;
 
        if (gun)
        {
            gunBaseRot    = gun.localEulerAngles;
            gunCurrentRot = gunBaseRot;
            gunTargetRot  = gunBaseRot;
        }
    }
 
    void Update()
    {
        // 팔 반동
        targetRot  = Vector3.Lerp(targetRot,  baseRot,   Time.deltaTime * recoverSpeed);
        currentRot = Vector3.Lerp(currentRot, targetRot, Time.deltaTime * recoilSpeed);
        transform.localEulerAngles = currentRot;
 
        // 총 반동 (팔이랑 동일하게)
        if (gun)
        {
            gunTargetRot  = Vector3.Lerp(gunTargetRot,  gunBaseRot,    Time.deltaTime * recoverSpeed);
            gunCurrentRot = Vector3.Lerp(gunCurrentRot, gunTargetRot,  Time.deltaTime * recoilSpeed);
            gun.localEulerAngles = gunCurrentRot;
        }
    }
 
    public void TriggerRecoil()
    {
        // 팔
        targetRot.x = Mathf.Clamp(
            targetRot.x + recoilX,
            baseRot.x + recoilX * 3f,
            baseRot.x
        );
 
        // 총 (팔이랑 같은 방향)
        if (gun)
        {
            gunTargetRot.x = Mathf.Clamp(
                gunTargetRot.x + recoilX,
                gunBaseRot.x + recoilX * 3f,
                gunBaseRot.x
            );
        }
    }
}