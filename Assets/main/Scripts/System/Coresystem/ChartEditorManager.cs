using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct NotePrefabConfig
{
    public NoteDate.NotesType noteType;
    public GameObject prefab;
}

public class ChartEditorManager : MonoBehaviour
{
    [Header("シーン上のオブジェクト参照")]
    [SerializeField] private NotsSpawn notsSpawn;
    [SerializeField] private ChartEditorGrid editorGrid;
    [SerializeField] private Text currentSongText;
    [SerializeField] private Text timeDisplayText;

    [Header("プレビューレーン設定 (5レーン対応: Element 0?4)")]
    [SerializeField] private Transform[] laneSpawnPoints = new Transform[5];
    [SerializeField] private Transform[] laneJudgePoints = new Transform[5];
    [SerializeField] private float defaultPreSpawnTime = 2.0f;

    [Header("ノーツ種別ごとのPrefab設定（ロングはLong_Startに1つだけ設定）")]
    [Tooltip("未設定のNotesTypeが来たときの予備Prefab")]
    [SerializeField] private GameObject defaultPreviewPrefab;
    [SerializeField] private List<NotePrefabConfig> notePrefabConfigs = new List<NotePrefabConfig>()
    {
        new NotePrefabConfig { noteType = NoteDate.NotesType.Short, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Long_Start, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Both, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Flick, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Damage, prefab = null }
    };

    private List<SongDirectory> availableSongs = new List<SongDirectory>();
    private int selectedSongIndex = 0;
    private string currentMusicPath = "";

    public ChartData activeChartData = new ChartData();
    public float PrespawnTime => notsSpawn != null ? notsSpawn.preSpawnTime : defaultPreSpawnTime;
    public bool IsAudioReady { get; private set; } = false;

    private Dictionary<GameObject, List<GameObject>> previewPools = new Dictionary<GameObject, List<GameObject>>();

    private void Awake()
    {
        if (editorGrid == null)
        {
            editorGrid = GetComponent<ChartEditorGrid>();
        }
    }

    private void Start()
    {
        RefreshSongListAndLoad(0);
    }

    private void Update()
    {
        UpdateTimeDisplay();
        UpdatePreviewNotes();
    }

    public void RefreshSongListAndLoad(int index = 0)
    {
        IsAudioReady = false;
        ClearAllPreviewObjects();
        if (editorGrid != null) editorGrid.ResetGridState();

        availableSongs = SongDirectoryManager.GetSongDirectories();

        if (availableSongs.Count == 0)
        {
            currentMusicPath = "";
            if (currentSongText != null) currentSongText.text = "N/A";
            Debug.LogWarning("[ChartEditor] Assets/Songs/ 内に有効な曲フォルダが見つかりません。");
            return;
        }

        selectedSongIndex = Mathf.Clamp(index, 0, availableSongs.Count - 1);
        SongDirectory currentSong = availableSongs[selectedSongIndex];
        currentMusicPath = currentSong.folderPath;

        if (currentSongText != null)
        {
            currentSongText.text = currentSong.folderName;
        }

        StartCoroutine(AudioManager.Instance.LoadAudioClipFromPath(currentSong.audioPath, (success) =>
        {
            if (success)
            {
                IsAudioReady = true;
                LoadChartData(currentSong.jsonPath);
            }
            else
            {
                IsAudioReady = false;
                Debug.LogError($"[ChartEditor] 音源のロードに失敗しました: {currentSong.folderName}");
            }
        }));
    }

    public void SelectNextSong()
    {
        if (availableSongs.Count == 0) return;
        int nextIndex = (selectedSongIndex + 1) % availableSongs.Count;
        RefreshSongListAndLoad(nextIndex);
    }

    public void SelectPrevSong()
    {
        if (availableSongs.Count == 0) return;
        int prevIndex = (selectedSongIndex - 1 + availableSongs.Count) % availableSongs.Count;
        RefreshSongListAndLoad(prevIndex);
    }

    public void AddNoteAtTime(int laneIndex, float time, NoteDate.NotesType type, float length = 0f)
    {
        if (!IsAudioReady || AudioManager.Instance == null) return;
        InsertNoteInternal(laneIndex, time, type, length);
    }

    public void AddLongNote(int laneIndex, float startTime, float endTime, float length)
    {
        if (!IsAudioReady || AudioManager.Instance == null) return;

        float snappedStart = ApplySnapIfEnabled(startTime);
        float snappedEnd = ApplySnapIfEnabled(endTime);
        float finalLength = Mathf.Max(0.05f, snappedEnd - snappedStart);

        InsertNoteInternal(laneIndex, snappedStart, NoteDate.NotesType.Long_Start, finalLength, false);
        InsertNoteInternal(laneIndex, snappedEnd, NoteDate.NotesType.Long_End, 0f, false);

        Debug.Log($"[ChartEditor] ロングノーツ確定 - Lane: {laneIndex}, Start: {snappedStart:F3}s, End: {snappedEnd:F3}s, Length: {finalLength:F3}s");
    }

    public bool TryMarkSimultaneousNoteAsBoth(float targetTime, float threshold)
    {
        float checkTime = ApplySnapIfEnabled(targetTime);
        bool found = false;
        for (int i = 0; i < activeChartData.notes.Count; i++)
        {
            NoteDate.Notes note = activeChartData.notes[i];
            if (Mathf.Abs(note.targetTime - checkTime) <= threshold &&
                (note.noteType == NoteDate.NotesType.Short || note.noteType == NoteDate.NotesType.Both))
            {
                note.noteType = NoteDate.NotesType.Both;
                activeChartData.notes[i] = note;
                found = true;
            }
        }
        return found;
    }

    private void InsertNoteInternal(int laneIndex, float time, NoteDate.NotesType type, float length, bool applySnap = true)
    {
        float finalTime = applySnap ? ApplySnapIfEnabled(time) : time;

        Vector3 targetPos = (laneJudgePoints != null && laneIndex >= 0 && laneIndex < laneJudgePoints.Length && laneJudgePoints[laneIndex] != null)
            ? laneJudgePoints[laneIndex].position
            : Vector3.zero;

        NoteDate.Notes newNote = new NoteDate.Notes
        {
            id = activeChartData.notes.Count,
            targetTime = finalTime,
            lane = laneIndex,
            targetPosition = targetPos,
            noteType = type,
            Length = length
        };

        activeChartData.notes.Add(newNote);
        SortAndReindexNotes();

        Debug.Log($"[ChartEditor] ノーツ登録 - Lane: {laneIndex}, Type: {type}, Time: {finalTime:F3}s, Length: {length:F2}s (総数: {activeChartData.notes.Count})");
    }

    private float ApplySnapIfEnabled(float rawTime)
    {
        if (editorGrid != null && editorGrid.isSnapEnabled && activeChartData != null && activeChartData.bpm > 0)
        {
            return editorGrid.SnapTime(rawTime, activeChartData.bpm);
        }
        return rawTime;
    }

    private void SortAndReindexNotes()
    {
        activeChartData.notes.Sort((a, b) => a.targetTime.CompareTo(b.targetTime));
        for (int i = 0; i < activeChartData.notes.Count; i++)
        {
            NoteDate.Notes temp = activeChartData.notes[i];
            temp.id = i;
            activeChartData.notes[i] = temp;
        }
    }

    public void SaveChartData()
    {
        if (string.IsNullOrEmpty(currentMusicPath))
        {
            Debug.LogWarning("[ChartEditor] 保存先の曲フォルダが選択されていません。");
            return;
        }

        string savePath = Path.Combine(currentMusicPath, "chart.json");
        ChartInputOutPut_Json.SaveChartData(savePath, activeChartData);

        if (File.Exists(savePath))
        {
            Debug.Log($"[ChartEditor] JSON保存完了: {savePath} (ノーツ数: {activeChartData.notes.Count})");
        }
        else
        {
            Debug.LogError($"[ChartEditor] JSONの書き出しに失敗しました: {savePath}");
        }
    }

    private void LoadChartData(string jsonPath)
    {
        if (File.Exists(jsonPath))
        {
            activeChartData = ChartInputOutPut_Json.LoadChartData(jsonPath);
            if (activeChartData == null) activeChartData = new ChartData();
            if (activeChartData.notes == null) activeChartData.notes = new List<NoteDate.Notes>();
            if (activeChartData.bpm <= 0) activeChartData.bpm = 120f;

            Debug.Log($"[ChartEditor] 既存JSONロード完了: {activeChartData.notes.Count} ノーツ (BPM: {activeChartData.bpm})");
        }
        else
        {
            activeChartData = new ChartData();
            if (activeChartData.bpm <= 0) activeChartData.bpm = 120f;
            Debug.Log($"[ChartEditor] 新規譜面として初期化しました (デフォルトBPM: {activeChartData.bpm})。");
        }
    }

    private void UpdateTimeDisplay()
    {
        if (timeDisplayText == null || AudioManager.Instance == null) return;

        float current = AudioManager.Instance.GetCurrentTime();
        float total = AudioManager.Instance.GetTotalDuration();
        timeDisplayText.text = $"{current:F2}s / {total:F2}s";
    }

    /// <summary>
    /// NotesTypeごとに設定されたPrefabを使って現在表示範囲のノーツを描画
    /// ロングノーツは Long_Start の1つのPrefabで始点?終点（targetTime + Length）を表現
    /// </summary>
    private void UpdatePreviewNotes()
    {
        if (!IsAudioReady || laneSpawnPoints == null || laneJudgePoints == null) return;

        Dictionary<GameObject, int> usedCountPerPrefab = new Dictionary<GameObject, int>();
        foreach (var kvp in previewPools)
        {
            usedCountPerPrefab[kvp.Key] = 0;
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                if (kvp.Value[i] != null) kvp.Value[i].SetActive(false);
            }
        }

        float currentTime = AudioManager.Instance.GetCurrentTime();
        float preSpawn = PrespawnTime;

        for (int i = 0; i < activeChartData.notes.Count; i++)
        {
            NoteDate.Notes note = activeChartData.notes[i];

            // ロングノーツは1つのPrefabに収まっているため、Long_End単体では描画しない
            if (note.noteType == NoteDate.NotesType.Long_End) continue;

            // ロングノーツの場合は終端（targetTime + Length）が過ぎるまで表示範囲に含める
            float noteEndTime = (note.noteType == NoteDate.NotesType.Long_Start)
                ? note.targetTime + note.Length
                : note.targetTime;

            if (noteEndTime < currentTime - 0.1f || note.targetTime > currentTime + preSpawn)
            {
                continue;
            }

            if (note.lane < 0 || note.lane >= laneSpawnPoints.Length || note.lane >= laneJudgePoints.Length) continue;
            if (laneSpawnPoints[note.lane] == null || laneJudgePoints[note.lane] == null) continue;

            GameObject targetPrefab = GetPrefabForNoteType(note.noteType);
            if (targetPrefab == null) continue;

            GameObject previewObj = GetOrCreatePooledObject(targetPrefab, usedCountPerPrefab);
            previewObj.SetActive(true);

            Vector3 spawnPos = laneSpawnPoints[note.lane].position;
            Vector3 judgePos = laneJudgePoints[note.lane].position;

            float progress = 1f - ((note.targetTime - currentTime) / preSpawn);
            previewObj.transform.position = Vector3.LerpUnclamped(spawnPos, judgePos, progress);

            // NotesConが付いているPrefabなら、以前のChretCreateManagerと同様にInit＆SetEndTimeで長さを反映
            if (note.noteType == NoteDate.NotesType.Long_Start && previewObj.TryGetComponent<NotesCon>(out var notesCon))
            {
                NoteDate.Notes previewData = note;
                notesCon.Init(previewData, preSpawn, judgePos);
                notesCon.SetEndTime(note.targetTime + note.Length);
            }
        }
    }

    private GameObject GetPrefabForNoteType(NoteDate.NotesType type)
    {
        for (int i = 0; i < notePrefabConfigs.Count; i++)
        {
            if (notePrefabConfigs[i].noteType == type && notePrefabConfigs[i].prefab != null)
            {
                return notePrefabConfigs[i].prefab;
            }
        }
        return defaultPreviewPrefab;
    }

    private GameObject GetOrCreatePooledObject(GameObject prefab, Dictionary<GameObject, int> usedCounts)
    {
        if (!previewPools.ContainsKey(prefab))
        {
            previewPools[prefab] = new List<GameObject>();
        }
        if (!usedCounts.ContainsKey(prefab))
        {
            usedCounts[prefab] = 0;
        }

        List<GameObject> pool = previewPools[prefab];
        int index = usedCounts[prefab];

        while (pool.Count <= index)
        {
            GameObject instance = Instantiate(prefab, transform);
            DisableInGameScripts(instance);
            pool.Add(instance);
        }

        usedCounts[prefab] = index + 1;
        return pool[index];
    }

    private void DisableInGameScripts(GameObject obj)
    {
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            // UI表示(Image等)とロングノーツ形状更新用のNotesConは残し、それ以外の移動・判定スクリプトを無効化
            if (scripts[i] != null && !(scripts[i] is Graphic) && !(scripts[i] is NotesCon))
            {
                scripts[i].enabled = false;
            }
        }

        Collider[] colliders3D = obj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders3D.Length; i++)
        {
            colliders3D[i].enabled = false;
        }

        Collider2D[] colliders2D = obj.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
        {
            colliders2D[i].enabled = false;
        }
    }

    private void ClearAllPreviewObjects()
    {
        foreach (var kvp in previewPools)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj != null) Destroy(obj);
            }
        }
        previewPools.Clear();
    }
}