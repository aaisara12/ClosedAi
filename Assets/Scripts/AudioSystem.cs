using UnityEngine;
using System.Collections.Generic;

public class AudioSystem : MonoBehaviour
{
    public enum Sound
    {
        Dash,
        EnemyDeath,
        EnemyGun,
        Grapple,
        Pistol,
        Reload,
        Smoke,
        Wallexit,
        Wallenter,
        Wallslide,
        Music,
        Sword,
        EnemyAggressive,
        EnemySuspicious,
    }

    public AudioLibrary library;
    public int initialPoolSize = 5;
    public float unusedLifetime = 10f;

    public static AudioSystem Instance;

    class PooledSource
    {
        public AudioSource source;
        public float lastUsedTime;
    }

    List<PooledSource> pool = new();
    Dictionary<int, PooledSource> activeLoops = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        library.Init();

        for (int i = 0; i < initialPoolSize; i++)
            CreateSource();
    }

    void Start()
    {
        PlayLoop(Sound.Music);
    }

    void Update()
    {
        CleanupUnused();
    }

    PooledSource CreateSource()
    {
        GameObject go = new GameObject("AudioSource");
        go.transform.parent = transform;

        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        var ps = new PooledSource
        {
            source = src,
            lastUsedTime = Time.time
        };

        pool.Add(ps);
        return ps;
    }

    PooledSource GetAvailable()
    {
        foreach (var p in pool)
        {
            if (!p.source.isPlaying && !activeLoops.ContainsValue(p))
                return p;
        }

        return CreateSource();
    }

    void CleanupUnused()
    {
        float now = Time.time;

        for (int i = pool.Count - 1; i >= 0; i--)
        {
            var p = pool[i];

            bool isLoopReserved = activeLoops.ContainsValue(p);

            if (!p.source.isPlaying &&
                !isLoopReserved &&
                now - p.lastUsedTime > unusedLifetime)
            {
                Destroy(p.source.gameObject);
                pool.RemoveAt(i);
            }
        }
    }

    // ---------------- ONE SHOT ----------------

    public static void Play(Sound sound, float volume = 1f, float pitch = 1f)
    {
        if (Instance == null) return;

        if (!Instance.library.TryGet(sound, out var entry))
        {
            Debug.LogWarning($"No clip for {sound}");
            return;
        }

        var pooled = Instance.GetAvailable();
        var src = pooled.source;

        src.clip = entry.clip;
        src.volume = volume;
        src.pitch = pitch;
        src.loop = false;

        if (entry.mixerGroup != null)
            src.outputAudioMixerGroup = entry.mixerGroup;

        src.Play();
        pooled.lastUsedTime = Time.time;
    }

    // ---------------- LOOP PLAY ----------------

    public static int PlayLoop(Sound sound, float volume = 1f, float pitch = 1f)
    {
        if (Instance == null) return 0;

        if (!Instance.library.TryGet(sound, out var entry))
        {
            Debug.LogWarning($"No clip for {sound}");
            return 0;
        }

        var pooled = Instance.GetAvailable();
        var src = pooled.source;

        int id = src.GetInstanceID();

        src.clip = entry.clip;
        src.volume = volume;
        src.pitch = pitch;
        src.loop = true;

        if (entry.mixerGroup != null)
            src.outputAudioMixerGroup = entry.mixerGroup;

        src.Play();

        pooled.lastUsedTime = Time.time;
        Instance.activeLoops.Add(id, pooled);
        return id;
    }

    // ---------------- LOOP STOP ----------------

    public static void StopLoop(int id)
    {
        if (Instance == null) return;

        if (!Instance.activeLoops.TryGetValue(id, out var pooled))
            return;

        pooled.source.Stop();
        pooled.source.loop = false;
        pooled.lastUsedTime = Time.time;

        Instance.activeLoops.Remove(id);
    }
}