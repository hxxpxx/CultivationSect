// ===== AudioManager.cs =====
// 放在 Assets/Scripts/Core/ 下
// 自动创建，无需手动挂载（也可手动挂到 PersistentObject 上）
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频池配置")]
    public int maxSimultaneousSFX = 16;     // 最大同时播放音效数
    public int initialPoolSize = 8;         // 初始池大小

    [Header("音量设置")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("BGM 配置")]
    public AudioClip defaultBGM;
    public float bgmFadeTime = 1.5f;

    [Header("音效注册表（可选，Inspector中直接拖入常用音效）")]
    public List<AudioClipEntry> registeredClips = new List<AudioClipEntry>();

    // ============ 内部 ============
    AudioSource bgmSource;
    Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    List<AudioSource> activeSFX = new List<AudioSource>();
    Dictionary<string, AudioClip> clipRegistry = new Dictionary<string, AudioClip>();

    // 音量持久化
    const string MASTER_KEY = "Audio_Master";
    const string BGM_KEY = "Audio_BGM";
    const string SFX_KEY = "Audio_SFX";

    // ============ 生命周期 ============

    void Awake()
    {
        // 单例 + 持久化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumeSettings();
        InitializeAudio();
        BuildClipRegistry();

        // 自动订阅 GameEvents
        SubscribeToGameEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    void InitializeAudio()
    {
        // BGM AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume * masterVolume;
        bgmSource.spatialBlend = 0f; // 2D

        // SFX 对象池
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledAudioSource();
        }

        // 播放默认 BGM
        if (defaultBGM != null)
            PlayBGM(defaultBGM);
    }

    AudioSource CreatePooledAudioSource()
    {
        var go = new GameObject($"SFX_Source_{sfxPool.Count + activeSFX.Count}");
        go.transform.SetParent(transform);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 默认2D，UI音效
        source.volume = sfxVolume * masterVolume;
        sfxPool.Enqueue(source);
        return source;
    }

    void BuildClipRegistry()
    {
        clipRegistry.Clear();
        foreach (var entry in registeredClips)
        {
            if (entry.clip != null && !string.IsNullOrEmpty(entry.clipName))
                clipRegistry[entry.clipName] = entry.clip;
        }
    }

    void SubscribeToGameEvents()
    {
        //GameEvents.OnPlaySFX += PlaySFX;
    }

    void UnsubscribeFromGameEvents()
    {
        //GameEvents.OnPlaySFX -= PlaySFX;
    }

    // ============ 公共 API ============

    /// <summary>
    /// 播放音效（通过 GameEvents 或代码直接调用）
    /// </summary>
    public void Play(string sfxName, float volumeScale = 1f)
    {
        AudioClip clip = GetClip(sfxName);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] 找不到音效: '{sfxName}'");
            return;
        }
        PlayClip(clip, volumeScale);
    }

    /// <summary>
    /// 直接播放 AudioClip
    /// </summary>
    public void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // 回收已播放完毕的
        RecycleFinishedSources();

        // 池空则动态扩展
        if (sfxPool.Count == 0)
        {
            if (activeSFX.Count >= maxSimultaneousSFX)
            {
                Debug.LogWarning("[AudioManager] 音效池已满，丢弃最旧音效");
                // 强制回收最旧的
                var oldest = activeSFX[0];
                oldest.Stop();
                activeSFX.RemoveAt(0);
                oldest.gameObject.SetActive(false);
                sfxPool.Enqueue(oldest);
            }
            else
            {
                CreatePooledAudioSource();
            }
        }

        var source = sfxPool.Dequeue();
        source.gameObject.SetActive(true);
        source.clip = clip;
        source.volume = sfxVolume * masterVolume * Mathf.Clamp01(volumeScale);
        source.pitch = 1f;
        source.loop = false;
        source.spatialBlend = 0f;
        source.Play();
        activeSFX.Add(source);
    }

    /// <summary>
    /// 播放音效（带随机音高变化，适合重复点击）
    /// </summary>
    public void PlayWithRandomPitch(string sfxName, float pitchMin = 0.9f, float pitchMax = 1.1f, float volumeScale = 1f)
    {
        AudioClip clip = GetClip(sfxName);
        if (clip == null) return;

        RecycleFinishedSources();

        if (sfxPool.Count == 0) CreatePooledAudioSource();
        var source = sfxPool.Dequeue();
        source.gameObject.SetActive(true);
        source.clip = clip;
        source.volume = sfxVolume * masterVolume * Mathf.Clamp01(volumeScale);
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.loop = false;
        source.Play();
        activeSFX.Add(source);
    }

    /// <summary>
    /// 播放 3D 音效（世界坐标）
    /// </summary>
    public void Play3D(string sfxName, Vector3 worldPosition, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 20f)
    {
        AudioClip clip = GetClip(sfxName);
        if (clip == null) return;

        RecycleFinishedSources();
        if (sfxPool.Count == 0) CreatePooledAudioSource();

        var source = sfxPool.Dequeue();
        source.gameObject.SetActive(true);
        source.transform.position = worldPosition;
        source.clip = clip;
        source.volume = sfxVolume * masterVolume * Mathf.Clamp01(volumeScale);
        source.spatialBlend = 1f; // 3D
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.pitch = 1f;
        source.loop = false;
        source.Play();
        activeSFX.Add(source);
    }

    /// <summary>
    /// 播放 BGM（带淡入淡出）
    /// </summary>
    public void PlayBGM(AudioClip bgmClip, bool fade = true)
    {
        if (bgmClip == null) return;
        if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return;

        if (fade)
            StartCoroutine(CrossfadeBGM(bgmClip));
        else
        {
            bgmSource.Stop();
            bgmSource.clip = bgmClip;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.Play();
        }
    }

    /// <summary>
    /// 停止 BGM
    /// </summary>
    public void StopBGM(bool fade = true)
    {
        if (fade)
            StartCoroutine(FadeOutBGM());
        else
            bgmSource.Stop();
    }

    /// <summary>
    /// 设置主音量
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume * masterVolume;
        PlayerPrefs.SetFloat(MASTER_KEY, masterVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_KEY, sfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 设置 BGM 音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume * masterVolume;
        PlayerPrefs.SetFloat(BGM_KEY, bgmVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 注册音效（运行时）
    /// </summary>
    public void RegisterClip(string name, AudioClip clip)
    {
        if (!string.IsNullOrEmpty(name) && clip != null)
            clipRegistry[name] = clip;
    }

    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopAllSFX()
    {
        foreach (var src in activeSFX)
        {
            src.Stop();
            src.gameObject.SetActive(false);
            sfxPool.Enqueue(src);
        }
        activeSFX.Clear();
    }

    // ============ 内部方法 ============

    AudioClip GetClip(string name)
    {
        if (clipRegistry.TryGetValue(name, out var clip))
            return clip;
        return null;
    }

    void RecycleFinishedSources()
    {
        for (int i = activeSFX.Count - 1; i >= 0; i--)
        {
            if (!activeSFX[i].isPlaying)
            {
                activeSFX[i].gameObject.SetActive(false);
                sfxPool.Enqueue(activeSFX[i]);
                activeSFX.RemoveAt(i);
            }
        }
    }

    void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BGM_KEY, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 1f);
    }

    // ============ BGM 淡入淡出协程 ============

    System.Collections.IEnumerator CrossfadeBGM(AudioClip newClip)
    {
        // 淡出旧 BGM
        float startVolume = bgmSource.volume;
        float timer = 0f;
        while (timer < bgmFadeTime)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / bgmFadeTime);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 淡入新 BGM
        timer = 0f;
        while (timer < bgmFadeTime)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume * masterVolume, timer / bgmFadeTime);
            yield return null;
        }
        bgmSource.volume = bgmVolume * masterVolume;
    }

    System.Collections.IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;
        while (timer < bgmFadeTime)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / bgmFadeTime);
            yield return null;
        }
        bgmSource.Stop();
    }

    // ============ 调试 ============

    [ContextMenu("打印音效池状态")]
    void DebugPoolStatus()
    {
        Debug.Log($"[AudioManager] 池空闲: {sfxPool.Count}, 活跃: {activeSFX.Count}, 注册表: {clipRegistry.Count}");
    }
}

// ===== AudioClipEntry.cs (可序列化条目) =====
// 放在 AudioManager.cs 同文件或单独文件

[System.Serializable]
public class AudioClipEntry
{
    public string clipName;
    public AudioClip clip;
}