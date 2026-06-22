using UnityEngine;

/// <summary>
/// 적 전용 커스텀 중력
/// 큐브 6면 외부 표면 감지 → 가장 가까운 면에 붙어 걸음
/// 개선 사항:
///   1. 가장 가까운 면 우선순위 선택 (거리 기반 정렬)
///   2. 중력 방향 급격한 전환 방지 (Slerp 보간)
///   3. 스폰홀(구멍) 추락 방지 레이어 마스크
///   4. 표면에서 떨어진 경우 마지막 중력 방향 유지
///   5. 이동 스크립트와 연동하는 GravityDir 프로퍼티
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
    public float spawnDropDelay = 0.5f; // 이 시간 동안은 천장에 붙지 않음
    private float spawnTimer = 0f;

    // ── 내부 상태
    Rigidbody rb;
    Vector3 currentGravityDir = Vector3.down;   // 실제 적용 중인 보간된 중력 방향
    Vector3 targetGravityDir  = Vector3.down;   // 감지된 목표 중력 방향
    float lostSurfaceTimer = 0f;
    bool onSurface = false;

    // 6방향 — 고정 배열로 GC 없이 재사용
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
    /// <summary>현재 보간된 중력 방향 (이동 스크립트에서 활용)</summary>
    public Vector3 GravityDir => currentGravityDir;
    /// <summary>현재 표면에 붙어 있는지 여부</summary>
    public bool IsOnSurface => onSurface;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        // 스폰 직후 일정 시간 동안은 표면 감지를 건너뛰고 강제 하강
        if (spawnTimer < spawnDropDelay)
        {
            spawnTimer += Time.fixedDeltaTime;
            
            // 목표 중력을 무조건 월드 아래쪽으로 고정
            targetGravityDir = Vector3.down;
            
            // 중력 방향 보간 (부드러운 전환)
            currentGravityDir = Vector3.Slerp(
                currentGravityDir,
                targetGravityDir,
                Time.fixedDeltaTime * gravityTurnSpeed
            ).normalized;
        }
        else
        {
            // 스폰 딜레이가 끝나면 정상적으로 6방향 표면 감지 시작
            DetectSurface();
        }

        ApplyGravity();
        AlignToSurface();
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

            // 가장 가까운 면 선택
            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                bestNormal  = hit.normal;
                found       = true;
            }
        }

        if (found)
        {
            // 감지 성공 — 목표 중력 방향 갱신
            targetGravityDir  = -bestNormal;
            lostSurfaceTimer  = 0f;
            onSurface         = true;
        }
        else
        {
            // 감지 실패 — 타임아웃까지 마지막 방향 유지
            lostSurfaceTimer += Time.fixedDeltaTime;
            if (lostSurfaceTimer >= lostSurfaceTimeout)
            {
                onSurface = false;
                // 완전히 이탈 시 가장 가까운 큐브 중심 방향으로 fallback
                targetGravityDir = GetFallbackGravity();
            }
        }

        // 중력 방향 부드럽게 보간
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
        rb.AddForce(currentGravityDir * gravityStrength, ForceMode.Acceleration);
    }

    // ────────────────────────────────────────
    //  3. 표면 법선에 맞게 회전 정렬
    // ────────────────────────────────────────
    void AlignToSurface()
    {
        // 현재 up 벡터를 중력 반대 방향으로 맞춤
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
    //     스포너(부모 큐브) 중심 방향으로 당김
    // ────────────────────────────────────────
    Vector3 GetFallbackGravity()
    {
        // 씬에 CubeCenter 태그 오브젝트가 있으면 그 방향으로
        GameObject cubeCenter = GameObject.FindWithTag("CubeCenter");
        if (cubeCenter != null)
        {
            Vector3 dir = (cubeCenter.transform.position - transform.position).normalized;
            return dir;
        }
        // 없으면 월드 다운 유지
        return Vector3.down;
    }

    // ────────────────────────────────────────
    //  5. 외부에서 중력 방향 강제 지정
    //     (게임 이벤트로 중력 전환 트리거할 때 활용)
    // ────────────────────────────────────────
    public void ForceGravityDirection(Vector3 dir)
    {
        targetGravityDir = dir.normalized;
    }

    // ────────────────────────────────────────
    //  에디터 디버그 기즈모
    // ────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // 중력 방향 (빨간 화살표)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, currentGravityDir * 1.5f);

        // 감지 레이 6방향 (노란색)
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        foreach (var dir in SixDirs)
            Gizmos.DrawRay(transform.position, dir * detectRange);

        // 표면 감지 상태
        Gizmos.color = onSurface ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}