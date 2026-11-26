// ArmRecoil.cs — 팔 + 총 같이 반동
using UnityEngine;

public class ArmRecoil : MonoBehaviour
{
    [Header("── 반동 설정 ──")]
    public float recoilX = -5f;
    public float recoilSpeed = 20f;
    public float recoverSpeed = 8f;

    [Header("── 같이 반동할 총 오브젝트 ──")]
    public Transform gun;

    private float targetRecoil;
    private float currentRecoil;
    private Quaternion armPose;
    private Quaternion armPoseWithRecoil;
    private bool armRecoilApplied;
    private Vector3 gunBaseAngles;

    private void Start()
    {
        if (gun)
        {
            gunBaseAngles = gun.localEulerAngles;
        }
    }

    private void Update()
    {
        // Animator가 자세를 쓰지 않는 프레임에도 반동이 누적되지 않도록 복구합니다.
        RestoreArmPose();
    }

    private void LateUpdate()
    {
        targetRecoil = Mathf.Lerp(targetRecoil, 0f, Time.deltaTime * recoverSpeed);
        currentRecoil = Mathf.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilSpeed);

        if (Mathf.Abs(currentRecoil) > 0.001f)
        {
            // Animator가 계산한 이번 프레임의 자세에 반동을 더합니다.
            armPose = transform.localRotation;
            Vector3 armAngles = transform.localEulerAngles;
            armAngles.x += currentRecoil;
            transform.localEulerAngles = armAngles;
            armPoseWithRecoil = transform.localRotation;
            armRecoilApplied = true;
        }

        if (gun)
        {
            Vector3 gunAngles = gunBaseAngles;
            gunAngles.x += currentRecoil;
            gun.localEulerAngles = gunAngles;
        }
    }

    private void OnDisable()
    {
        RestoreArmPose();
        targetRecoil = 0f;
        currentRecoil = 0f;
    }

    private void RestoreArmPose()
    {
        if (armRecoilApplied && transform.localRotation == armPoseWithRecoil)
        {
            transform.localRotation = armPose;
        }

        armRecoilApplied = false;
    }

    public void TriggerRecoil()
    {
        float maxRecoil = recoilX * 3f;
        targetRecoil = Mathf.Clamp(targetRecoil + recoilX,
            Mathf.Min(0f, maxRecoil), Mathf.Max(0f, maxRecoil));
    }
}
