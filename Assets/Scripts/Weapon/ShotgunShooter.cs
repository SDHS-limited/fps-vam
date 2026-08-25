using UnityEngine;

public class ShotgunShooter : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    public Transform barrel;

    [Header("── 팔 반동 ──")]
    public ArmRecoil recoilLeft;
    public ArmRecoil recoilRight;
    public CameraShake cameraShake;

    [Header("── 총알 및 산탄 설정 ──")]
    public GameObject bulletPrefab;

    [Tooltip("총알 속도")]
    public float bulletSpeed = 120f;

    [Tooltip("펠릿 하나당 데미지")]
    public float bulletDamage = 15f;

    [Tooltip("한 발당 펠릿 수")]
    public int pelletCount = 8;

    [Tooltip("최대 탄퍼짐 각도")]
    [Range(0f, 30f)]
    public float spreadAngle = 7f;

    [Tooltip("탄퍼짐 중심 집중도. 높을수록 중앙에 많이 모임")]
    [Range(0.1f, 5f)]
    public float spreadBias = 2f;

    [Header("── 발사 설정 ──")]
    public float fireRate = 0.25f;

    [Header("── 이펙트 ──")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;

    [Header("── 오브젝트 풀 ──")]
    public int poolSize = 100;

    [Header("── 선입력 ──")]
    public float inputBufferWindow = 0.15f;

    private float nextFireTime = 0f;
    private float inputBufferTimer = 0f;

    private GameObject[] pool;
    private int poolIndex = 0;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponentInParent<AudioSource>();
        InitPool();
    }

    void Update()
    {
        // 발사 입력
        if (Input.GetButtonDown("Fire1"))
        {
            inputBufferTimer = inputBufferWindow;
        }

        // 발사 가능
        if (inputBufferTimer > 0f && Time.time >= nextFireTime)
        {
            Shoot();

            inputBufferTimer = 0f;
        }

        // 선입력 감소
        if (inputBufferTimer > 0f)
        {
            inputBufferTimer -= Time.deltaTime;
        }
    }

    void Shoot()
    {
        if (barrel == null)
            return;

        nextFireTime = Time.time + fireRate;

        // ─────────────────────────────
        // 산탄 발사
        // ─────────────────────────────

        for (int i = 0; i < pelletCount; i++)
        {
            SpawnBullet(barrel);
        }

        // 총구 이펙트
        //muzzleFlash?.Play();

        // 사운드
        if (shootSound != null)
        {
            audioSource?.PlayOneShot(shootSound);
        }

        // 반동
        recoilLeft?.TriggerRecoil();
        recoilRight?.TriggerRecoil();

        // 카메라 흔들림
        cameraShake?.Shake(0.3f, 0.2f);
    }

    void SpawnBullet(Transform spawnPoint)
    {
        if (bulletPrefab == null || spawnPoint == null)
            return;

        GameObject bulletObj = GetBulletFromPool();

        // ─────────────────────────────
        // 정확한 원뿔형 산탄 생성
        // ─────────────────────────────

        Vector3 fireDirection = GetSpreadDirection(
            spawnPoint.forward,
            spreadAngle
        );

        // 총구 위치
        bulletObj.transform.position = spawnPoint.position;

        // 탄환 방향
        bulletObj.transform.rotation =
            Quaternion.LookRotation(fireDirection);

        // Bullet 초기화
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Init(
                fireDirection,
                bulletSpeed,
                bulletDamage
            );
        }

        // 마지막에 활성화
        bulletObj.SetActive(true);
    }

    Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        // 총구 방향을 기준으로 한 원형 랜덤 좌표
        Vector2 randomCircle = Random.insideUnitCircle;

        // 중심에 얼마나 집중시킬지 결정
        float distance =
            Mathf.Pow(Random.value, spreadBias);

        randomCircle *= distance;

        // 각도 → 반지름
        float spreadRadius =
            Mathf.Tan(angle * Mathf.Deg2Rad);

        randomCircle *= spreadRadius;

        // 총구 기준 좌표계 생성
        Vector3 right = Vector3.Cross(
            Vector3.up,
            forward
        ).normalized;

        // 총구가 위쪽을 향하는 경우를 위한 보정
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(
                Vector3.forward,
                forward
            ).normalized;
        }

        Vector3 up = Vector3.Cross(
            forward,
            right
        ).normalized;

        // 최종 방향
        Vector3 direction =
            forward +
            right * randomCircle.x +
            up * randomCircle.y;

        return direction.normalized;
    }

    // ─────────────────────────────
    // 오브젝트 풀
    // ─────────────────────────────

    void InitPool()
    {
        if (poolSize <= 0 || bulletPrefab == null)
            return;

        pool = new GameObject[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = Instantiate(bulletPrefab);

            pool[i].SetActive(false);
        }
    }

    GameObject GetBulletFromPool()
    {
        if (pool == null)
        {
            return Instantiate(bulletPrefab);
        }

        for (int i = 0; i < poolSize; i++)
        {
            int index =
                (poolIndex + i) % poolSize;

            if (!pool[index].activeInHierarchy)
            {
                poolIndex =
                    (index + 1) % poolSize;

                return pool[index];
            }
        }

        // 풀이 부족하면 추가 생성
        return Instantiate(bulletPrefab);
    }
}