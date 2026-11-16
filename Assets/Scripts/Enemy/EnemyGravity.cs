using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 적 전용 커스텀 중력 (Gravity Anchor + 피격 타격감 연출 통합 버전)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyGravity : MonoBehaviour
{
    [Header("중력 세기")]
    [Tooltip("중력 가속도 (유니티 기본 9.81보다 크게 잡아야 큐브에 밀착됨)")]
    public float gravityStrength = 25f;

    [Header("표면 감지")]
    [Tooltip("Ground 태그 — 큐브 6면 오브젝트에 설정")]
    public string groundTag = "Ground";
    [Tooltip("레이 감지 거리 — 캐릭터 크기의 약 1.5배 권장")]
    public float detectRange = 1.5f;
    [Tooltip("Ground 레이어 마스크 (스폰홀 레이어 제외)")]
    public LayerMask groundMask = ~0;

    [Header("중력 전환 보간")]
    [Tooltip("중력 방향 전환 속도 (높을수록 즉각 전환, 낮을수록 부드럽게)")]
    public float gravityTurnSpeed = 8f;
    [Tooltip("표면 정렬 회전 속도")]
    public float alignSpeed = 10f;

    [Header("표면 이탈 허용 시간")]
    [Tooltip("감지 실패 후 이 시간(초) 동안은 마지막 중력 방향 유지")]
    public float lostSurfaceTimeout = 0.5f;

    [Header("스폰 설정")]
    [Tooltip("스폰 직후 표면 감지를 무시하고 강제로 아래로 떨어지게 할 시간(초)")]
    public float spawnDropDelay = 0.5f;
    private float spawnTimer = 0f;

    [Header("피격 타격감 연출 (Hit Tilt)")]
    [Tooltip("기울어질 적의 외형(Mesh) 자식 오브젝트. 비워두면 루트에서 자동 탐색")]
    public Transform visualModel;
    [Tooltip("피격 시 휘어지는 Z축 각도")]
    public float hitTiltAngleZ = -25f;
    [Tooltip("원래 자세로 복구되는 속도")]
    public float hitTiltRecoverSpeed = 15f;

    // ── 내부 상태
    Rigidbody rb;
    Vector3 currentGravityDir = Vector3.down;
    Vector3 targetGravityDir  = Vector3.down;
    float lostSurfaceTimer = 0f;
    bool onSurface = false;

    // ── 피격 연출용 내부 변수
    private float currentTiltZ = 0f;
    private Quaternion initialVisualLocalRot;

    // ── Gravity Anchor 제어용 상태 변수
    private bool isAnchorOverridden = false;
    private Vector3 anchorGravityDir;
    private float anchorGravityMultiplier = 1f;
    private float breakerOverrideTimer = 0f;

    static readonly Vector3[] SixDirs =
    {
        Vector3.down,
        Vector3.up,
        Vector3.left,
        Vector3.right,
        Vector3.forward,
        Vector3.back
    };

    // 외부 참조용 프로퍼티
    public Vector3 GravityDir => currentGravityDir;
    public bool IsOnSurface => onSurface;
    public Rigidbody Rb => rb;
    public bool IsAnchorOverridden => isAnchorOverridden;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;

        // visualModel이 인스펙터에서 할당되지 않았다면 첫 번째 자식 오브젝트를 자동 할당
        if (visualModel == null && transform.childCount > 0)
        {
            visualModel = transform.GetChild(0);
        }

        if (visualModel != null)
        {
            initialVisualLocalRot = visualModel.localRotation;
        }
    }

    void FixedUpdate()
    {
        // 1. Gravity Breaker 타격 직후라면 모든 앵커 효과 일시 무시
        if (breakerOverrideTimer > 0f)
        {
            breakerOverrideTimer -= Time.fixedDeltaTime;
            DetectSurface();
            ApplyGravity();
            AlignToSurface();
            return;
        }

        // 2. 스폰 직후 강제 하강
        if (spawnTimer < spawnDropDelay)
        {
            spawnTimer += Time.fixedDeltaTime;
            targetGravityDir = Vector3.down;
            currentGravityDir = Vector3.Slerp(
                currentGravityDir,
                targetGravityDir,
                Time.fixedDeltaTime * gravityTurnSpeed
            ).normalized;
        }
        // 3. 앵커에 의해 중력이 강제 제어 중일 때
        else if (isAnchorOverridden)
        {
            onSurface = false;
            targetGravityDir = anchorGravityDir;
            currentGravityDir = Vector3.Slerp(
                currentGravityDir,
                targetGravityDir,
                Time.fixedDeltaTime * gravityTurnSpeed
            ).normalized;
        }
        // 4. 평소 상태: 6방향 표면 감지
        else
        {
            DetectSurface();
        }

        ApplyGravity();
        AlignToSurface();
    }

    void Update()
    {
        // 피격 기울기 복구 애니메이션 처리
        UpdateHitTilt();
    }

    // ────────────────────────────────────────
    //  피격 타격감 연출 로직
    // ────────────────────────────────────────
    void UpdateHitTilt()
    {
        if (visualModel == null) return;

        if (Mathf.Abs(currentTiltZ) > 0.01f)
        {
            // 0도로 부드럽게 탄성 복구
            currentTiltZ = Mathf.Lerp(currentTiltZ, 0f, Time.deltaTime * hitTiltRecoverSpeed);
            visualModel.localRotation = initialVisualLocalRot * Quaternion.Euler(0f, 0f, currentTiltZ);
        }
        else if (currentTiltZ != 0f)
        {
            currentTiltZ = 0f;
            visualModel.localRotation = initialVisualLocalRot;
        }
    }

    /// <summary>
    /// 적이 데미지를 입을 때 호출하세요! (예: TakeDamage() 내부에서 호출)
    /// 순간적으로 rotation.z가 -25도 꺾였다가 복구됩니다.
    /// </summary>
    public void PlayHitTilt()
    {
        currentTiltZ = hitTiltAngleZ; // 기본값 -25f
        if (visualModel != null)
        {
            visualModel.localRotation = initialVisualLocalRot * Quaternion.Euler(0f, 0f, currentTiltZ);
        }
    }

    // ────────────────────────────────────────
    //  1. 표면 감지 — 가장 가까운 Ground 면 우선
    // ────────────────────────────────────────
    void DetectSurface()
    {
        float closestDist = Mathf.Infinity;
        Vector3 bestNormal = Vector3.zero;
        bool found = false;

        foreach (var dir in SixDirs)
        {
            if (!Physics.Raycast(
                    transform.position,
                    dir,
                    out RaycastHit hit,
                    detectRange,
                    groundMask))
                continue;

            if (!hit.collider.CompareTag(groundTag))
                continue;

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                bestNormal  = hit.normal;
                found       = true;
            }
        }

        if (found)
        {
            targetGravityDir  = -bestNormal;
            lostSurfaceTimer  = 0f;
            onSurface         = true;
        }
        else
        {
            lostSurfaceTimer += Time.fixedDeltaTime;
            if (lostSurfaceTimer >= lostSurfaceTimeout)
            {
                onSurface = false;
                targetGravityDir = GetFallbackGravity();
            }
        }

        currentGravityDir = Vector3.Slerp(
            currentGravityDir,
            targetGravityDir,
            Time.fixedDeltaTime * gravityTurnSpeed
        ).normalized;
    }

    // ────────────────────────────────────────
    //  2. 중력 힘 적용
    // ────────────────────────────────────────
    void ApplyGravity()
    {
        float strength = isAnchorOverridden ? gravityStrength * anchorGravityMultiplier : gravityStrength;
        rb.AddForce(currentGravityDir * strength, ForceMode.Acceleration);
    }

    // ────────────────────────────────────────
    //  3. 표면 법선에 맞게 회전 정렬
    // ────────────────────────────────────────
    void AlignToSurface()
    {
        if (currentGravityDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.FromToRotation(
            transform.up,
            -currentGravityDir
        ) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.fixedDeltaTime * alignSpeed
        );
    }

    // ────────────────────────────────────────
    //  4. Fallback — 표면에서 완전히 이탈 시
    // ────────────────────────────────────────
    Vector3 GetFallbackGravity()
    {
        GameObject cubeCenter = GameObject.FindWithTag("CubeCenter");
        if (cubeCenter != null)
        {
            Vector3 dir = (cubeCenter.transform.position - transform.position).normalized;
            return dir;
        }
        return Vector3.down;
    }

    // ────────────────────────────────────────
    //  5. 외부에서 중력 방향 강제 지정
    // ────────────────────────────────────────
    public void ForceGravityDirection(Vector3 dir)
    {
        targetGravityDir = dir.normalized;
    }

    // ────────────────────────────────────────
    //  6. Gravity Anchor 연동 전용 메서드
    // ────────────────────────────────────────
    public void SetAnchorDirectionalGravity(Vector3 dir, float strengthMultiplier = 1.2f)
    {
        isAnchorOverridden = true;
        anchorGravityDir = dir.normalized;
        anchorGravityMultiplier = strengthMultiplier;
    }

    public void SetAnchorZeroGravity(bool enableZeroG)
    {
        if (enableZeroG)
        {
            isAnchorOverridden = true;
            anchorGravityDir = Vector3.zero;
            anchorGravityMultiplier = 0f;
        }
        else
        {
            ClearAnchorOverride();
        }
    }

    public void ClearAnchorOverride()
    {
        isAnchorOverridden = false;
        anchorGravityMultiplier = 1f;
        lostSurfaceTimer = 0f;
    }

    public void BreakAnchorControl(float duration = 1.0f)
    {
        breakerOverrideTimer = duration;
        ClearAnchorOverride();
    }

    // ────────────────────────────────────────
    //  에디터 디버그 기즈모
    // ────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isAnchorOverridden ? Color.cyan : Color.red;
        Gizmos.DrawRay(transform.position, currentGravityDir * 1.5f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        foreach (var dir in SixDirs)
            Gizmos.DrawRay(transform.position, dir * detectRange);

        Gizmos.color = onSurface ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
