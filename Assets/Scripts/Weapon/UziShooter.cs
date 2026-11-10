using UnityEngine;
using UnityEngine.Audio;

public class UziShooter : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    public Transform barrel;

    [Header("── 반동 ──")]
   // public ArmRecoil recoil;
    public CameraShake cameraShake;

    [Header("── 총알 설정 ──")]
    public GameObject bulletPrefab;
    public Camera aimCamera;
    [Min(1f)] public float aimDistance = 300f;
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
    public MagnumMuzzleFlash muzzleFlashPrefab;
    public AudioClip shootSound;
    public AudioMixerGroup sfxMixer;
    [Range(0f, 1f)] public float shotVolume = 0.45f;

    [Header("── 오브젝트 풀 ──")]
    public int poolSize = 40;

    private float nextFireTime = 0f;

    private GameObject[] pool;
    private int poolIndex = 0;

    private AudioSource audioSource;
    private MagnumMuzzleFlash flash;

    void Start()
    {
        if (aimCamera == null) aimCamera = GetComponentInParent<Camera>();
        if (aimCamera == null) aimCamera = Camera.main;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.outputAudioMixerGroup = sfxMixer;
        if (muzzleFlashPrefab != null)
        {
            flash = Instantiate(muzzleFlashPrefab);
            flash.size *= 0.8f;
            flash.duration = 0.065f;
        }

        InitPool();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
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

        muzzleFlash?.Play();
        flash?.Play(barrel);

        // 발사음
        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound, shotVolume);
        }

        // 반동
        //  recoil?.TriggerRecoil();

        // 카메라 흔들림
        cameraShake?.Shake(0.04f, 0.04f);
    }

    void OnDisable() { flash?.Hide(); }
    void OnDestroy() { if (flash != null) Destroy(flash.gameObject); }

    void SpawnBullet()
    {
        if (bulletPrefab == null || barrel == null)
            return;

        GameObject bulletObj = GetBulletFromPool();

        Vector3 spawnPosition = barrel.position;
        Vector3 target = GetAimPoint();
        if (aimCamera != null)
        {
            Vector3 toMuzzle = spawnPosition - aimCamera.transform.position;
            if (TryAimHit(new Ray(aimCamera.transform.position, toMuzzle), toMuzzle.magnitude, out RaycastHit cover))
                spawnPosition = cover.point - toMuzzle.normalized * 0.01f;
        }
        Vector3 direction = (target - spawnPosition).normalized;
        if (direction.sqrMagnitude < 0.001f) direction = barrel.forward;
        bulletObj.transform.position = spawnPosition;
        bulletObj.transform.rotation = Quaternion.LookRotation(direction);

        bulletObj.SetActive(true);

        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Init(
                direction,
                bulletSpeed,
                bulletDamage,
                transform.root,
                true
            );
        }
    }

    Vector3 GetAimPoint()
    {
        Ray ray = aimCamera != null
            ? aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(barrel.position, barrel.forward);
        return TryAimHit(ray, aimDistance, out RaycastHit hit) ? hit.point : ray.GetPoint(aimDistance);
    }

    bool TryAimHit(Ray ray, float distance, out RaycastHit closest)
    {
        var bullet = bulletPrefab != null ? bulletPrefab.GetComponent<Bullet>() : null;
        int mask = bullet != null ? bullet.hitMask.value : Physics.DefaultRaycastLayers;
        return ShotRaycast.Closest(ray, distance, mask, transform.root, out closest);
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
