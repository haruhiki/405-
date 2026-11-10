using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChartEditorManager : MonoBehaviour
{
    [Header("JSON管理譜面データ")]
    public ChartData activeChartData = new ChartData(); // 譜面データの管理用インスタンス
    [SerializeField] private string currentSaveFileName = "NewChartData.json"; // 現在の保存ファイル名   

    [Header("プレビューノーツ生成設定")]
    [SerializeField] private GameObject shortNotePrefab;
    [SerializeField] private GameObject longNotePrefab;
    [SerializeField] private GameObject rushNotePrefab;
    [SerializeField] private GameObject bothNotePrefab;

    [Header("レーン・位置設定")]
    [SerializeField] private Transform[] spawnPoints;   // ノーツ生成位置の配列
    [SerializeField] private Transform[] targetCircles; // 判定円の位置の配列

    // TODO: 同時押しは現状実装しているが、将来的には削除するかも？
    [SerializeField] private Transform bothSpawnPoint;   // 同時押しノーツの生成位置
    [SerializeField] private Transform bothTargetCircle; // 同時押しノーツの判定円位置

    [Header("生成パラメータ")]
    [SerializeField] private float preSpawnTime = 1.0f; // ノーツ生成の事前時間

    // TODO: 今回はキーボード入力でのテスト実装だが、将来的なことを考えて残しておく
    [Header("シーン上のオブジェクト参照")]
    [SerializeField] private NotsSpawn notsSpawn;

    // TODO: ラッシュノーツの自動連打は、将来的には削除するかも？
    [Header("ラッシュ自動連打設定")]
    [SerializeField] private float rushInterval = 0.1f;

    [Header("楽曲フォルダ管理")]
    public List<SongDirectory> availableSongs = new List<SongDirectory>();
    private int currentMusicIndex = 0;
    private string currentMusicPath = "";

    private bool isPreviewPlaying = false;
    private int playbackIndex = 0;
    private float lastCheckedTime = 0f;

    private void Start()
    {
        // 起動時に利用可能な楽曲データを取得・ロード
        RefleshSongListAndLoad(0);
    }

    private void Update()
    {
        if (AudioManager.Instance == null) { return; }
        float currentTime = AudioManager.Instance.GetCurrentTime();

        if (isPreviewPlaying && AudioManager.Instance.IsBGMPlaying())
        {
            UpdateChartPlayback(currentTime);
        }
        lastCheckedTime = currentTime;
    }

    /// <summary>
    /// 利用可能な曲リストを更新し、指定インデックスの曲をロードする
    /// </summary>
    /// <param name="index">ロードする曲のインデックス</param>
    public void RefleshSongListAndLoad(int index)
    {
        availableSongs = SongDirectoryManager.GetSongDirectories();
        if (availableSongs == null || availableSongs.Count <= 0)
        {
            Debug.LogWarning("[ChartEditor] 音源データがSongフォルダに存在しません。");
            return;
        }

        currentMusicIndex = Mathf.Clamp(index, 0, availableSongs.Count - 1);
        SongDirectory targetSong = availableSongs[currentMusicIndex];
        currentMusicPath = targetSong.folderPath;

        // コルーチンを起動して音源とJSONを実際にロード
        StopAllCoroutines();
        StartCoroutine(LoadSongRoutine(targetSong));
    }

    private IEnumerator LoadSongRoutine(SongDirectory songDir)
    {
        // 1. JSON譜面データのロード
        if (File.Exists(songDir.jsonPath))
        {
            activeChartData = ChartInputOutPut_Json.LoadChartData(songDir.jsonPath);
            Debug.Log($"[ChartEditor] JSON譜面ロード成功: {songDir.jsonPath}");
        }
        else
        {
            activeChartData = new ChartData
            {
                title = songDir.folderName
            };
            Debug.Log($"[ChartEditor] 新規譜面データ作成: {songDir.folderName}");
        }

        // 2. 音源ファイル（AudioClip）のロード
        if (!string.IsNullOrEmpty(songDir.audioPath) && AudioManager.Instance != null)
        {
            yield return AudioManager.Instance.LoadAudioClipFromPath(songDir.audioPath);
        }

        // 3. 再生インデックスのリセット
        if (AudioManager.Instance != null)
        {
            ResetPlaybackIndexToTime(AudioManager.Instance.GetCurrentTime());
        }

        Debug.Log($"[ChartEditor] 楽曲読み込み完了: {songDir.folderName}");
    }

    /// <summary>
    /// ノーツを譜面データに追加し、プレビューを生成
    /// </summary>
    public void AddNote(float time, int lane, int type, float length = 0f)
    {
        NoteDate.Notes noteToAdd = new NoteDate.Notes
        {
            id = activeChartData.notes.Count + 1,
            lane = lane,
            targetTime = time,
            noteType = (NoteDate.NotesType)type,
            Length = length
        };

        activeChartData.notes.Add(noteToAdd);
        activeChartData.SortNotesByTargetTime();

        SpawnSinglePreview(noteToAdd, time);
    }

    /// <summary>
    /// プレビュー自動再生
    /// </summary>
    private void UpdateChartPlayback(float currentTime)
    {
        var notes = activeChartData.notes;
        if (notes.Count == 0 || playbackIndex >= notes.Count) { return; }

        if (currentTime < lastCheckedTime)
        {
            ResetPlaybackIndexToTime(currentTime);
        }

        while (playbackIndex < notes.Count &&
               currentTime >= notes[playbackIndex].targetTime - preSpawnTime)
        {
            var note = notes[playbackIndex];
            SpawnSinglePreview(note, currentTime);
            playbackIndex++;
        }
    }

    public float PrespawnTime => preSpawnTime;

    /// <summary>
    /// 打ち込み開始時にリアルタイムロングノーツオブジェクトを生成
    /// </summary>
    public NotesCon SpawnHandlePreview(int lane, float currentTime)
    {
        if (longNotePrefab == null) return null;

        Vector3 spawnPos = GetSpawnPositionForLane(lane, NoteDate.NotesType.Long_Start);
        Vector3 targetPos = GetTargetPosition(lane, NoteDate.NotesType.Long_Start);

        GameObject obj = Instantiate(longNotePrefab, spawnPos, Quaternion.identity);
        NotesCon noteCon = obj.GetComponent<NotesCon>();

        if (noteCon != null)
        {
            NoteDate.Notes tempNote = new NoteDate.Notes
            {
                lane = lane,
                targetTime = currentTime + preSpawnTime,
                noteType = NoteDate.NotesType.Long_Start
            };

            noteCon.Init(tempNote, preSpawnTime, targetPos);
            noteCon.SetEndTime(tempNote.targetTime);
        }
        return noteCon;
    }

    /// <summary>
    /// 単一のプレビューノーツを生成するメソッド
    /// </summary>
    private void SpawnSinglePreview(NoteDate.Notes note, float currentTime)
    {
        GameObject prefab = GetPrefabForNoteType(note.noteType);
        if (prefab == null) { return; }

        Vector3 spawnPos = GetSpawnPositionForLane(note.lane, note.noteType);
        Vector3 targetPos = GetTargetPosition(note.lane, note.noteType);

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

        NotesCon noteCon = obj.GetComponent<NotesCon>();
        float currentPrespawn = notsSpawn != null ? notsSpawn.preSpawnTime : preSpawnTime;

        if (noteCon != null)
        {
            NoteDate.Notes tempNote = new NoteDate.Notes
            {
                lane = note.lane,
                targetTime = note.targetTime,
                noteType = note.noteType
            };
            noteCon.Init(tempNote, currentPrespawn, targetPos);

            if (note.noteType == NoteDate.NotesType.Long_Start)
            {
                noteCon.SetEndTime(tempNote.targetTime + note.Length);
            }
        }
        Destroy(obj, currentPrespawn + note.Length + 0.5f);
    }

    /// <summary>
    /// レーン・ノーツ種別に応じた生成位置を取得
    /// </summary>
    private Vector3 GetSpawnPositionForLane(int lane, NoteDate.NotesType noteType)
    {
        if (noteType == NoteDate.NotesType.Both || noteType == NoteDate.NotesType.Rush)
        {
            if (bothSpawnPoint != null) { return bothSpawnPoint.position; }
            if (spawnPoints != null && spawnPoints.Length > 1)
            {
                return (spawnPoints[0].position + spawnPoints[1].position) * 0.5f;
            }
        }

        if (spawnPoints != null && lane >= 0 && lane < spawnPoints.Length)
        {
            return spawnPoints[lane].position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// レーン・ノーツ種別に応じたターゲット位置を取得
    /// </summary>
    private Vector3 GetTargetPosition(int lane, NoteDate.NotesType noteType)
    {
        if (noteType == NoteDate.NotesType.Both || noteType == NoteDate.NotesType.Rush)
        {
            if (bothTargetCircle != null) { return bothTargetCircle.position; }
            if (targetCircles != null && targetCircles.Length > 1)
            {
                return (targetCircles[0].position + targetCircles[1].position) * 0.5f;
            }
        }

        if (targetCircles != null && lane >= 0 && lane < targetCircles.Length)
        {
            return targetCircles[lane].position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 譜面データをJSONファイルに保存
    /// </summary>
    public void SaveChartData(string fileName = "")
    {
        if (string.IsNullOrEmpty(fileName)) { fileName = currentSaveFileName; }
        string savePath = GetSaveFilePath(fileName);
        ChartInputOutPut_Json.SaveChartData(savePath, activeChartData);
        Debug.Log($"[ChartEditor] 譜面を保存しました: {savePath}");
    }

    /// <summary>
    /// 譜面データをJSONファイルからロード
    /// </summary>
    public void LoadChartData(string fileName = "")
    {
        if (string.IsNullOrEmpty(fileName)) { fileName = currentSaveFileName; }
        string loadPath = GetSaveFilePath(fileName);
        activeChartData = ChartInputOutPut_Json.LoadChartData(loadPath);
        if (AudioManager.Instance != null)
        {
            ResetPlaybackIndexToTime(AudioManager.Instance.GetCurrentTime());
        }
        Debug.Log($"[ChartEditor] 譜面をロードしました: {loadPath}");
    }

    private string GetSaveFilePath(string fileName)
    {
        if(!string.IsNullOrEmpty(currentMusicPath) && Directory.Exists(currentMusicPath))
        {
            return Path.Combine(currentMusicPath,"chart.json");
        }

        string folder = Path.Combine(Application.dataPath, "Songs");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return Path.Combine(folder, "chart.json");
    }

    /// <summary>
    /// 再生位置を指定時間にリセット
    /// </summary>
    private void ResetPlaybackIndexToTime(float time)
    {
        playbackIndex = 0;
        var notes = activeChartData.notes;
        while (playbackIndex < notes.Count && notes[playbackIndex].targetTime - preSpawnTime < time)
        {
            playbackIndex++;
        }
    }

    /// <summary>
    /// ノーツの種類に応じたプレハブを取得
    /// </summary>
    private GameObject GetPrefabForNoteType(NoteDate.NotesType noteType)
    {
        switch (noteType)
        {
            case NoteDate.NotesType.Short:
                return shortNotePrefab;
            case NoteDate.NotesType.Long_Start:
                return longNotePrefab;
            case NoteDate.NotesType.Rush:
                return rushNotePrefab;
            case NoteDate.NotesType.Both:
                return bothNotePrefab;
            default:
                return null;
        }
    }

    /// <summary>
    /// エディタ用画面表示 UI
    /// </summary>
    private void OnGUI()
    {
        if (AudioManager.Instance == null) { return; }
        AudioDataSO activeData = AudioManager.Instance.GetActiveMusicData();

        GUI.Box(new Rect(10, 10, 380, 190), "? 譜面作成エディタ");
        float currentTime = AudioManager.Instance.GetCurrentTime();

        string currentSongName = availableSongs.Count > currentMusicIndex ? availableSongs[currentMusicIndex].folderName : "N/A";
        GUI.Label(new Rect(20, 35, 360, 20), $"選択中の曲: {currentSongName}");
        GUI.Label(new Rect(20, 60, 360, 20), $"再生時間: {currentTime:F3}s");
        GUI.Label(new Rect(20, 85, 360, 20), $"登録ノーツ数: {activeChartData.notes.Count}");

        // 曲切り替えUIボタン
        if (GUI.Button(new Rect(20, 115, 100, 30), "? 前の曲"))
        {
            RefleshSongListAndLoad(currentMusicIndex - 1);
        }
        if (GUI.Button(new Rect(130, 115, 100, 30), "次の曲 ?"))
        {
            RefleshSongListAndLoad(currentMusicIndex + 1);
        }
        if (GUI.Button(new Rect(240, 115, 120, 30), "リスト更新"))
        {
            RefleshSongListAndLoad(currentMusicIndex);
        }

        GUI.Label(new Rect(20, 155, 360, 20), "Ctrl+S: 保存 | Ctrl+L: ロード | Space: 再生/停止");
    }
}