using UnityEngine;

public class UziShooter : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    public Transform barrel;

    [Header("── 반동 ──")]
   // public ArmRecoil recoil;
    public CameraShake cameraShake;

    [Header("── 총알 설정 ──")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 100f;
    public float bulletDamage = 20f;

    [Header("── 발사 설정 ──")]
    [Tooltip("초당 발사 횟수")]
    public float fireRate = 12f;

    [Tooltip("탄창 용량")]
    public int magazineSize = 40;

    [Tooltip("재장전")]
    public Animator reloadAnimator;
    private bool isReloading = false;

    [Tooltip("현재 탄약")]
    public int currentAmmo;

    [Header("── 이펙트 ──")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;

    [Header("── 오브젝트 풀 ──")]
    public int poolSize = 40;

    private float nextFireTime = 0f;

    private GameObject[] pool;
    private int poolIndex = 0;

    private AudioSource audioSource;

    void Start()
    {

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        InitPool();
    }

    void Update()
    {
        if (KeyManager.instance == null)
            return;

        if (!KeyManager.instance.keys.ContainsKey("Attack"))
            return;

        // Uzi는 자동 연사
        if (Input.GetKey(KeyManager.instance.keys["Attack"]))
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
            }
        }

        // if(magazineSize <= 0)
        // {
        //     Reload();
        //     magazineSize = 30;
        // }
    }

    void Shoot()
    {
        magazineSize--;
        nextFireTime = Time.time + (1f / fireRate);

        SpawnBullet();

        // 총구 이펙트
        //muzzleFlash?.Play();

        // 발사음
        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // 반동
        //  recoil?.TriggerRecoil();

        // 카메라 흔들림
        cameraShake?.Shake(0.04f, 0.04f);
    }

    void SpawnBullet()
    {
        if (bulletPrefab == null || barrel == null)
            return;

        GameObject bulletObj = GetBulletFromPool();

        bulletObj.transform.position = barrel.position;
        bulletObj.transform.rotation = barrel.rotation;

        bulletObj.SetActive(true);

        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Init(
                barrel.forward,
                bulletSpeed,
                bulletDamage
            );
        }
    }

    // ─────────────────────────────
    // 탄창
    // ─────────────────────────────

    public void Reload()
    {
        reloadAnimator?.SetTrigger("reload");

        Debug.Log("Uzi 재장전!");
    }

    // public bool IsEmpty()
    // {
    //     return currentAmmo <= 0;
    // }

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
        // 풀을 사용하지 않는 경우
        if (pool == null)
        {
            return Instantiate(bulletPrefab);
        }

        // 비활성화된 총알 찾기
        for (int i = 0; i < poolSize; i++)
        {
            int index = (poolIndex + i) % poolSize;

            if (!pool[index].activeInHierarchy)
            {
                poolIndex = (index + 1) % poolSize;

                return pool[index];
            }
        }

        // 모든 총알이 사용 중이면 가장 오래된 총알 재사용
        GameObject bullet = pool[poolIndex];

        bullet.SetActive(false);

        poolIndex = (poolIndex + 1) % poolSize;

        return bullet;
    }
}