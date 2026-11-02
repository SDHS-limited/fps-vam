using System;
using UnityEngine;

// Reused on the Unity main thread. Grow and retry if crowded, so no hits are lost.
internal static class ShotRaycast
{
    private static RaycastHit[] hits = new RaycastHit[32];

    public static bool Closest(Ray ray, float distance, int mask, Transform owner, out RaycastHit closest)
    {
        int count;
        while ((count = Physics.RaycastNonAlloc(ray, hits, distance, mask, QueryTriggerInteraction.Ignore)) == hits.Length)
            Array.Resize(ref hits, hits.Length * 2);
        closest = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (owner != null && hit.transform.IsChildOf(owner)) continue;
            if (hit.collider.GetComponentInParent<Bullet>() != null || hit.distance >= nearest) continue;
            nearest = hit.distance;
            closest = hit;
        }
        return nearest < float.PositiveInfinity;
    }
}
