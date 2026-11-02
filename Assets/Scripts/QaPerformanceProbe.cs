#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

// Added only at runtime by MCP. Never included in player builds or saved scenes.
public sealed class QaPerformanceProbe : MonoBehaviour
{
    public string reportName = "baseline";
    public bool finished;
    private float started, nextShot;
    private Action shoot;
    private GravityPlayerController player;
    private Camera view;
    private bool oldBackground;
    private ProfilerRecorder gc, main;
    private readonly List<double> frames = new List<double>(4096);
    private readonly List<double> allocations = new List<double>(4096);
    private readonly List<double> cpu = new List<double>(4096);
    private readonly Dictionary<string, double> markers = new Dictionary<string, double>();
    private int sampledFrames, shots;

    void Start()
    {
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        UnityEngine.Random.InitState(4219);
        player = FindFirstObjectByType<GravityPlayerController>();
        player.maxHp = player.currentHp = 1000000;
        if (player.timerUI != null) player.timerUI.enabled = false;
        var shooter = FindFirstObjectByType<DoubleMagnumShooter>();
        view = shooter.GetComponentInParent<Camera>();
        var look = view.GetComponent<camera>();
        if (look != null) look.enabled = false;
        shoot = (Action)Delegate.CreateDelegate(typeof(Action), shooter,
            typeof(DoubleMagnumShooter).GetMethod("Shoot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
        foreach (var spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
        {
            for (int i = 0; i < 15; i++) spawner.SendMessage("TrySpawnEnemy");
            spawner.StopSpawning();
        }
        gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        started = Time.realtimeSinceStartup;
    }

    void Update()
    {
        if (finished) return;
        float elapsed = Time.realtimeSinceStartup - started;
        if (elapsed >= 25f) { Finish(); return; }
        view.transform.localRotation = Quaternion.Euler(10f * Mathf.Sin(elapsed), elapsed * 25f, 0f);
        if (elapsed >= nextShot)
        {
            shoot(); shots++;
            nextShot = elapsed + 0.55f;
        }
        player.GetComponent<Rigidbody>().AddForce(player.transform.right * Mathf.Sin(elapsed) * 2f, ForceMode.Acceleration);
        if (elapsed < 5f) return;
        frames.Add(Time.unscaledDeltaTime * 1000.0);
        allocations.Add(gc.LastValue);
        cpu.Add(main.LastValue / 1000000.0);
        if (Time.frameCount % 30 != 0) return;
        using (var data = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(UnityEditorInternal.ProfilerDriver.lastFrameIndex, 0))
        {
            if (!data.valid) return;
            sampledFrames++;
            for (int i = 0; i < data.sampleCount; i++)
            {
                var name = data.GetSampleName(i);
                if (!markers.ContainsKey(name)) markers[name] = 0;
                markers[name] += data.GetSampleTimeMs(i);
            }
        }
    }

    static string Stats(List<double> values)
    {
        values.Sort(); if (values.Count == 0) return "no samples";
        double sum = 0; foreach (double v in values) sum += v;
        return $"mean={sum / values.Count:F3}, p50={values[values.Count / 2]:F3}, p95={values[(int)((values.Count - 1) * .95)]:F3}, max={values[values.Count - 1]:F3}";
    }

    void Finish()
    {
        finished = true;
        var lines = new List<string> { $"Run={reportName}, samples={frames.Count}, shots={shots}, enemies={FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length}",
            "Frame ms: " + Stats(frames), "Main thread ms: " + Stats(cpu), "GC bytes/frame: " + Stats(allocations), $"Profiler sampled frames={sampledFrames}" };
        var sorted = new List<KeyValuePair<string,double>>(markers);
        sorted.Sort((a,b) => b.Value.CompareTo(a.Value));
        for(int i=0;i<Math.Min(65,sorted.Count);i++) lines.Add($"{sorted[i].Key}: {sorted[i].Value / Math.Max(1,sampledFrames):F4} ms/sample");
        Directory.CreateDirectory(Application.dataPath + "/QAReports");
        File.WriteAllLines(Application.dataPath + "/QAReports/" + reportName + ".txt", lines);
        gc.Dispose(); main.Dispose();
        Debug.Log("QA performance run completed: " + reportName);
    }

    void OnDestroy()
    {
        if (!finished) { gc.Dispose(); main.Dispose(); }
        Application.runInBackground = oldBackground;
    }
}
#endif
