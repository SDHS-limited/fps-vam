// Bullet.cs
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

        if (Physics.Raycast(transform.position, move.normalized,
            out RaycastHit hit, move.magnitude + 0.05f))
        {
            if (hit.collider.CompareTag(groundTag))
                OnGroundHit(hit);   // 땅 → 파티클만
            else if(hit.collider.CompareTag(UIWall))
                OnGroundHit(hit);
            else
                OnHit(hit);         // 그 외 → 기존 처리
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

        // 총알 메시만 숨기기 (파티클은 별도 오브젝트라 영향 없음)
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        // 파티클 재생
        if (hitEffectPrefab)
        {
            var fx = Instantiate(hitEffectPrefab,
                hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 2f);
        }

        // 파티클 재생 후 풀 반환
        Invoke(nameof(ReturnToPool), 0.1f);
    }

    // ── 일반 충돌
    void OnHit(RaycastHit hit)
    {
        hasHit = true;

       // hit.collider.GetComponent<IDamageable>()?.TakeDamage(damage);
        hit.collider.GetComponent<Rigidbody>()?.AddForce(-hit.normal * 300f);

        if (hitEffectPrefab)
        {
            var fx = Instantiate(hitEffectPrefab,
                hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 2f);
        }

        if (bulletHolePrefab)
        {
            var hole = Instantiate(bulletHolePrefab,
                hit.point + hit.normal * 0.001f,
                Quaternion.LookRotation(hit.normal));
            hole.transform.SetParent(hit.collider.transform);
            Destroy(hole, 8f);
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
}