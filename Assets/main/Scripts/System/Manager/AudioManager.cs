using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting;

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
        if(activeAudioData != null) 
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
        BGMSoruce.Stop();
    }

    /// <summary> /// BGMの一時停止 /// </summary>
    public void BGMPause()
    {
        BGMSoruce.Pause();
    }

    /// <summary>　/// BGMの再開　/// </summary>
    public void BGMauPause()
    {
        BGMSoruce.UnPause();
    }


    /// <summary>
    /// BGMSoundのリスト内にあるBGMを再生
    /// </summary>
    /// <param name="bgm"></param>
    public void PlayBGM(BGMSound.BGMDETA bgm)
    {
        BGMSound data = BGMSounds.Find(data => data.bgmData == bgm);
        //データがnullどうかチェック
        if (data != null)
        {
            BGMSoruce.clip = data.bgmclip;
            BGMSoruce.volume = data.bgmVolume * masterVolume * bgmmasterVolume;
            BGMSoruce.Play();
        }
        else
        {
            //データの有無をチェック
            Debug.LogError("指定されたBGMデータが見つかりません:" + bgm);
        }

    }

    /// <summary>
    /// 特定の楽曲データを読み込む
    /// </summary>
    /// <param name="audioData"></param>
    public void LoadMusicData(AudioDataSO audioData)
    {
        if(audioData == null) { return; }

        activeAudioData = audioData;
        if(BGMSoruce != null && activeAudioData.audioClip != null) 
        {
            BGMSoruce.clip = activeAudioData.audioClip;
            BGMSoruce.time = 0;
            BGMSoruce.pitch = 1.0f;
            BGMSoruce.loop = false;
            BGMSoruce.playOnAwake = false;


            BGMSoruce.volume = masterVolume * bgmmasterVolume;
            Debug.Log($"楽曲データセット: {activeAudioData.musicTitle} (BPM: {activeAudioData.bpm})");

        }
    }

    /// <summary>
    /// エディタ用音楽再生
    /// </summary>
    public void PlayActiveMusic() 
    {
        if(BGMSoruce == null || BGMSoruce.clip == null) { return; }
        if(BGMSoruce.time >= BGMSoruce.clip.length) 
        {
            BGMSoruce.time = 0;
        }

        BGMSoruce.Play();
    }

    /// <summary>
    /// エディタ用再生スキップ
    /// </summary>
    /// <param name="seconds"></param>
    public void SkipTime(float seconds) 
    {
        if(BGMSoruce == null || BGMSoruce.clip == null) { return; }
        float targetTime = BGMSoruce.time + seconds;
        BGMSoruce.time = Mathf.Clamp(targetTime, 0f, BGMSoruce.clip.length);
    }

    //再生速度
    public void SetPitch(float pitch) 
    {
        if(BGMSoruce == null || BGMSoruce.clip == null) { return; }
        BGMSoruce.pitch = Mathf.Clamp(pitch, 0.25f, 3.0f);
    }

    //エディタ用
    public bool IsBGMPlaying() => BGMSoruce != null && BGMSoruce.isPlaying;
    public float GetCurrentTime() => BGMSoruce != null ? BGMSoruce.time : 0f;
    public float GetTotalDuration() => (BGMSoruce != null && BGMSoruce.clip != null) ? BGMSoruce.clip.length : 0f;
    public float GetCurrentPitch() => BGMSoruce != null ? BGMSoruce.pitch : 1.0f;
    public AudioDataSO GetActiveMusicData() => activeAudioData;

    /// <summary> /// SEを再生 /// </summary>
    /// <param name="categoryName"></param>
    /// <param name="se"></param>
    public void PlaySE(string categoryName, SESound.SEDATA se)
    {
        SECategory category = SECategorys.Find(category => category.categoryName == categoryName);
        //categoryがnullかどうかチェック
        if (category != null)
        {
            SESound date = category.sounds.Find(sound => sound.seData == se);
            if (date != null)
            {
                SESource.volume = date.seVolume * masterVolume * semasterVolume;
                SESource.PlayOneShot(date.seclip);
            }
            else
            {
                //SEの有無をチェック
                Debug.LogError("指定されたSEが見つかりません:" + se);
            }
        }
        else
        {
            //カテゴリーの有無をチェック
            Debug.LogError("指定されたカテゴリが見つかりません。" + categoryName);
        }
    }

    /// <summary> /// SEカテゴリー内のSEを再生 /// </summary>
    /// <param name="categoryName"></param>
    /// <param name="index"></param>
    public void PlayspecificSE(string categoryName, int index)
    {
        SECategory category = SECategorys.Find(category => category.categoryName == categoryName);
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
    /// <param name="categoryName">カテゴリー名</param>
    /// <param name="se">SE種類</param>
    public void PlayLoopSE(string categoryName, SESound.SEDATA se)
    {
        if (loopSESource == null) InitializeLoopSESource();

        SECategory category = SECategorys.Find(category => category.categoryName == categoryName);
        if (category != null)
        {
            SESound data = category.sounds.Find(sound => sound.seData == se);
            if (data != null)
            {
                loopSESource.clip = data.seclip;
                loopSESource.volume = data.seVolume * masterVolume * semasterVolume;
                loopSESource.loop = true;
                loopSESource.Play();
                Debug.Log($"【LoopSE開始】{categoryName} - {se}");
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
            Debug.Log("【LoopSE停止】");
        }
    }

    /// <summary>
    /// Pause all audio sources used by AudioManager
    /// </summary>
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

    /// <summary>
    /// Resume all audio sources previously paused
    /// </summary>
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

    /// <summary> /// マスター音量を設定（BGMとSE両方に適用）/// </summary>
    /// <param name="volume">0.0 - 1.0</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        Debug.Log($"【マスター音量設定】{masterVolume:F2}");
    }

    /// <summary> /// BGM音量を設定 /// </summary>
    /// <param name="volume">0.0 - 1.0</param>
    public void SetBGMVolume(float volume)
    {
        bgmmasterVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();
        Debug.Log($"【BGM音量設定】{bgmmasterVolume:F2}");
    }

    /// <summary> /// SE音量を設定 /// </summary>
    /// <param name="volume">0.0 - 1.0</param>
    public void SetSEVolume(float volume)
    {
        semasterVolume = Mathf.Clamp01(volume);
        UpdateSEVolume();
        Debug.Log($"【SE音量設定】{semasterVolume:F2}");
    }

    /// <summary> /// 現在のマスター音量を取得 /// </summary>
    public float GetMasterVolume() => masterVolume;

    /// <summary> /// 現在のBGM音量を取得 /// </summary>
    public float GetBGMVolume() => bgmmasterVolume;

    /// <summary> /// 現在のSE音量を取得  /// </summary>
    public float GetSEVolume() => semasterVolume;

    /// <summary> /// すべての音声の音量を更新 /// </summary>
    private void UpdateAllVolumes()
    {
        UpdateBGMVolume();
        UpdateSEVolume();
    }

    /// <summary> /// BGMの音量を更新（現在再生中なら即座に反映）/// </summary>
    private void UpdateBGMVolume()
    {
        if (BGMSoruce != null)
        {
            BGMSoruce.volume = masterVolume * bgmmasterVolume;
        }
    }

    /// <summary> /// SEの音量を更新（現在再生中なら即座に反映） /// </summary>
    private void UpdateSEVolume()
    {
        if (SESource != null)
        {
            SESource.volume = masterVolume * semasterVolume;
        }
        if (loopSESource != null)
        {
            loopSESource.volume = masterVolume * semasterVolume;
        }
    }



}

/// <summary>/// BGMデータの管理　/// </summary>
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

/// <summary> /// SEの音源データ管理 /// </summary>
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


/// <summary>　/// SEをカテゴリーごとに管理　/// </summary>
[Serializable]
public class SECategory
{
    //カテゴリ名で管理
    public string categoryName;
    public List<SESound> sounds = new List<SESound>();
}
