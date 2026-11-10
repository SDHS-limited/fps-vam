// DoubleMagnumShooter.cs — 왼쪽/오른쪽 번갈아 발사 + 팔 개별 반동
using UnityEngine;
 
public class DoubleMagnumShooter : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    public Transform barrelLeft;
    public Transform barrelRight;
 
    [Header("── 팔 반동 (각각 연결) ──")]
    public ArmRecoil recoilLeft;    // L 오브젝트의 ArmRecoil
    public ArmRecoil recoilRight;   // shoulder.R 오브젝트의 ArmRecoil
    public CameraShake cameraShake;
 
    [Header("── 총알 설정 ──")]
    public GameObject bulletPrefab;
    public float bulletSpeed  = 120f;
    public float bulletDamage = 75f;

    [Header("── 중앙 조준 ──")]
    public Camera aimCamera;
    [Min(1f)] public float aimDistance = 300f;
 
    [Header("── 발사 설정 ──")]
    public float fireRate = 0.55f;
 
    [Header("── 이펙트 ──")]
    // public ParticleSystem muzzleFlashLeft;
    // public ParticleSystem muzzleFlashRight;
    public MagnumMuzzleFlash muzzleFlashPrefab;
    public AudioClip      shootSound;
    [Range(0f, 1f)] public float shotVolume = 0.7f;
 
    [Header("── 오브젝트 풀 ──")]
    public int poolSize = 20;
 
    private float        nextFireTime  = 0f;
    private int          currentBarrel = 0;   // 0=왼쪽, 1=오른쪽
    private GameObject[] pool;
    private int          poolIndex = 0;
    public AudioSource  audioSource;
    private MagnumMuzzleFlash flashLeft;
    private MagnumMuzzleFlash flashRight;
 
    void Start()
    {
        if (aimCamera == null) aimCamera = GetComponentInParent<Camera>();
        if (aimCamera == null) aimCamera = Camera.main;
        // 부모가 아닌 현재 오브젝트에서 AudioSource를 찾고, 없다면 자동으로 추가합니다.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        InitPool(); //
        if (muzzleFlashPrefab != null)
        {
            flashLeft = Instantiate(muzzleFlashPrefab);
            flashRight = Instantiate(muzzleFlashPrefab);
        }
    }
 
    void Update()
    {
        if (Time.timeScale == 0f || KeyManager.instance == null) return;
        if (Input.GetKeyDown(KeyManager.instance.keys["Attack"]) && Time.time >= nextFireTime)
            Shoot();
            
    }
 
    void Shoot()
    {
        nextFireTime = Time.time + fireRate;
 
        bool useLeft = (currentBarrel == 0);
        currentBarrel = 1 - currentBarrel;
 
        Transform barrel = useLeft ? barrelLeft : barrelRight;
 
        SpawnBullet(barrel);
 
        (useLeft ? flashLeft : flashRight)?.Play(barrel);
        // (useLeft ? muzzleFlashLeft : muzzleFlashRight)?.Play();
        if (shootSound != null) audioSource.PlayOneShot(shootSound, shotVolume);
 
        // ★ 발사한 쪽 팔만 반동
        if (useLeft)  recoilLeft?.TriggerRecoil();
        else          recoilRight?.TriggerRecoil();
        cameraShake?.Shake(0.1f, 0.1f);
    }
 
    void OnDisable()
    {
        flashLeft?.Hide();
        flashRight?.Hide();
    }

    void OnDestroy()
    {
        if (flashLeft != null) Destroy(flashLeft.gameObject);
        if (flashRight != null) Destroy(flashRight.gameObject);
    }

    void SpawnBullet(Transform barrel)
    {
        if (bulletPrefab == null || barrel == null) return;
 
        Vector3 spawnPosition = barrel.position;
        Vector3 target = GetAimPoint(barrel);
        // A muzzle pushed through nearby cover must not spawn a bullet beyond it.
        if (aimCamera != null)
        {
            Vector3 toMuzzle = spawnPosition - aimCamera.transform.position;
            if (TryAimHit(new Ray(aimCamera.transform.position, toMuzzle), toMuzzle.magnitude, out RaycastHit cover))
                spawnPosition = cover.point - toMuzzle.normalized * 0.01f;
        }
        Vector3 direction = (target - spawnPosition).normalized;
        if (direction.sqrMagnitude < 0.001f) direction = barrel.forward;
        GameObject bulletObj = GetBulletFromPool();
        bulletObj.transform.position = spawnPosition;
        bulletObj.transform.rotation = Quaternion.LookRotation(direction);
        bulletObj.SetActive(true);
 
        bulletObj.GetComponent<Bullet>()?.Init(direction, bulletSpeed,
            GetComponentInParent<GravityPlayerController>()?.GetBulletDamage(bulletDamage) ?? bulletDamage,
            transform.root, true);
    }

    Vector3 GetAimPoint(Transform barrel)
    {
        if (aimCamera == null) aimCamera = GetComponentInParent<Camera>();
        if (aimCamera == null) aimCamera = Camera.main;
        Ray ray = aimCamera != null
            ? aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(barrel.position, barrel.forward);
        return TryAimHit(ray, aimDistance, out RaycastHit hit) ? hit.point : ray.GetPoint(aimDistance);
    }

    bool TryAimHit(Ray ray, float distance, out RaycastHit closest)
    {
        closest = default;
        var bullet = bulletPrefab != null ? bulletPrefab.GetComponent<Bullet>() : null;
        int mask = bullet != null ? bullet.hitMask.value : Physics.DefaultRaycastLayers;
        return ShotRaycast.Closest(ray, distance, mask, transform.root, out closest);
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
        if (pool == null) return Instantiate(bulletPrefab);
        for (int i = 0; i < poolSize; i++)
        {
            int idx = (poolIndex + i) % poolSize;
            if (!pool[idx].activeInHierarchy)
            {
                poolIndex = (idx + 1) % poolSize;
                return pool[idx];
            }
        }
        var obj = pool[poolIndex];
        obj.SetActive(false);
        poolIndex = (poolIndex + 1) % poolSize;
        return obj;
    }
}
 
