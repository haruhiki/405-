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
    [Tooltip("インゲームと同じNotsSpawnを指定すると移動時間(preSpawnTime)が完全一致します")]
    [SerializeField] private NotsSpawn notsSpawn;
    [SerializeField] private ChartEditorGrid editorGrid;

    [Header("プレビューレーン設定 (5レーン対応: Element 0?4)")]
    [Tooltip("インゲーム側のノーツ出現位置（エディタではノーツの到達・消滅地点）")]
    [SerializeField] private Transform[] laneSpawnPoints = new Transform[5];
    [Tooltip("インゲーム側の判定位置（エディタでは叩いた瞬間の出現地点）")]
    [SerializeField] private Transform[] laneJudgePoints = new Transform[5];
    [SerializeField] private float defaultPreSpawnTime = 2.0f;

    [Header("プレビュー表示の調整")]
    [Tooltip("ロングノーツのローカルY軸(trailObject)をレーン方向(Judge->Spawn)に自動回転させる")]
    [SerializeField] private bool alignLongTrailToLaneDirection = true;

    [Header("ノーツ種別ごとのPrefab設定（ロングはLong_Startに1つだけ設定）")]
    [SerializeField] private GameObject defaultPreviewPrefab;
    [SerializeField] private List<NotePrefabConfig> notePrefabConfigs = new List<NotePrefabConfig>()
    {
        new NotePrefabConfig { noteType = NoteDate.NotesType.Short, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Long_Start, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Both, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Flick, prefab = null },
        new NotePrefabConfig { noteType = NoteDate.NotesType.Damage, prefab = null }
    };

    [Header("GUI表示設定")]
    [SerializeField] private bool showEditorGUI = true;

    private List<SongDirectory> availableSongs = new List<SongDirectory>();
    private int selectedSongIndex = 0;
    private string currentMusicPath = "";
    private string currentSongTitle = "未ロード";
    private string statusMessage = "初期化中...";
    private Vector2 songScrollPos = Vector2.zero;

    public ChartData activeChartData = new ChartData();
    public float PrespawnTime => notsSpawn != null ? notsSpawn.preSpawnTime : defaultPreSpawnTime;
    public bool IsAudioReady { get; private set; } = false;

    private Dictionary<GameObject, List<GameObject>> previewPools = new Dictionary<GameObject, List<GameObject>>();

    // 入力中（押下中）のリアルタイム仮プレビュー用 (5レーン分)
    private GameObject[] activeHoldPreviewObjs = new GameObject[5];
    private float[] activeHoldStartTimes = new float[5];
    private bool[] activeHoldIsLong = new bool[5];

    private void Awake()
    {
        if (editorGrid == null)
        {
            editorGrid = GetComponent<ChartEditorGrid>();
        }

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            AudioManager found = FindObjectOfType<AudioManager>();
            if (found == null)
            {
                statusMessage = "エラー: シーンにAudioManagerがありません！";
                Debug.LogError("[ChartEditor] シーン上に AudioManager が配置されていません！");
                return;
            }
        }

        RefreshSongListAndLoad(0);
    }

    private void Update()
    {
        UpdatePreviewNotes();
        UpdateActiveHoldPreviews();
    }

    public void RefreshSongListAndLoad(int index = 0)
    {
        IsAudioReady = false;
        ClearAllPreviewObjects();
        CancelAllActiveHoldPreviews();
        if (editorGrid != null) editorGrid.ResetGridState();

        availableSongs = SongDirectoryManager.GetSongDirectories();

        if (availableSongs.Count == 0)
        {
            currentMusicPath = "";
            currentSongTitle = "曲フォルダなし (Assets/Songs/)";
            statusMessage = "Assets/Songs/[曲名]/ に音源を入れて再スキャンしてください";
            Debug.LogWarning("[ChartEditor] Assets/Songs/ 内に有効な曲フォルダが見つかりません。");
            return;
        }

        selectedSongIndex = Mathf.Clamp(index, 0, availableSongs.Count - 1);
        SongDirectory currentSong = availableSongs[selectedSongIndex];
        currentMusicPath = currentSong.folderPath;
        currentSongTitle = currentSong.folderName;
        statusMessage = $"音源ロード中: {currentSongTitle}...";

        if (AudioManager.Instance == null)
        {
            statusMessage = "エラー: AudioManager.Instance が null です";
            return;
        }

        StartCoroutine(AudioManager.Instance.LoadAudioClipFromPath(currentSong.audioPath, (success) =>
        {
            if (success)
            {
                IsAudioReady = true;
                LoadChartData(currentSong.jsonPath);
                statusMessage = "準備完了 (Spaceで再生/停止)";
                Debug.Log($"[ChartEditor] ロード完了: {currentSongTitle} ({currentSong.audioPath})");
            }
            else
            {
                IsAudioReady = false;
                statusMessage = $"ロード失敗: {currentSong.audioPath}";
                Debug.LogError($"[ChartEditor] 音源のロードに失敗しました: {currentSong.audioPath}");
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

    private void GetLaneEndpoints(int laneIndex, out Vector3 spawnPos, out Vector3 judgePos)
    {
        judgePos = (laneJudgePoints != null && laneIndex < laneJudgePoints.Length && laneJudgePoints[laneIndex] != null)
            ? laneJudgePoints[laneIndex].position
            : Vector3.zero;

        spawnPos = (laneSpawnPoints != null && laneIndex < laneSpawnPoints.Length && laneSpawnPoints[laneIndex] != null)
            ? laneSpawnPoints[laneIndex].position
            : judgePos + Vector3.right * 10f;

        if (Vector3.Distance(spawnPos, judgePos) < 0.01f)
        {
            spawnPos = judgePos + Vector3.right * 10f;
        }
    }

    private void AlignLongNoteOrientationForEditor(GameObject obj, Vector3 spawnPos, Vector3 judgePos)
    {
        if (!alignLongTrailToLaneDirection) return;

        Vector3 dirToJudge = (judgePos - spawnPos).normalized;
        if (dirToJudge.sqrMagnitude > 0.001f)
        {
            obj.transform.rotation = Quaternion.FromToRotation(Vector3.up, dirToJudge);
        }
    }

    /// <summary>
    /// キーを押した瞬間に呼ばれ、JudgePointのど真ん中から即座に仮プレビューを発射する
    /// </summary>
    public void BeginTapOrHoldPreview(int laneIndex, float startTime, NoteDate.NotesType initialType)
    {
        if (laneIndex < 0 || laneIndex >= 5) return;
        CancelHoldPreview(laneIndex);

        activeHoldStartTimes[laneIndex] = startTime;
        activeHoldIsLong[laneIndex] = false;

        GameObject prefab = GetPrefabForNoteType(initialType);
        if (prefab == null) prefab = defaultPreviewPrefab;
        if (prefab == null) return;

        GetLaneEndpoints(laneIndex, out Vector3 spawnPos, out Vector3 judgePos);

        GameObject obj = Instantiate(prefab, judgePos, Quaternion.identity, transform);
        DisableInGameScripts(obj);
        activeHoldPreviewObjs[laneIndex] = obj;
    }

    /// <summary>
    /// 押し続け時間がロング閾値を超えた瞬間、仮ショートノーツをロングノーツプレビューへ昇格させる
    /// </summary>
    public void PromotePreviewToLong(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= 5) return;

        float startTime = activeHoldStartTimes[laneIndex];
        CancelHoldPreview(laneIndex);

        activeHoldStartTimes[laneIndex] = startTime;
        activeHoldIsLong[laneIndex] = true;

        GameObject longPrefab = GetPrefabForNoteType(NoteDate.NotesType.Long_Start);
        if (longPrefab == null) longPrefab = defaultPreviewPrefab;
        if (longPrefab == null) return;

        GetLaneEndpoints(laneIndex, out Vector3 spawnPos, out Vector3 judgePos);

        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : startTime;
        float progress = Mathf.Max(0f, (currentTime - startTime) / PrespawnTime);
        Vector3 currentPos = Vector3.LerpUnclamped(judgePos, spawnPos, progress);

        GameObject obj = Instantiate(longPrefab, currentPos, Quaternion.identity, transform);
        DisableInGameScripts(obj);
        AlignLongNoteOrientationForEditor(obj, spawnPos, judgePos);
        activeHoldPreviewObjs[laneIndex] = obj;

        if (obj.TryGetComponent<NotesCon>(out var notesCon))
        {
            NoteDate.Notes tempNote = new NoteDate.Notes
            {
                lane = laneIndex,
                targetTime = startTime,
                noteType = NoteDate.NotesType.Long_Start
            };
            obj.transform.position = spawnPos;
            notesCon.Init(tempNote, PrespawnTime, judgePos);
            notesCon.SetEndTime(currentTime);
            obj.transform.position = currentPos;
        }
    }

    public void CancelHoldPreview(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= 5) return;
        if (activeHoldPreviewObjs[laneIndex] != null)
        {
            Destroy(activeHoldPreviewObjs[laneIndex]);
            activeHoldPreviewObjs[laneIndex] = null;
        }
        activeHoldIsLong[laneIndex] = false;
    }

    private void CancelAllActiveHoldPreviews()
    {
        for (int i = 0; i < activeHoldPreviewObjs.Length; i++)
        {
            CancelHoldPreview(i);
        }
    }

    /// <summary>
    /// 押下中の仮ノーツ（ショート／ロング両方）を毎フレーム滑らかに前進・伸長させる
    /// </summary>
    private void UpdateActiveHoldPreviews()
    {
        if (!IsAudioReady || AudioManager.Instance == null) return;
        float currentTime = AudioManager.Instance.GetCurrentTime();
        float preSpawn = PrespawnTime;

        for (int i = 0; i < activeHoldPreviewObjs.Length; i++)
        {
            GameObject obj = activeHoldPreviewObjs[i];
            if (obj == null) continue;

            float startTime = activeHoldStartTimes[i];
            float duration = Mathf.Max(0f, currentTime - startTime);
            GetLaneEndpoints(i, out Vector3 spawnPos, out Vector3 judgePos);

            float progress = Mathf.Max(0f, (currentTime - startTime) / preSpawn);
            obj.transform.position = Vector3.LerpUnclamped(judgePos, spawnPos, progress);

            if (activeHoldIsLong[i])
            {
                if (obj.TryGetComponent<NotesCon>(out var notesCon))
                {
                    notesCon.SetEndTime(startTime + duration);
                }
                else
                {
                    float totalDist = Vector3.Distance(spawnPos, judgePos);
                    float dist = totalDist * (duration / preSpawn);
                    Vector3 s = obj.transform.localScale;
                    s.y = Mathf.Max(0.2f, dist);
                    obj.transform.localScale = s;
                }
            }
        }
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

        statusMessage = $"ロング登録: Lane {laneIndex} ({snappedStart:F2}s ? {snappedEnd:F2}s)";
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

        statusMessage = $"登録: {type} | Lane {laneIndex} | {finalTime:F2}s";
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
            statusMessage = "保存失敗: 曲フォルダ未選択";
            return;
        }

        string savePath = Path.Combine(currentMusicPath, "chart.json");
        ChartInputOutPut_Json.SaveChartData(savePath, activeChartData);

        if (File.Exists(savePath))
        {
            statusMessage = $"保存完了！ ({activeChartData.notes.Count} ノーツ)";
            Debug.Log($"[ChartEditor] JSON保存完了: {savePath} (ノーツ数: {activeChartData.notes.Count})");
        }
        else
        {
            statusMessage = "保存エラー: ファイル書き出し失敗";
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
        }
        else
        {
            activeChartData = new ChartData();
            if (activeChartData.bpm <= 0) activeChartData.bpm = 120f;
        }
    }

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

            if (note.noteType == NoteDate.NotesType.Long_End) continue;

            float noteEndTime = (note.noteType == NoteDate.NotesType.Long_Start)
                ? note.targetTime + note.Length
                : note.targetTime;

            if (currentTime < note.targetTime - 0.05f || currentTime > noteEndTime + preSpawn)
            {
                continue;
            }

            if (note.lane < 0 || note.lane >= laneSpawnPoints.Length || note.lane >= laneJudgePoints.Length) continue;

            GameObject targetPrefab = GetPrefabForNoteType(note.noteType);
            if (targetPrefab == null) continue;

            GameObject previewObj = GetOrCreatePooledObject(targetPrefab, usedCountPerPrefab);
            previewObj.SetActive(true);

            GetLaneEndpoints(note.lane, out Vector3 spawnPos, out Vector3 judgePos);

            if (note.noteType == NoteDate.NotesType.Long_Start)
            {
                AlignLongNoteOrientationForEditor(previewObj, spawnPos, judgePos);

                if (previewObj.TryGetComponent<NotesCon>(out var notesCon))
                {
                    previewObj.transform.position = spawnPos;
                    notesCon.Init(note, preSpawn, judgePos);
                    notesCon.SetEndTime(note.targetTime + note.Length);
                }
            }

            // 未来拍スナップ時に判定ラインより手前へ飛び出さないよう 0f 以上にクランプ
            float progress = Mathf.Max(0f, (currentTime - note.targetTime) / preSpawn);
            previewObj.transform.position = Vector3.LerpUnclamped(judgePos, spawnPos, progress);
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
            if (scripts[i] != null && !(scripts[i] is Graphic))
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

    private void OnGUI()
    {
        if (!showEditorGUI) return;

        float panelWidth = 400f;
        float panelHeight = 445f;
        float margin = 10f;

        float startX = Screen.width - panelWidth - margin;
        float startY = Screen.height - panelHeight - margin;

        GUI.BeginGroup(new Rect(startX, startY, panelWidth, panelHeight));

        GUI.Box(new Rect(0, 0, panelWidth, panelHeight), "? 譜面作成エディタ (5レーン対応)");

        GUI.Label(new Rect(10, 25, 380, 22), $"選択中 [{selectedSongIndex + 1}/{Mathf.Max(1, availableSongs.Count)}]: {currentSongTitle}");

        if (GUI.Button(new Rect(10, 50, 115, 25), "? 前の曲"))
        {
            SelectPrevSong();
        }
        if (GUI.Button(new Rect(135, 50, 115, 25), "次の曲 ?"))
        {
            SelectNextSong();
        }
        if (GUI.Button(new Rect(260, 50, 130, 25), "? 再スキャン"))
        {
            RefreshSongListAndLoad(selectedSongIndex);
        }

        if (availableSongs.Count > 0)
        {
            GUI.Box(new Rect(10, 80, 380, 85), "");
            songScrollPos = GUI.BeginScrollView(new Rect(12, 82, 376, 81), songScrollPos, new Rect(0, 0, 355, availableSongs.Count * 24));
            for (int i = 0; i < availableSongs.Count; i++)
            {
                string prefix = (i == selectedSongIndex) ? "? " : "  ";
                if (GUI.Button(new Rect(2, i * 24, 350, 22), $"{prefix}{availableSongs[i].folderName}"))
                {
                    RefreshSongListAndLoad(i);
                }
            }
            GUI.EndScrollView();
        }

        float currentTime = (AudioManager.Instance != null) ? AudioManager.Instance.GetCurrentTime() : 0f;
        float totalTime = (AudioManager.Instance != null) ? AudioManager.Instance.GetTotalDuration() : 0f;
        bool isPlaying = (AudioManager.Instance != null) && AudioManager.Instance.IsBGMPlaying();

        string stateLabel = isPlaying ? "<color=lime>? 再生中</color>" : "<color=yellow>? 停止中</color>";
        GUI.Label(new Rect(10, 172, 380, 22), $"状態: {stateLabel}   時間: {currentTime:F2}s / {totalTime:F2}s");
        GUI.Label(new Rect(10, 194, 380, 22), $"BPM: {activeChartData.bpm}   記録ノーツ数: {activeChartData.notes.Count}");
        GUI.Label(new Rect(10, 216, 380, 22), $"ステータス: {statusMessage}");

        if (editorGrid != null)
        {
            string snapText = editorGrid.isSnapEnabled ? $"スナップ: ON (1/{editorGrid.beatDivision})" : "スナップ: OFF";
            if (GUI.Button(new Rect(10, 245, 185, 25), snapText))
            {
                editorGrid.isSnapEnabled = !editorGrid.isSnapEnabled;
            }
            if (GUI.Button(new Rect(205, 245, 185, 25), "分割変更 (4/8/16/32)"))
            {
                editorGrid.CycleDivision();
            }
        }

        GUI.Label(new Rect(10, 280, 380, 20), "【キーボード操作】");
        GUI.Label(new Rect(10, 300, 380, 20), "Space : 再生 / 一時停止");
        GUI.Label(new Rect(10, 320, 380, 20), "D / F / G / H / J : 5レーン入力 (長押しでロング)");
        GUI.Label(new Rect(10, 340, 380, 20), "複数同時押し : 自動で Both 登録");
        GUI.Label(new Rect(10, 360, 380, 20), "← / → : 5秒スキップ");
        GUI.Label(new Rect(10, 380, 380, 20), "F5 : chart.json に保存");

        if (GUI.Button(new Rect(10, 402, 380, 26), "? 譜面を保存 (F5)"))
        {
            SaveChartData();
        }

        GUI.EndGroup();
    }
}