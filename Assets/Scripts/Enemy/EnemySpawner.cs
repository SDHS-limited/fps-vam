using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 기반 적 스포너
/// - 시간이 지날수록 스폰 간격이 짧아짐 (속도 증가)
/// - 소형(Runner) / 대형(Brute) 혼합 스폰
/// - 천장 홀(SpawnHole) 위치에서 낙하 생성
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("적 프리팹")]
    [Tooltip("소형 변이체 프리팹")]
    public GameObject runnerPrefab;
    [Tooltip("대형 변이체 프리팹")]
    public GameObject brutePrefab;

    [Header("스폰 위치")]
    [Tooltip("스폰 홀 Transform (천장 구멍 위치)")]
    public Transform spawnHole;
    [Tooltip("스폰 위치 랜덤 오프셋 반경 (홀 중심에서 퍼지는 범위)")]
    public float spawnRadius = 0.5f;

    [Header("스폰 속도 설정")]
    [Tooltip("초기 스폰 간격 (초)")]
    public float initialInterval = 3.0f;
    [Tooltip("최소 스폰 간격 — 이 값 이하로는 내려가지 않음 (초)")]
    public float minInterval = 0.4f;
    [Tooltip("몇 초마다 스폰 간격을 줄일지")]
    public float difficultyTickInterval = 10.0f;
    [Tooltip("매 틱마다 간격을 줄이는 비율 (0.1 = 10% 감소)")]
    [Range(0.01f, 0.5f)]
    public float intervalDecreaseRate = 0.1f;

    [Header("대형 적 비율")]
    [Tooltip("전체 스폰 중 대형 적(Brute)이 나올 확률 (0~1)")]
    [Range(0f, 1f)]
    public float bruteChance = 0.2f;
    [Tooltip("시간이 지날수록 대형 적 확률 증가량 (매 틱마다)")]
    [Range(0f, 0.1f)]
    public float bruteChanceIncrease = 0.02f;
    [Tooltip("대형 적 최대 확률")]
    [Range(0f, 1f)]
    public float maxBruteChance = 0.5f;

    [Header("동시 존재 제한")]
    [Tooltip("씬에 동시에 존재할 수 있는 최대 적 수 (0 = 무제한)")]
    public int maxEnemyCount = 30;

    [Header("낙하 설정")]
    [Tooltip("스폰 시 위쪽으로 살짝 튀어나오는 힘 (Rigidbody 사용 시)")]
    public float spawnDropForce = 2.0f;

    // 내부 상태
    private float currentInterval;
    private float spawnTimer;
    private float difficultyTimer;
    private bool isSpawning = false;
    private List<GameObject> activeEnemies = new List<GameObject>();

    // 외부에서 읽을 수 있는 상태
    public float CurrentInterval => currentInterval;
    public int ActiveEnemyCount => activeEnemies.Count;

    // 이벤트 (선택 사항 — UI 연동 등에 활용)
    public System.Action<GameObject> OnEnemySpawned;
    public System.Action<float> OnIntervalChanged;

    void Start()
    {
        currentInterval = initialInterval;
        spawnTimer = currentInterval;
        difficultyTimer = 0f;

        // 자동 시작 — 필요하면 StartSpawning() / StopSpawning()으로 제어
        StartSpawning();
    }

    void Update()
    {
        if (!isSpawning) return;

        float dt = Time.deltaTime;

        // ── 난이도 틱: 일정 시간마다 스폰 간격 감소
        difficultyTimer += dt;
        if (difficultyTimer >= difficultyTickInterval)
        {
            difficultyTimer = 0f;
            IncreaseDifficulty();
        }

        // ── 스폰 타이머
        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            spawnTimer = currentInterval;
            TrySpawnEnemy();
        }
    }

    // ────────────────────────────────────────
    //  스폰 제어
    // ────────────────────────────────────────

    /// <summary>스폰 시작</summary>
    public void StartSpawning()
    {
        isSpawning = true;
    }

    /// <summary>스폰 정지 (웨이브 클리어, 게임 오버 등)</summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }

    /// <summary>스포너 완전 초기화 (스테이지 재시작 시)</summary>
    public void ResetSpawner()
    {
        StopSpawning();
        currentInterval = initialInterval;
        spawnTimer = currentInterval;
        difficultyTimer = 0f;
        bruteChance = Mathf.Clamp(bruteChance, 0f, maxBruteChance);

        // 현재 활성 적 모두 제거
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();
    }

    // ────────────────────────────────────────
    //  난이도 증가
    // ────────────────────────────────────────

    private void IncreaseDifficulty()
    {
        // 스폰 간격 감소
        currentInterval = Mathf.Max(
            minInterval,
            currentInterval * (1f - intervalDecreaseRate)
        );

        // 대형 적 확률 증가
        bruteChance = Mathf.Min(maxBruteChance, bruteChance + bruteChanceIncrease);

        OnIntervalChanged?.Invoke(currentInterval);

        Debug.Log($"[Spawner] 난이도 증가 — 간격: {currentInterval:F2}s / 브루트 확률: {bruteChance:P0}");
    }

    // ────────────────────────────────────────
    //  적 생성
    // ────────────────────────────────────────

    private void TrySpawnEnemy()
    {
        // 최대 적 수 초과 시 스폰 스킵
        CleanupDeadEnemies();
        if (maxEnemyCount > 0 && activeEnemies.Count >= maxEnemyCount)
            return;

        // 스폰 위치 계산 (홀 중심 + 랜덤 오프셋)
        Vector3 spawnPos = GetSpawnPosition();

        // 소형 / 대형 결정
        bool spawnBrute = Random.value < bruteChance;
        GameObject prefab = spawnBrute ? brutePrefab : runnerPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[Spawner] {(spawnBrute ? "Brute" : "Runner")} 프리팹이 없습니다.");
            return;
        }

        // 생성
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);

        // 낙하 물리 적용 (Rigidbody가 있을 경우)
        ApplyDropForce(enemy);

        // 스폰 이벤트
        OnEnemySpawned?.Invoke(enemy);

        Debug.Log($"[Spawner] {(spawnBrute ? "BRUTE" : "Runner")} 스폰 @ {spawnPos} | 현재 적 수: {activeEnemies.Count}");
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnHole == null)
        {
            // 스폰홀 미지정 시 스포너 오브젝트 위치 사용
            return transform.position;
        }

        // 홀 중심에서 반경 내 랜덤 위치 (XZ 평면)
        Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
        return spawnHole.position + new Vector3(offset2D.x, 0f, offset2D.y);
    }

    private void ApplyDropForce(GameObject enemy)
    {
        if (enemy.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // 스폰홀에서 아래로 낙하하는 방향 (중력 방향에 따라 조정 필요)
            rb.linearVelocity = Vector3.down * spawnDropForce;
        }
    }

    // ────────────────────────────────────────
    //  유틸
    // ────────────────────────────────────────

    /// <summary>이미 파괴된 적 오브젝트를 리스트에서 제거</summary>
    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    /// <summary>외부에서 특정 적이 죽었을 때 호출 (적 스크립트에서 호출 권장)</summary>
    public void OnEnemyDied(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
    }

    // ────────────────────────────────────────
    //  에디터 디버그
    // ────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (spawnHole == null) return;

        // 스폰 반경 시각화
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(spawnHole.position, spawnRadius);
        Gizmos.DrawSphere(spawnHole.position, 0.1f);
    }
}