using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// REVFIELD — Gravity Anchor (설치형 중력 제어 장치)
/// Enemy.cs 및 EnemyGravity.cs 완벽 연동 버전
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class GravityAnchor : MonoBehaviour
{
    public enum AnchorMode
    {
        Attraction,  // ① 흡입 모드
        Repulsion,   // ② 반발 모드
        Directional  // ③ 방향 중력 모드
    }

    [Header("기본 설정")]
    public AnchorMode currentMode = AnchorMode.Attraction;
    [Tooltip("중력 영향 반경")]
    public float effectRadius = 6f;
    [Tooltip("앵커 지속 시간 (초)")]
    public float lifeTime = 10f;

    [Header("① Attraction (흡입) 설정")]
    [Tooltip("중심으로 끌어당기는 힘")]
    public float pullForce = 35f;
    [Tooltip("중심점 겹침 방지를 위한 감속 거리")]
    public float stopDistance = 1.5f;
    [Tooltip("흡입 시 적의 표면 밀착 중력을 끄고 공중에 띄울지 여부")]
    public bool floatEnemiesOnPull = true;

    [Header("② Repulsion (반발 — Enemy.ApplyGravityPulse 연동)")]
    [Tooltip("바깥으로 날려버리는 속도")]
    public float pushSpeed = 18f;
    [Tooltip("반발 파동 직접 피해량")]
    public float pulseDirectDamage = 5f;
    [Tooltip("밀려난 적이 벽에 부딪힐 때 입는 추가 충돌 피해량")]
    public float pulseWallDamage = 25f;
    [Tooltip("반발 파동 발동 주기 (초)")]
    public float pulseInterval = 1.2f;
    private float pulseTimer = 0f;

    [Header("③ Directional (방향 중력) 설정")]
    [Tooltip("기존 EnemyGravity.gravityStrength 대비 배율")]
    public float directionalStrengthMultiplier = 1.2f;

    private SphereCollider triggerZone;
    private List<EnemyGravity> affectedEnemies = new List<EnemyGravity>();

    private void Awake()
    {
        triggerZone = GetComponent<SphereCollider>();
        triggerZone.isTrigger = true;
        triggerZone.radius = effectRadius;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // [테스트용] 숫자키 1, 2, 3으로 실시간 모드 전환
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeMode(AnchorMode.Attraction);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeMode(AnchorMode.Repulsion);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeMode(AnchorMode.Directional);

        if (currentMode == AnchorMode.Repulsion)
        {
            pulseTimer -= Time.deltaTime;
            if (pulseTimer <= 0f)
            {
                TriggerRepulsionPulse();
                pulseTimer = pulseInterval;
            }
        }
    }

    private void FixedUpdate()
    {
        affectedEnemies.RemoveAll(item => item == null);

        foreach (var enemyGrav in affectedEnemies)
        {
            Enemy enemyMain = enemyGrav.GetComponent<Enemy>();
            bool isBrute = enemyMain != null && enemyMain.isBrute;

            switch (currentMode)
            {
                case AnchorMode.Attraction:
                    HandleAttraction(enemyGrav, isBrute);
                    break;

                case AnchorMode.Repulsion:
                    enemyGrav.ClearAnchorOverride();
                    break;

                case AnchorMode.Directional:
                    // Brute(중량급) 적은 방향 중력의 영향을 절반만 받도록 처리
                    float mult = isBrute ? directionalStrengthMultiplier * 0.5f : directionalStrengthMultiplier;
                    enemyGrav.SetAnchorDirectionalGravity(transform.up, mult);
                    break;
            }
        }
    }

    // ① Attraction (흡입 모드)
    private void HandleAttraction(EnemyGravity enemyGrav, bool isBrute)
    {
        // 일반 적은 무중력으로 띄워서 끌어당기고, Brute는 바닥에 붙은 채로 느리게 끌려옴
        if (floatEnemiesOnPull && !isBrute)
        {
            enemyGrav.SetAnchorZeroGravity(true);
        }

        Vector3 direction = transform.position - enemyGrav.transform.position;
        float distance = direction.magnitude;
        float actualPull = isBrute ? pullForce * 0.35f : pullForce;

        if (distance > stopDistance)
        {
            enemyGrav.Rb.AddForce(direction.normalized * actualPull, ForceMode.Acceleration);
        }
        else
        {
            // Unity 6 linearVelocity 감속 처리
            enemyGrav.Rb.linearVelocity *= 0.85f;
        }
    }

    // ② Repulsion (반발 모드 — Enemy.ApplyGravityPulse 직접 활용)
    private void TriggerRepulsionPulse()
    {
        foreach (var enemyGrav in affectedEnemies)
        {
            if (enemyGrav == null) continue;

            Vector3 pushDir = (enemyGrav.transform.position - transform.position).normalized;
            pushDir = (pushDir + (-enemyGrav.GravityDir * 0.35f)).normalized;

            // Enemy.cs에 이미 구현된 ApplyGravityPulse를 호출하여 벽 충돌 데미지까지 자동 연동!
            if (enemyGrav.TryGetComponent(out Enemy enemyMain))
            {
                enemyMain.ApplyGravityPulse(pushDir, pushSpeed, pulseDirectDamage, pulseWallDamage);
            }
            else
            {
                enemyGrav.BreakAnchorControl(0.4f);
                enemyGrav.Rb.AddForce(pushDir * pushSpeed, ForceMode.VelocityChange);
            }
        }
    }

    public void ChangeMode(AnchorMode newMode)
    {
        currentMode = newMode;
        pulseTimer = 0f;

        foreach (var enemyGrav in affectedEnemies)
        {
            if (enemyGrav != null) enemyGrav.ClearAnchorOverride();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out EnemyGravity enemyGrav))
        {
            if (!affectedEnemies.Contains(enemyGrav))
            {
                affectedEnemies.Add(enemyGrav);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out EnemyGravity enemyGrav))
        {
            if (affectedEnemies.Contains(enemyGrav))
            {
                enemyGrav.ClearAnchorOverride();
                affectedEnemies.Remove(enemyGrav);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var enemyGrav in affectedEnemies)
        {
            if (enemyGrav != null) enemyGrav.ClearAnchorOverride();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = currentMode switch
        {
            AnchorMode.Attraction => new Color(0.5f, 0f, 1f, 0.25f),
            AnchorMode.Repulsion => new Color(1f, 0.2f, 0.2f, 0.25f),
            AnchorMode.Directional => new Color(0f, 0.8f, 1f, 0.25f),
            _ => Color.white
        };
        Gizmos.DrawSphere(transform.position, effectRadius);

        if (currentMode == AnchorMode.Directional)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.up * 3f);
        }
    }
}
