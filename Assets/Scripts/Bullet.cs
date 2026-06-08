using UnityEngine;

public class Bullet : MonoBehaviour
{
[Header("총알 설정")]
    public float speed       = 120f;    // 총알 속도 (m/s)
    public float damage      = 75f;     // 피해량
    public float lifeTime    = 3f;      // 최대 생존 시간 (초)
    public float gravity     = 3f;      // 중력 영향 (0 = 직선 비행)
 
    [Header("충돌 이펙트")]
    public GameObject hitEffectPrefab;      // 충돌 파티클
    public GameObject bulletHolePrefab;     // 탄흔 데칼
    public TrailRenderer bulletTrail;       // 총알 궤적
 
    private Vector3   velocity;
    private bool      hasHit = false;
 
    // ── 초기화 (DoubleMagnumAnimator 에서 호출)
    public void Init(Vector3 direction, float spd, float dmg)
    {
        speed  = spd;
        damage = dmg;
        velocity = direction.normalized * speed;
    }
 
    void Start()
    {
        // Init 없이 사용할 때 기본값
        if (velocity == Vector3.zero)
            velocity = transform.forward * speed;
 
        Destroy(gameObject, lifeTime);
    }
 
    void Update()
    {
        if (hasHit) return;
 
        // 중력 적용
        velocity += Vector3.down * gravity * Time.deltaTime;
 
        // 이동
        Vector3 move = velocity * Time.deltaTime;
 
        // ── 이동 중 레이캐스트로 관통 체크 (빠른 총알 터널링 방지)
        if (Physics.Raycast(transform.position, move.normalized,
            out RaycastHit hit, move.magnitude + 0.1f))
        {
            OnHit(hit);
        }
        else
        {
            transform.position += move;
        }
 
        // 총알 방향을 속도 방향으로 회전
        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity);
    }
 
    void OnHit(RaycastHit hit)
    {
        hasHit = true;
 
        // 피해 적용
        //hit.collider.GetComponent<IDamageable>()?.TakeDamage(damage);
 
        // 물리 밀기
        hit.collider.GetComponent<Rigidbody>()
            ?.AddForce(-hit.normal * 300f);
 
        // 충돌 파티클
        if (hitEffectPrefab)
        {
            var fx = Instantiate(hitEffectPrefab,
                hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 2f);
        }
 
        // 탄흔
        if (bulletHolePrefab)
        {
            var hole = Instantiate(bulletHolePrefab,
                hit.point + hit.normal * 0.001f,
                Quaternion.LookRotation(hit.normal));
            // 탄흔을 맞은 오브젝트에 붙이기 (움직이는 적에도 따라감)
            hole.transform.SetParent(hit.collider.transform);
            Destroy(hole, 8f);
        }
 
        // 트레일 남기고 오브젝트 제거
        if (bulletTrail)
        {
            bulletTrail.transform.SetParent(null); // 트레일 분리
            Destroy(bulletTrail.gameObject, bulletTrail.time + 0.1f);
        }
 
        Destroy(gameObject);
    }
 
    // 벽/바닥에 박혔을 때 트리거도 처리
    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        var fakeHit = new RaycastHit();
        //other.GetComponent<IDamageable>()?.TakeDamage(damage);
        Destroy(gameObject);
    }
}
