using UnityEngine;

// Reuses the three sprite planes from Toon Muzzleflash 10 for each shot.
public sealed class MagnumMuzzleFlash : MonoBehaviour
{
    public SpriteRenderer[] sideRenderers;
    public SpriteRenderer frontRenderer;
    public Sprite[] sideFrames;
    public Sprite[] frontFrames;
    public Light flashLight;
    [Min(0.01f)] public float duration = 0.09f;
    [Min(0.001f)] public float size = 0.028f;

    private Transform barrel;
    private float elapsed;
    private float shotScale;
    private float roll;
    private bool emitting;

    void Awake() { Hide(); }

    public void Play(Transform origin)
    {
        if (origin == null) return;
        barrel = origin;
        elapsed = 0f;
        shotScale = size * Random.Range(0.9f, 1.1f);
        roll = Random.Range(-18f, 18f);
        emitting = true;
        UpdateVisuals();
    }

    void LateUpdate()
    {
        if (!emitting) return;
        elapsed += Time.deltaTime;
        if (elapsed >= duration || barrel == null || !barrel.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        float t = Mathf.Clamp01(elapsed / duration);
        // The prefab origin is the flash base. Its artwork extends along +X.
        // Keep the origin exactly on the authored left/right muzzle marker.
        transform.SetPositionAndRotation(barrel.position,
            barrel.rotation * Quaternion.AngleAxis(roll, Vector3.forward) * Quaternion.Euler(0f, -90f, 0f));
        transform.localScale = Vector3.one * shotScale;
        foreach (var r in sideRenderers) SetFrame(r, sideFrames, t);
        SetFrame(frontRenderer, frontFrames, t);
        if (flashLight != null)
        {
            flashLight.enabled = t < 0.55f;
            flashLight.intensity = 2.2f * Mathf.Pow(1f - t, 3f);
        }
    }

    static void SetFrame(SpriteRenderer renderer, Sprite[] frames, float t)
    {
        if (renderer == null) return;
        renderer.enabled = true;
        if (frames != null && frames.Length > 0)
            renderer.sprite = frames[Mathf.Min((int)(t * frames.Length), frames.Length - 1)];
        renderer.color = new Color(1f, 1f, 1f, 1f - t * t);
    }

    public void Hide()
    {
        emitting = false;
        if (sideRenderers != null) foreach (var r in sideRenderers) if (r != null) r.enabled = false;
        if (frontRenderer != null) frontRenderer.enabled = false;
        if (flashLight != null) flashLight.enabled = false;
    }

    void OnDisable() { Hide(); }
}
