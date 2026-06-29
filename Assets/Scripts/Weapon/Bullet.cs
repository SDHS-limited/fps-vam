// 수정된 Bullet.cs
using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float speed    = 120f;
    public float damage   = 75f;
    public float lifeTime = 3f;
    public float gravity  = 2f;

    [Header("충돌 이펙트")]
    public GameObject hitEffectPrefab;
    public GameObject bulletHolePrefab;

    [Header("땅 설정")]
    public string groundTag = "Ground";
    public string UIWall = "Ground2";

    [Header("충돌 마스크 (중요!)")]
    [Tooltip("총알이 맞출 대상만 체크하세요. (예: Default, Enemy, Ground 등. Player는 체크 해제!)")]
    public LayerMask hitMask = ~0; // 기본값: 모든 레이어와 충돌 (Inspector에서 세팅 필요)

    private Vector3 velocity;
    private bool    hasHit = false;

    public void Init(Vector3 direction, float spd, float dmg)
    {
        speed    = spd;
        damage   = dmg;
        velocity = direction.normalized * speed;
        hasHit   = false;
    }

    void OnEnable()
    {
        hasHit = false;
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    void OnDisable()
    {
        CancelInvoke();
        velocity = Vector3.zero;
    }

    void Update()
    {
        if (hasHit) return;

        velocity += Vector3.down * gravity * Time.deltaTime;
        Vector3 move = velocity * Time.deltaTime;

        // ★ 수정됨: hitMask를 추가하여 플레이어나 총알끼리의 충돌을 무시합니다.
        if (Physics.Raycast(transform.position, move.normalized, out RaycastHit hit, move.magnitude + 0.05f, hitMask))
        {
            if (hit.collider.CompareTag(groundTag) || hit.collider.CompareTag(UIWall))
                OnGroundHit(hit);   // 땅, 벽 → 파티클만
            else
                OnHit(hit);         // 그 외(적) → 데미지 처리

            return;
        }

        transform.position += move;

        if (velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(velocity);
    }

    // ── 땅에 닿았을 때: 총알 숨기고 파티클만 재생
    void OnGroundHit(RaycastHit hit)
    {
        hasHit = true;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        if (hitEffectPrefab)
        {
            var fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 2f);
        }

        Invoke(nameof(ReturnToPool), 0.1f);
    }

    // ── 일반 충돌
void OnHit(RaycastHit hit)
    {
        hasHit = true;

        // ★ 테스트용 로그: 총알이 대체 뭘 때렸는지 유니티 콘솔창(Console)에 출력합니다.
        Debug.Log("<color=yellow>총알이 때린 오브젝트: " + hit.collider.gameObject.name + "</color>");

        CubeFace face = hit.collider.GetComponentInParent<CubeFace>();
        
        // ★ GetComponent를 GetComponentInParent로 변경
        // (자식 콜라이더에 맞아도 부모에 있는 Enemy 스크립트를 찾아냅니다)
        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log("<color=red>적중! 적에게 데미지 들어감</color>");
        }

        if (hitEffectPrefab)
        {
            var fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 2f);
        }

        ReturnToPool();
    }

    void ReturnToPool()
    {
        // 풀 반환 전 Renderer 다시 켜두기 (재사용 대비)
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        gameObject.SetActive(false);
    }

    // ★★★ 삭제됨: 총알을 자폭하게 만들던 OnCollisionEnter 함수 통째로 삭제 ★★★
}