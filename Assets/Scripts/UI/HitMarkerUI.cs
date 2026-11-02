using UnityEngine;
using UnityEngine.UI;

// Four mesh strokes: no textures, spawned effects, or per-hit coroutines.
public sealed class HitMarkerUI : MonoBehaviour
{
    private HitMarkerGraphic marker;
    private GravityPlayerController player;
    private float shownAt;
    private float duration;
    private float killPriorityUntil;
    private bool kill;
    private static readonly Color HitColor = new Color(1f, 0.48f, 0.08f);
    private static readonly Color KillColor = new Color(1f, 0.12f, 0.12f);

    public static HitMarkerUI Create(GravityPlayerController owner)
    {
        var root = new GameObject("Hit Marker HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(owner.transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var ui = root.AddComponent<HitMarkerUI>();
        ui.player = owner;
        var strokes = new GameObject("Hit Marker", typeof(RectTransform), typeof(CanvasRenderer));
        strokes.transform.SetParent(root.transform, false);
        var rect = strokes.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(80, 80);
        rect.anchoredPosition = Vector2.zero;
        ui.marker = strokes.AddComponent<HitMarkerGraphic>();
        ui.marker.raycastTarget = false;
        ui.marker.enabled = false;
        return ui;
    }

    public void Show(bool lethal)
    {
        if (player.IsDead || Time.timeScale == 0f) return;
        float now = Time.unscaledTime;
        // A shotgun pellet or another target must not overwrite a recent kill.
        if (!lethal && now < killPriorityUntil) return;
        kill = lethal;
        duration = lethal ? 0.26f : 0.15f;
        shownAt = now;
        if (lethal) killPriorityUntil = now + duration;
        marker.color = lethal ? KillColor : HitColor;
        marker.strokeScale = lethal ? 1.18f : 1f;
        marker.enabled = true;
        marker.SetVerticesDirty();
    }

    void Update()
    {
        if (marker == null || !marker.enabled) return;
        float t = (Time.unscaledTime - shownAt) / duration;
        if (t >= 1f || player.IsDead || Time.timeScale == 0f)
        {
            marker.enabled = false;
            return;
        }
        var color = kill ? KillColor : HitColor;
        color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, t));
        marker.color = color;
        marker.strokeScale = (kill ? 1.5f : 1f) * (1f + 0.12f * (1f - t));
        marker.SetVerticesDirty();
    }
}

public sealed class HitMarkerGraphic : MaskableGraphic
{
    public float strokeScale = 1f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        const float diagonal = 0.70710678f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = new Vector2((i & 1) == 0 ? -diagonal : diagonal, (i & 2) == 0 ? -diagonal : diagonal);
            Vector2 normal = new Vector2(-direction.y, direction.x) * 1.25f;
            Vector2 start = direction * (9f * strokeScale);
            Vector2 end = direction * (18f * strokeScale);
            int index = vh.currentVertCount;
            vh.AddVert(start - normal, color, Vector2.zero);
            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
