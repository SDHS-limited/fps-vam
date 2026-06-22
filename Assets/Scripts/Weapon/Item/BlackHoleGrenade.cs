using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BlackHoleGrenade : MonoBehaviour
{
    [Header("수류탄 폭발 설정")]
    public float explosionDelay = 3f;   // 투척 후 폭발까지의 시간 (초)
    public float pullRadius = 10f;      // 적을 끌어당기는 감지 반경
    public float pullForce = 8f;        // 적을 끌어당기는 힘
    public float explosionRadius = 6f;  // 실제 대미지가 들어가는 폭발 반경
    public float explosionDamage = 100f; // 폭발 대미지

    [Header("시각 효과(VFX) 설정")]
    public GameObject explosionParticlePrefab; // 폭발 파티클 프리팹을 연결할 변수
    public float particleDestroyDelay = 2f;    // 파티클이 사라지기까지의 시간

    void Start()
    {
        // 생성되자마자 타이머 코루틴 시작
        StartCoroutine(GrenadeRoutine());
    }

    IEnumerator GrenadeRoutine()
    {
        float timer = 0f;

        // 지정된 시간 동안 매 프레임 적을 끌어당김
        while (timer < explosionDelay)
        {
            PullEnemies();
            timer += Time.deltaTime;
            yield return null;
        }

        // 타이머가 끝나면 폭발
        Explode();
    }

    void PullEnemies()
    {
        // 당기는 반경 내의 모든 콜라이더 감지
        Collider[] colliders = Physics.OverlapSphere(transform.position, pullRadius);
        foreach (Collider col in colliders)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 수류탄 중심을 향하는 방향 벡터 계산
                Vector3 pullDir = (transform.position - enemy.transform.position).normalized;
                
                // 적의 위치를 수류탄 쪽으로 강제 이동
                enemy.transform.position += pullDir * pullForce * Time.deltaTime;
            }
        }
    }

    void Explode()
    {
        // 폭발 반경 내의 적들에게 대미지 적용
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 기존 Enemy 스크립트의 타격 함수 호출
                enemy.TakeDamage(explosionDamage);
            }
        }

        // 2. 폭발 파티클 생성 및 재생
        if (explosionParticlePrefab != null)
        {
            // 수류탄의 현재 위치에 파티클 프리팹 생성
            GameObject effectInstance = Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
            
            // 재생이 끝난 파티클이 메모리에 쌓이지 않도록 일정 시간 뒤에 파괴
            Destroy(effectInstance, particleDestroyDelay);
        }

        // 3. 수류탄 자신을 씬에서 제거
        Destroy(gameObject);
    }

    // 유니티 에디터에서 당기는 범위와 폭발 범위를 시각적으로 확인하기 위한 기즈모
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pullRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}