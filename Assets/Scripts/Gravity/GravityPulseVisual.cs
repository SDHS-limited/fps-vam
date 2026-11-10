using UnityEngine;

// A short ring travelling along the aim line. The effect owns its material and cleans up.
public sealed class GravityPulseVisual : MonoBehaviour
{
    private const int Segments = 32;
    private const float Duration = 0.18f;
    private static readonly Color PulseColor = new Color(0.25f, 0.85f, 1f, 0.85f);

    private LineRenderer ring;
    private Material ringMaterial;
    private Vector3 origin;
    private Vector3 direction;
    private Quaternion orientation;
    private float range;
    private float radius;
    private float age;

    public static void Play(Vector3 origin, Vector3 direction, float range, float radius)
    {
        if (direction.sqrMagnitude < 0.0001f || range <= 0f || radius <= 0f) return;
        var wave = new GameObject("Gravity Pulse Wave");
        wave.AddComponent<GravityPulseVisual>().Initialize(origin, direction, range, radius);
    }

    private void Initialize(Vector3 start, Vector3 forward, float travelRange, float waveRadius)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Destroy(gameObject);
            return;
        }

        origin = start;
        direction = forward.normalized;
        orientation = Quaternion.FromToRotation(Vector3.forward, direction);
        range = travelRange;
        radius = waveRadius;
        ringMaterial = new Material(shader);
        ring = gameObject.AddComponent<LineRenderer>();
        ring.material = ringMaterial;
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = Segments;
        ring.widthMultiplier = 0.08f;
        ring.numCornerVertices = 2;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        Draw(0f);
    }

    private void Update()
    {
        if (ring == null) return;
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / Duration);
        Draw(progress);
        if (progress >= 1f) Destroy(gameObject);
    }

    private void Draw(float progress)
    {
        Vector3 center = origin + direction * Mathf.Lerp(0.5f, range, progress);
        float currentRadius = Mathf.Lerp(0.35f, radius, Mathf.Sin(progress * Mathf.PI * 0.5f));
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            ring.SetPosition(i, center + orientation * radial * currentRadius);
        }
        Color color = PulseColor;
        color.a *= 1f - progress;
        ring.startColor = ring.endColor = color;
    }

    private void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
    }
}
