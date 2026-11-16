using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class AudioManager : MonoBehaviour
{
    [Header("音源データ")]
    [SerializeField] private AudioSource SESource;
    [SerializeField] private AudioSource BGMSoruce;
    [SerializeField] private List<BGMSound> BGMSounds;
    [SerializeField] private List<SECategory> SECategorys;

    [Header("Volume管理")]
    [SerializeField, Range(0f, 1f)] public float masterVolume = 1.0f;
    [SerializeField, Range(0f, 1f)] public float bgmmasterVolume = 1.0f;
    [SerializeField, Range(0f, 1f)] public float semasterVolume = 1.0f;

    [Header("譜面作成・エディタ拡張機能")]
    [SerializeField] private AudioDataSO activeAudioData;  //現在選択されている楽曲データ

    //再生時のタイマー補正用変数
    private double dspStartTime = 0.0;
    private double bgmoffsetPauseTime = 0.0;

    // ループSE用AudioSource
    private AudioSource loopSESource;

    #region singleton
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    private void Start()
    {
        if (activeAudioData != null)
        {
            LoadMusicData(activeAudioData);
        }

        // LoopSE用AudioSourceの初期化
        InitializeLoopSESource();
    }

    /// <summary>
    /// ループSE用AudioSourceの初期化
    /// </summary>
    private void InitializeLoopSESource()
    {
        if (loopSESource != null) return;

        GameObject loopSeObj = new GameObject("LoopSESource");
        loopSeObj.transform.SetParent(transform);
        loopSESource = loopSeObj.AddComponent<AudioSource>();
        loopSESource.playOnAwake = false;
    }

    /// <summary> /// BGMの停止 /// </summary>
    public void BGMStop()
    {
        if (BGMSoruce == null) return;
        BGMSoruce.Stop();
        BGMSoruce.time = 0f;
        bgmoffsetPauseTime = 0.0;
    }

    /// <summary> /// BGMの一時停止 /// </summary>
    public void BGMPause()
    {
        if (BGMSoruce == null) return;
        BGMSoruce.Pause();
        bgmoffsetPauseTime = BGMSoruce.time;
    }

    /// <summary> /// BGMの再開 /// </summary>
    public void BGMauPause()
    {
        if (BGMSoruce == null) return;
        dspStartTime = AudioSettings.dspTime;
        bgmoffsetPauseTime = BGMSoruce.time;
        BGMSoruce.UnPause();
    }

    /// <summary>
    /// エディタ用：再生と一時停止のトグル切り替え
    /// </summary>
    public void TogglePlayPause()
    {
        if (BGMSoruce == null || BGMSoruce.clip == null)
        {
            Debug.LogWarning("[AudioManager] BGMがロードされていないため再生できません。");
            return;
        }

        if (BGMSoruce.isPlaying)
        {
            BGMPause();
            Debug.Log($"[AudioManager] 一時停止 ({BGMSoruce.time:F2}s)");
        }
        else
        {
            PlayActiveMusic();
            Debug.Log($"[AudioManager] 再生開始 ({BGMSoruce.time:F2}s)");
        }
    }

    /// <summary>
    /// BGMSoundのリスト内にあるBGMを再生
    /// </summary>
    public void PlayBGM(BGMSound.BGMDETA bgm)
    {
        BGMSound data = BGMSounds.Find(d => d.bgmData == bgm);
        if (data != null)
        {
            BGMSoruce.clip = data.bgmclip;
            BGMSoruce.volume = data.bgmVolume * masterVolume * bgmmasterVolume;
            BGMSoruce.Play();
        }
        else
        {
            Debug.LogError("指定されたBGMデータが見つかりません:" + bgm);
        }
    }

    /// <summary>
    /// 特定の楽曲データを読み込む（インゲーム用SO）
    /// </summary>
    public void LoadMusicData(AudioDataSO audioData)
    {
        if (audioData == null) { return; }

        activeAudioData = audioData;
        if (BGMSoruce != null && activeAudioData.audioClip != null)
        {
            BGMSoruce.clip = activeAudioData.audioClip;
            BGMSoruce.time = 0;
            BGMSoruce.pitch = 1.0f;
            BGMSoruce.loop = false;
            BGMSoruce.playOnAwake = false;
            bgmoffsetPauseTime = 0.0;

            BGMSoruce.volume = masterVolume * bgmmasterVolume;
            Debug.Log($"楽曲データセット: {activeAudioData.musicTitle} (BPM: {activeAudioData.bpm})");
        }
    }

    /// <summary> /// 音楽再生 /// </summary>
    public void PlayActiveMusic()
    {
        if (BGMSoruce == null || BGMSoruce.clip == null) { return; }

        dspStartTime = AudioSettings.dspTime;
        bgmoffsetPauseTime = BGMSoruce.time;
        BGMSoruce.volume = masterVolume * bgmmasterVolume;
        BGMSoruce.Play();
    }

    /// <summary>
    /// エディタ用再生スキップ
    /// </summary>
    public void SkipTime(float seconds)
    {
        if (BGMSoruce == null || BGMSoruce.clip == null) { return; }

        float targetTime = Mathf.Clamp(BGMSoruce.time + seconds, 0f, BGMSoruce.clip.length);
        BGMSoruce.time = targetTime;

        bgmoffsetPauseTime = targetTime;
        dspStartTime = AudioSettings.dspTime;
    }

    //再生速度
    public void SetPitch(float pitch)
    {
        if (BGMSoruce == null || BGMSoruce.clip == null) { return; }

        // 速度変更時も現在時刻がズレないよう基準点を再記録
        if (BGMSoruce.isPlaying)
        {
            bgmoffsetPauseTime = GetCurrentTime();
            dspStartTime = AudioSettings.dspTime;
        }
        BGMSoruce.pitch = Mathf.Clamp(pitch, 0.25f, 3.0f);
    }

    //エディタ用ステータス取得
    public bool IsBGMPlaying() => BGMSoruce != null && BGMSoruce.isPlaying;

    public float GetCurrentTime()
    {
        if (BGMSoruce == null || BGMSoruce.clip == null) return 0f;

        // 停止中・一時停止中はDSP計算を行わず現在のAudioSource.timeを返す
        if (!BGMSoruce.isPlaying)
        {
            return BGMSoruce.time;
        }

        double elapsedTime = (AudioSettings.dspTime - dspStartTime) * BGMSoruce.pitch;
        return (float)(bgmoffsetPauseTime + elapsedTime);
    }

    public float GetTotalDuration() => (BGMSoruce != null && BGMSoruce.clip != null) ? BGMSoruce.clip.length : 0f;
    public float GetCurrentPitch() => BGMSoruce != null ? BGMSoruce.pitch : 1.0f;
    public AudioDataSO GetActiveMusicData() => activeAudioData;

    /// <summary> /// SEを再生 /// </summary>
    public void PlaySE(string categoryName, SESound.SEDATA se)
    {
        SECategory category = SECategorys.Find(c => c.categoryName == categoryName);
        if (category != null)
        {
            SESound data = category.sounds.Find(sound => sound.seData == se);
            if (data != null)
            {
                SESource.volume = data.seVolume * masterVolume * semasterVolume;
                SESource.PlayOneShot(data.seclip);
            }
            else
            {
                Debug.LogError("指定されたSEが見つかりません:" + se);
            }
        }
        else
        {
            Debug.LogError("指定されたカテゴリが見つかりません。" + categoryName);
        }
    }

    /// <summary> /// SEカテゴリー内のSEを再生 /// </summary>
    public void PlayspecificSE(string categoryName, int index)
    {
        SECategory category = SECategorys.Find(c => c.categoryName == categoryName);
        if (category != null)
        {
            if (index >= 0 && index < category.sounds.Count)
            {
                SESound data = category.sounds[index];
                SESource.volume = data.seVolume * masterVolume * semasterVolume;
                SESource.PlayOneShot(data.seclip);
            }
            else
            {
                Debug.LogError("指定されたSEのインデックスが見つかりません。" + index + categoryName);
            }
        }
        else
        {
            Debug.LogError("指定されたカテゴリが見つかりません。" + categoryName);
        }
    }

    /// <summary> /// ループSEを再生（ロングノーツ用） /// </summary>
    public void PlayLoopSE(string categoryName, SESound.SEDATA se)
    {
        if (loopSESource == null) InitializeLoopSESource();

        SECategory category = SECategorys.Find(c => c.categoryName == categoryName);
        if (category != null)
        {
            SESound data = category.sounds.Find(sound => sound.seData == se);
            if (data != null)
            {
                loopSESource.clip = data.seclip;
                loopSESource.volume = data.seVolume * masterVolume * semasterVolume;
                loopSESource.loop = true;
                loopSESource.Play();
            }
            else
            {
                Debug.LogError("指定されたSEが見つかりません:" + se);
            }
        }
        else
        {
            Debug.LogError("指定されたカテゴリが見つかりません。" + categoryName);
        }
    }

    /// <summary> /// ループSEを停止 /// </summary>
    public void StopLoopSE()
    {
        if (loopSESource != null && loopSESource.isPlaying)
        {
            loopSESource.Stop();
            loopSESource.clip = null;
        }
    }

    public void PauseAll()
    {
        try
        {
            if (BGMSoruce != null && BGMSoruce.isPlaying) BGMSoruce.Pause();
            if (SESource != null) SESource.Pause();
            if (loopSESource != null && loopSESource.isPlaying) loopSESource.Pause();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("AudioManager.PauseAll failed: " + ex.Message);
        }
    }

    public void ResumeAll()
    {
        try
        {
            if (BGMSoruce != null) BGMSoruce.UnPause();
            if (SESource != null) SESource.UnPause();
            if (loopSESource != null) loopSESource.UnPause();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("AudioManager.ResumeAll failed: " + ex.Message);
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        bgmmasterVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();
    }

    public void SetSEVolume(float volume)
    {
        semasterVolume = Mathf.Clamp01(volume);
        UpdateSEVolume();
    }

    public float GetMasterVolume() => masterVolume;
    public float GetBGMVolume() => bgmmasterVolume;
    public float GetSEVolume() => semasterVolume;

    private void UpdateAllVolumes()
    {
        UpdateBGMVolume();
        UpdateSEVolume();
    }

    private void UpdateBGMVolume()
    {
        if (BGMSoruce != null) BGMSoruce.volume = masterVolume * bgmmasterVolume;
    }

    private void UpdateSEVolume()
    {
        if (SESource != null) SESource.volume = masterVolume * semasterVolume;
        if (loopSESource != null) loopSESource.volume = masterVolume * semasterVolume;
    }

    /// <summary> /// AudioClipの非同期読み込み（エディタ用） /// </summary>
    public IEnumerator LoadAudioClipFromPath(string filePath, System.Action<bool> onComp = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogWarning($"[AudioManager] 音源ファイルが見つかりません: {filePath}");
            onComp?.Invoke(false);
            yield break;
        }

        // 拡張子からAudioTypeを判別
        AudioType audioType = AudioType.OGGVORBIS;
        string ext = Path.GetExtension(filePath).ToLower();
        if (ext == ".wav") { audioType = AudioType.WAV; }
        else if (ext == ".mp3") { audioType = AudioType.MPEG; }

        // Windowsのパス区切り文字を正規化してURI生成
        string fileUri = "file:///" + filePath.Replace("\\", "/");

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUri, audioType))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AudioManager] 音源ロード失敗: {www.error} (URI: {fileUri})");
                onComp?.Invoke(false);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null && BGMSoruce != null)
                {
                    BGMSoruce.Stop();
                    BGMSoruce.clip = clip;
                    BGMSoruce.time = 0f;
                    BGMSoruce.pitch = 1.0f;
                    BGMSoruce.loop = false;
                    BGMSoruce.volume = masterVolume * bgmmasterVolume;

                    bgmoffsetPauseTime = 0.0;
                    dspStartTime = AudioSettings.dspTime;

                    Debug.Log($"[AudioManager] 音源ロード成功: {Path.GetFileName(filePath)} ({clip.length:F2}s)");
                    onComp?.Invoke(true);
                }
                else
                {
                    Debug.LogError("[AudioManager] AudioClipの取得またはBGMSoruceの参照に失敗しました。");
                    onComp?.Invoke(false);
                }
            }
        }
    }
}

[Serializable]
public class BGMSound
{
    public enum BGMDETA
    {
        Title,
        Ingame,
        Result,
        None,
    }
    public BGMDETA bgmData;
    public AudioClip bgmclip;
    [Range(0f, 1f)] public float bgmVolume = 1;
}

[Serializable]
public class SESound
{
    public enum SEDATA
    {
        character,
        Tap,
        System,
        None,
    }
    public SEDATA seData;
    public AudioClip seclip;
    [Range(0f, 1f)] public float seVolume = 1;
}

[Serializable]
public class SECategory
{
    public string categoryName;
    public List<SESound> sounds = new List<SESound>();
}