using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(GravityPlayerController), typeof(Rigidbody))]
public sealed class GravityShiftMotor : MonoBehaviour
{
    public KeyCode shiftKey = KeyCode.E;
    [Min(1f)] public float aimRange = 60f;
    [Range(360f, 1440f)] public float rotationSpeed = 1100f;
    [Min(0.1f)] public float inputCooldown = 0.3f;
    [Min(1f)] public float maximumShiftSpeed = 14f;
    [Header("Shift Feel")]
    [Range(0.08f, 0.5f)] public float quarterTurnDuration = 0.18f;
    [Range(0.12f, 0.6f)] public float halfTurnDuration = 0.28f;
    [Min(0f)] public float launchSpeed = 12f;
    [Min(1f)] public float maximumTravelSpeed = 18f;
    [Range(0f, 15f)] public float shiftFovKick = 7f;
    [Range(0f, 10f)] public float landingFovKick = 2f;
    public AudioClip shiftSound;
    public AudioClip landingSound;
    [Range(0f, 1f)] public float soundVolume = 0.55f;
    public LayerMask surfaceMask = Physics.DefaultRaycastLayers;
    public bool showHUD = true;

    public Vector3 GravityDirection { get; private set; } = Vector3.down;
    public bool IsTurning => viewPivot != null && Quaternion.Angle(viewPivot.localRotation, Quaternion.identity) > 1f;
    public int ShiftCount { get; private set; }
    private GravityPlayerController player;
    private Rigidbody body;
    private Camera view;
    private Transform viewPivot;
    private AudioSource shiftAudio;
    private float readyAt, nextPreview;
    private float turnElapsed, turnDuration, turnAngle, landingReadyAt, travelExpiresAt;
    private Quaternion viewRotationAtShift;
    private bool travelling;
    private RaycastHit[] hits = new RaycastHit[32];
    private TMP_Text status;
    private GameObject hud;
    private string displayed;
    private bool hasAim;
    private Vector3 aimNormal;

    void Awake()
    {
        player = GetComponent<GravityPlayerController>();
        body = GetComponent<Rigidbody>();
        view = GetComponentInChildren<Camera>();
        GravityDirection = -Cardinal(transform.up);
        if (view != null)
        {
            Transform cameraTransform = view.transform;
            viewPivot = new GameObject("Gravity View Pivot").transform;
            viewPivot.SetParent(cameraTransform.parent, false);
            cameraTransform.SetParent(viewPivot, false);
        }
        shiftAudio = gameObject.AddComponent<AudioSource>();
        shiftAudio.playOnAwake = false;
        shiftAudio.spatialBlend = 0f;
    }

    void Start()
    {
        if (!showHUD) return;
        hud = new GameObject("Gravity Shift HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hud.transform.SetParent(transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 35;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var label = new GameObject("Shift Status", typeof(RectTransform), typeof(CanvasRenderer));
        label.transform.SetParent(hud.transform, false);
        status = label.AddComponent<TextMeshProUGUI>();
        status.fontSize = 22; status.alignment = TextAlignmentOptions.Center;
        status.raycastTarget = false;
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f); rect.sizeDelta = new Vector2(760, 65);
        rect.anchoredPosition = new Vector2(0, -45);
    }

    void Update()
    {
        bool blocked = player.IsDead || Time.timeScale == 0f || Cursor.visible;
        if (status != null) status.enabled = !blocked;
        if (blocked || view == null) return;
        KeyCode key = KeyManager.instance != null && KeyManager.instance.keys.TryGetValue("GravityShift", out var mapped) ? mapped : shiftKey;
        if (Input.GetKeyDown(key)) TryShift(view.ViewportPointToRay(new Vector3(.5f, .5f, 0)));
        if (Time.unscaledTime < nextPreview) return;
        nextPreview = Time.unscaledTime + .1f;
        hasAim = TrySurface(view.ViewportPointToRay(new Vector3(.5f, .5f, 0)), out aimNormal, out _);
        string message = IsTurning ? "GRAVITY SHIFT - TURNING" : hasAim
            ? (Vector3.Dot(aimNormal, -GravityDirection) > .99f ? "CURRENT SURFACE" : "SHIFT [" + key + "] - " + Face(aimNormal))
            : "AIM AT A WALL OR CEILING - SHIFT [" + key + "]";
        if (status != null && displayed != message)
        {
            displayed = message; status.text = message;
            status.color = hasAim ? new Color(.25f, .9f, 1f) : new Color(.8f, .85f, .9f);
        }
    }

    void LateUpdate()
    {
        if (viewPivot == null || !isActiveAndEnabled || player.IsDead || Time.timeScale == 0f || turnElapsed >= turnDuration) return;
        turnElapsed = Mathf.Min(turnDuration, turnElapsed + Time.deltaTime);
        float t = turnElapsed / Mathf.Max(0.01f, turnDuration);
        viewPivot.localRotation = Quaternion.Slerp(viewRotationAtShift, Quaternion.identity, EaseOutSine(t));
    }

    public bool TryShift(Ray aim)
    {
        if (!isActiveAndEnabled || player.IsDead || Time.timeScale == 0f || Time.time < readyAt) return false;
        if (!TrySurface(aim, out var normal, out float distance) || Vector3.Dot(normal, -GravityDirection) > .99f) return false;
        Quaternion turn = FrameRotation(body.rotation * Vector3.up, normal);
        Vector3 tangentVelocity = Vector3.ProjectOnPlane(body.linearVelocity, -GravityDirection);
        Quaternion targetRotation = turn * body.rotation;
        turnAngle = Quaternion.Angle(body.rotation, targetRotation);
        turnDuration = Mathf.Lerp(quarterTurnDuration, halfTurnDuration, Mathf.InverseLerp(90f, 180f, turnAngle));
        turnDuration = Mathf.Max(turnDuration, turnAngle * Mathf.PI / (2f * rotationSpeed));
        turnElapsed = 0f;
        if (viewPivot != null)
        {
            // Keep the view steady while the collision capsule changes orientation at once.
            viewRotationAtShift = Quaternion.Inverse(targetRotation) * view.transform.rotation
                                * Quaternion.Inverse(view.transform.localRotation);
            viewPivot.localRotation = viewRotationAtShift;
        }
        body.rotation = targetRotation;
        GravityDirection = -normal;
        // Close surfaces need less launch speed; distant surfaces get an immediate pull.
        float safeDistance = Mathf.Max(0f, distance - 1f);
        float pull = Mathf.Min(launchSpeed, Mathf.Sqrt(2f * player.gravStrength * safeDistance));
        body.linearVelocity = Vector3.ClampMagnitude(turn * tangentVelocity, maximumShiftSpeed)
                            + GravityDirection * Mathf.Max(3f, pull);
        body.WakeUp();
        travelling = true;
        landingReadyAt = Time.time + 0.08f;
        travelExpiresAt = Time.time + 5f;
        player.ApplyGravityShiftFovKick(shiftFovKick);
        if (shiftSound != null) shiftAudio.PlayOneShot(shiftSound, soundVolume);
        ShiftCount++;
        readyAt = Time.time + inputCooldown;
        return true;
    }

    bool TrySurface(Ray aim, out Vector3 normal, out float distance)
    {
        normal = Vector3.zero;
        distance = 0f;
        int count;
        while ((count = Physics.RaycastNonAlloc(aim, hits, aimRange, surfaceMask, QueryTriggerInteraction.Ignore)) == hits.Length)
            System.Array.Resize(ref hits, hits.Length * 2);
        float nearest = float.PositiveInfinity;
        int selected = -1;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].transform.IsChildOf(transform.root)) continue;
            var collider = hits[i].collider;
            if (collider.GetComponentInParent<TimeItem>() != null || collider.GetComponentInParent<HealthItem>() != null ||
                collider.GetComponentInParent<AbilityBoxItem>() != null) continue;
            if (hits[i].distance >= nearest) continue;
            nearest = hits[i].distance; selected = i;
        }
        // Stop at the first obstruction; do not shift through an enemy or prop.
        if (selected < 0 || !hits[selected].collider.CompareTag(player.groundTag)) return false;
        normal = Cardinal(hits[selected].normal);
        distance = hits[selected].distance;
        return Vector3.Dot(normal, hits[selected].normal) > .98f;
    }

    public void AlignBody()
    {
        Vector3 currentUp = body.rotation * Vector3.up;
        Vector3 desiredUp = -GravityDirection;
        if (Vector3.Dot(currentUp, desiredUp) < 0.9999f)
            body.MoveRotation(FrameRotation(currentUp, desiredUp) * body.rotation);
    }

    public void LimitTravelSpeed()
    {
        if (!travelling) return;
        if (Time.time >= travelExpiresAt) { travelling = false; return; }
        Vector3 velocity = body.linearVelocity;
        float alongGravity = Vector3.Dot(velocity, GravityDirection);
        if (alongGravity > maximumTravelSpeed)
            body.linearVelocity = velocity + GravityDirection * (maximumTravelSpeed - alongGravity);
    }

    void OnCollisionEnter(Collision collision) { CheckLanding(collision); }
    void OnCollisionStay(Collision collision) { CheckLanding(collision); }

    void CheckLanding(Collision collision)
    {
        if (!travelling || Time.time < landingReadyAt || !collision.collider.CompareTag(player.groundTag)) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(collision.GetContact(i).normal, -GravityDirection) < 0.65f) continue;
            travelling = false;
            player.ApplyGravityShiftFovKick(landingFovKick);
            if (landingSound != null) shiftAudio.PlayOneShot(landingSound, soundVolume);
            break;
        }
    }

    static float EaseOutSine(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }

    Quaternion FrameRotation(Vector3 fromUp, Vector3 toUp)
    {
        if (Vector3.Dot(fromUp, toUp) >= -0.999f)
            return Quaternion.FromToRotation(fromUp, toUp);

        // Keep a stable axis for 180-degree turns even if another script tilted the body.
        Vector3 axis = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, fromUp);
        if (axis.sqrMagnitude < 0.0001f)
            axis = Vector3.Cross(fromUp, Mathf.Abs(fromUp.x) < 0.9f ? Vector3.right : Vector3.forward);
        Quaternion flip = Quaternion.AngleAxis(180f, axis.normalized);
        return Quaternion.FromToRotation(flip * fromUp, toUp) * flip;
    }

    static Vector3 Cardinal(Vector3 direction)
    {
        Vector3 d = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
        if (d.x > d.y && d.x > d.z) return Vector3.right * Mathf.Sign(direction.x);
        return d.y > d.z ? Vector3.up * Mathf.Sign(direction.y) : Vector3.forward * Mathf.Sign(direction.z);
    }

    static string Face(Vector3 up)
    {
        if (up.y > .5f) return "FLOOR";
        if (up.y < -.5f) return "CEILING";
        if (up.x > .5f) return "LEFT WALL";
        if (up.x < -.5f) return "RIGHT WALL";
        return up.z > 0 ? "BACK WALL" : "FRONT WALL";
    }

    void OnDisable()
    {
        travelling = false;
        if (viewPivot != null) viewPivot.localRotation = Quaternion.identity;
        if (hud != null) hud.SetActive(false);
    }
    void OnEnable() { if (hud != null) hud.SetActive(true); }
}
