using UnityEngine;

public class ShotgunShooter : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    // 샷건은 보통 양손으로 하나의 총을 잡으므로 총구(Barrel)를 하나만 사용합니다.
    public Transform barrel; 

    [Header("── 팔 반동 (양팔 동시 적용) ──")]
    public ArmRecoil recoilLeft;
    public ArmRecoil recoilRight;
    public CameraShake cameraShake;

    [Header("── 총알 및 산탄 설정 ──")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 120f;
    public float bulletDamage = 15f; // 펠릿 하나당 데미지 (15 * 8 = 총 120 데미지)
    public int pelletCount = 8;      // 한 번에 발사될 펠릿(산탄) 수
    public float spreadAngle = 7f;   // 탄퍼짐 각도 (클수록 넓게 퍼짐)

    [Header("── 발사 설정 ──")]
    public float fireRate = 0.25f;    // 샷건은 매그넘보다 연사속도가 느림

    [Header("── 이펙트 ──")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;

    [Header("── 오브젝트 풀 ──")]
    // 샷건은 한 번에 8개씩 발사하므로 풀 사이즈를 넉넉하게 잡아야 합니다.
    public int poolSize = 100; 

    private float nextFireTime = 0f;
    private GameObject[] pool;
    private int poolIndex = 0;
    private AudioSource audioSource;

    [Header("── 선입력(Input Buffer) 설정 ──")]
    public float inputBufferWindow = 0.15f; // 0.15초 안에 누른 클릭은 예약해줌
    private float inputBufferTimer = 0f;    // 현재 남은 선입력 시간

    void Start()
    {
        audioSource = GetComponentInParent<AudioSource>();
        InitPool();
    }

    void Update()
    {
// 1. 마우스를 클릭하면 선입력 타이머를 최대로 채움 (예약 접수)
        if (Input.GetButtonDown("Fire1"))
        {
            inputBufferTimer = inputBufferWindow;
        }

        // 2. 선입력 타이머가 남아있고, 발사 쿨타임이 다 찼다면 발사!
        if (inputBufferTimer > 0f && Time.time >= nextFireTime)
        {
            Shoot();
            inputBufferTimer = 0f; // 발사했으므로 예약 초기화
            //Debug.Log("샷건 발사! 다음 발사 가능 시간: " + nextFireTime);
        }

        // 3. 매 프레임마다 선입력 타이머 감소 (예약 시간 만료 처리)
        if (inputBufferTimer > 0f)
        {
            inputBufferTimer -= Time.deltaTime;
        }

        // (선택) 쿨타임 중 클릭 피드백 
        // 선입력이 아예 닿지 않는 너무 이른 시점에 클릭했을 때만 로그 출력
        if (Input.GetButtonDown("Fire1") && Time.time < nextFireTime - inputBufferWindow)
        {
            //Debug.Log("샷건은 연사속도가 느립니다. 잠시 기다려주세요.");
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        // pelletCount 만큼 총알을 동시에 생성하여 흩뿌립니다.
        for (int i = 0; i < pelletCount; i++)
        {
            SpawnBullet(barrel);
        }

//        muzzleFlash?.Play();
        audioSource?.PlayOneShot(shootSound);

        // ★ 샷건은 묵직하므로 양팔에 동시에 반동을 줍니다.
        recoilLeft?.TriggerRecoil();
        recoilRight?.TriggerRecoil();
        
        // 매그넘보다 더 크고 강하게 카메라 흔들림 적용
        cameraShake?.Shake(0.3f, 0.2f); 
    }

    void SpawnBullet(Transform spawnPoint)
    {
        if (bulletPrefab == null || spawnPoint == null)
            return;

        GameObject bulletObj = GetBulletFromPool();

        float randomX = Random.Range(-spreadAngle, spreadAngle);
        float randomY = Random.Range(-spreadAngle, spreadAngle);

        Quaternion spreadRotation =
            Quaternion.Euler(randomX, randomY, 0);

        Vector3 fireDirection =
            spreadRotation * spawnPoint.forward;

        bulletObj.transform.SetPositionAndRotation(
            spawnPoint.position,
            Quaternion.LookRotation(fireDirection)
        );

        Bullet bullet = bulletObj.GetComponent<Bullet>();

        bullet.Init(fireDirection, bulletSpeed, bulletDamage);

        bulletObj.SetActive(true);
    }

    void InitPool()
    {
        if (poolSize <= 0 || bulletPrefab == null) return;
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
            return Instantiate(bulletPrefab);
    
        for (int i = 0; i < poolSize; i++)
        {
            int idx = (poolIndex + i) % poolSize;
    
            if (!pool[idx].activeInHierarchy)
            {
                poolIndex = (idx + 1) % poolSize;
                return pool[idx];
            }
        }
    
        // 풀이 부족하면 새로 생성
        return Instantiate(bulletPrefab);
    }
}