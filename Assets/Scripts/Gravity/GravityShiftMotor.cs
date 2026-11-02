using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(GravityPlayerController), typeof(Rigidbody))]
public sealed class GravityShiftMotor : MonoBehaviour
{
    public KeyCode shiftKey = KeyCode.E;
    [Min(1f)] public float aimRange = 60f;
    [Range(90f, 720f)] public float rotationSpeed = 360f;
    [Min(0.1f)] public float inputCooldown = 0.3f;
    [Min(1f)] public float maximumShiftSpeed = 14f;
    public LayerMask surfaceMask = Physics.DefaultRaycastLayers;
    public bool showHUD = true;

    public Vector3 GravityDirection { get; private set; } = Vector3.down;
    public bool IsTurning => Vector3.Angle(transform.up, -GravityDirection) > 1f;
    public int ShiftCount { get; private set; }
    private GravityPlayerController player;
    private Rigidbody body;
    private Camera view;
    private float readyAt, nextPreview;
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
        hasAim = TrySurface(view.ViewportPointToRay(new Vector3(.5f, .5f, 0)), out aimNormal);
        string message = IsTurning ? "GRAVITY SHIFT - TURNING" : hasAim
            ? (Vector3.Dot(aimNormal, -GravityDirection) > .99f ? "CURRENT SURFACE" : "SHIFT [" + key + "] - " + Face(aimNormal))
            : "AIM AT A WALL OR CEILING - SHIFT [" + key + "]";
        if (status != null && displayed != message)
        {
            displayed = message; status.text = message;
            status.color = hasAim ? new Color(.25f, .9f, 1f) : new Color(.8f, .85f, .9f);
        }
    }

    public bool TryShift(Ray aim)
    {
        if (!isActiveAndEnabled || player.IsDead || Time.timeScale == 0f || Time.time < readyAt) return false;
        if (!TrySurface(aim, out var normal) || Vector3.Dot(normal, -GravityDirection) > .99f) return false;
        Vector3 oldUp = -GravityDirection;
        Quaternion turn = FrameRotation(oldUp, normal);
        Vector3 tangentVelocity = Vector3.ProjectOnPlane(body.linearVelocity, oldUp);
        GravityDirection = -normal;
        body.linearVelocity = Vector3.ClampMagnitude(turn * tangentVelocity, maximumShiftSpeed) + GravityDirection * 1.5f;
        body.WakeUp();
        ShiftCount++;
        readyAt = Time.time + inputCooldown;
        return true;
    }

    bool TrySurface(Ray aim, out Vector3 normal)
    {
        normal = Vector3.zero;
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
        return Vector3.Dot(normal, hits[selected].normal) > .98f;
    }

    public void AlignBody(float dt)
    {
        Vector3 desiredUp = -GravityDirection;
        Quaternion target = FrameRotation(body.rotation * Vector3.up, desiredUp) * body.rotation;
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, target, rotationSpeed * dt));
    }

    Quaternion FrameRotation(Vector3 fromUp, Vector3 toUp)
    {
        // Deterministic axis for floor-to-ceiling (180 degree) transitions.
        return Vector3.Dot(fromUp, toUp) < -.999f
            ? Quaternion.AngleAxis(180f, Vector3.ProjectOnPlane(body.rotation * Vector3.forward, fromUp).normalized)
            : Quaternion.FromToRotation(fromUp, toUp);
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

    void OnDisable() { if (hud != null) hud.SetActive(false); }
    void OnEnable() { if (hud != null) hud.SetActive(true); }
}
