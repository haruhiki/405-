using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChretCreateManager : MonoBehaviour
{
    [Header("タイム操作設定")] 
    [SerializeField] private float skipSeconds = 5.0f;

    [Header("プレビューノーツ生成設定")]
    [SerializeField] private GameObject shortNotePrefab;
    [SerializeField] private GameObject longNotePrefab;
    [SerializeField] private GameObject rushNotePrefab;
    [SerializeField] private GameObject bothNotePrefab;

    [Header("中央ライン（Both / Rush用）の位置設定")]
    [SerializeField] private Transform bothSpawnPoint;
    [SerializeField] private Transform bothTargetCircle;

    [Header("シーン上のオブジェクト参照")]
    [SerializeField] private NotsSpawn notsSpawn;

    [Header("ラッシュ自動連打設定")]
    [SerializeField] private float rushInterval = 0.1f;

    private List<RecordedNoteData> recordedNotesList = new List<RecordedNoteData>();

    // 打ち込み時のリアルタイム管理用（配列でレーンごとに1つだけ保持）
    private NotesCon[] activeRecordingNotes = new NotesCon[2];
    private float[] holdStartTimes = new float[2];
    private float[] rushTimers = new float[2];

    private bool isPreviewPlaying = false;
    private int playbackIndex = 0;
    private float lastCheckedTime = 0f;

    private void Update()
    {
        if (AudioManager.Instance == null) { return; }
        if (Keyboard.current == null) { return; }

        float currentTime = AudioManager.Instance.GetCurrentTime();

        HandlePlaybackCon();
        HandleSpeedControls();
        HandleTimeCon();

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            isPreviewPlaying = !isPreviewPlaying;
            if (isPreviewPlaying)
            {
                ResetPlaybackIndexToTime(currentTime);
                Debug.Log("<color=green>[プレビューモード: ON]</color> 記録されたノーツの再生を開始します。");
            }
            else
            {
                Debug.Log("<color=red>[プレビューモード: OFF]</color> 再生を停止しました。");
            }
        }

        if (isPreviewPlaying && AudioManager.Instance.IsBGMPlaying())
        {
            UpdateChartPlayback(currentTime);
        }

        if (Keyboard.current.sKey.wasPressedThisFrame &&
            (Keyboard.current.ctrlKey.isPressed || Keyboard.current.altKey.isPressed))
        {
            ExportToCSV();
        }

        if (notsSpawn != null)
        {
            HandleRecording();
        }

        lastCheckedTime = currentTime;
    }

    private void HandlePlaybackCon()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (AudioManager.Instance.IsBGMPlaying())
            {
                AudioManager.Instance.BGMPause();
                ForceReleaseAllHolds();
            }
            else
            {
                AudioManager.Instance.PlayActiveMusic();
            }
        }
    }

    private void HandleTimeCon()
    {
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            AudioManager.Instance.SkipTime(skipSeconds);
            float newTime = AudioManager.Instance.GetCurrentTime();
            if (notsSpawn != null) notsSpawn.ResetSpawnIndexToTime(newTime);
            ResetPlaybackIndexToTime(newTime);
            ForceReleaseAllHolds();
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            AudioManager.Instance.SkipTime(-skipSeconds);
            float newTime = AudioManager.Instance.GetCurrentTime();
            if (notsSpawn != null) notsSpawn.ResetSpawnIndexToTime(newTime);
            ResetPlaybackIndexToTime(newTime);
            ForceReleaseAllHolds();
        }
    }

    private void HandleSpeedControls()
    {
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            AudioManager.Instance.SetPitch(Mathf.Min(AudioManager.Instance.GetCurrentPitch() + 0.25f, 3.0f));
        }
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            AudioManager.Instance.SetPitch(Mathf.Max(AudioManager.Instance.GetCurrentPitch() - 0.25f, 0.25f));
        }
    }

    private void HandleRecording()
    {
        float currentTime = AudioManager.Instance.GetCurrentTime();

        // Gキー：同時押し（Both）
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            RecordBothNote(currentTime);
        }

        // F/Jキー：単押し・ロングノーツ
        ProcessKeyRec(0, Keyboard.current.fKey);
        ProcessKeyRec(1, Keyboard.current.jKey);

        // D/Kキー：ラッシュノーツ（連打）
        ProcessRushKeyRec(0, Keyboard.current.dKey);
        ProcessRushKeyRec(1, Keyboard.current.kKey);
    }

    /// <summary>
    /// 同時押し（Both）：計算により100%完全な中心位置へ生成・移動させる
    /// </summary>
    private void RecordBothNote(float time)
    {
        RecordedNoteData note = new RecordedNoteData(time, 0, 4);
        recordedNotesList.Add(note);
        recordedNotesList.Sort((a, b) => a.time.CompareTo(b.time));

        // インスペクターのズレを吸収するため、スクリプト側で左右レーンの完全な中心点を常に動的計算
        Vector3 spawnPos = (notsSpawn.spawnPoints[0].position + notsSpawn.spawnPoints[1].position) * 0.5f;
        Vector3 targetPos = (notsSpawn.leftTargetCircle.position + notsSpawn.rightTargetCircle.position) * 0.5f;

        if (bothSpawnPoint != null) spawnPos = bothSpawnPoint.position;
        if (bothTargetCircle != null) targetPos = bothTargetCircle.position;

        SpawnSinglePreview(bothNotePrefab != null ? bothNotePrefab : shortNotePrefab, 0, time, NoteDate.NotesType.Both, spawnPos, targetPos, 0f);
    }

    /// <summary>
    /// 押している間だけ通常ロングノーツを滑らかに伸ばす
    /// </summary>
    private void ProcessKeyRec(int lane, KeyControl key)
    {
        float currentTime = AudioManager.Instance.GetCurrentTime();
        Transform spawnPoint = notsSpawn.spawnPoints[lane];
        Transform targetCircle = (lane == 0) ? notsSpawn.leftTargetCircle : notsSpawn.rightTargetCircle;

        // 1. 押した瞬間：通常のロングノーツを生成（※SetHoldVisualは絶対に呼ばない）
        if (key.wasPressedThisFrame)
        {
            holdStartTimes[lane] = currentTime;

            if (longNotePrefab != null && activeRecordingNotes[lane] == null)
            {
                GameObject previewObj = Instantiate(longNotePrefab, spawnPoint.position, Quaternion.identity);
                NotesCon notesCon = previewObj.GetComponent<NotesCon>();
                if (notesCon != null)
                {
                    NoteDate.Notes tempNote = new NoteDate.Notes 
                    { 
                        lane = lane, 
                        targetTime = currentTime + notsSpawn.preSpawnTime, 
                        noteType = NoteDate.NotesType.Long_Start 
                    };
                    notesCon.Init(tempNote, notsSpawn.preSpawnTime, targetCircle.position);
                    
                    // 黄色の不具合ラインは絶対に呼ばない
                    // 初期状態の終了時間は、開始時間と同一（長さ0）
                    notesCon.SetEndTime(tempNote.targetTime);
                    
                    activeRecordingNotes[lane] = notesCon;
                }
            }
        }

        // 2. 押している間：リアルタイムに入力時間分だけノーツのEnd時間を伸ばす
        if (key.isPressed && activeRecordingNotes[lane] != null)
        {
            float currentDuration = currentTime - holdStartTimes[lane];
            float initialTargetTime = holdStartTimes[lane] + notsSpawn.preSpawnTime;
            
            // 通常ロングノーツのメッシュ・レール仕様に基づき、お尻を後ろへ伸ばし続ける
            activeRecordingNotes[lane].SetEndTime(initialTargetTime + currentDuration);
        }

        // 3. 離された瞬間：確定処理
        if (key.wasReleasedThisFrame)
        {
            float duration = currentTime - holdStartTimes[lane];

            if (duration < 0.15f)
            {
                // 押し時間が短い場合は単押し。生成していたロングは即座に破棄
                if (activeRecordingNotes[lane] != null)
                {
                    Destroy(activeRecordingNotes[lane].gameObject);
                    activeRecordingNotes[lane] = null;
                }
                
                RecordSingleNote(lane, holdStartTimes[lane]);
                SpawnSinglePreview(shortNotePrefab, lane, holdStartTimes[lane], NoteDate.NotesType.Short, spawnPoint.position, targetCircle.position, 0f);
            }
            else
            {
                // ロングノーツ確定
                RecordLongNote(lane, holdStartTimes[lane], currentTime);
                
                // リアルタイムで綺麗に伸びていたオブジェクトをそのまま確定ノーツとして残す
                if (activeRecordingNotes[lane] != null)
                {
                    // 譜面の生存時間（寿命）を設定して管理から切り離す
                    Destroy(activeRecordingNotes[lane].gameObject, notsSpawn.preSpawnTime + duration + 0.5f);
                    activeRecordingNotes[lane] = null;
                }
            }
        }
    }

    private void ProcessRushKeyRec(int lane, KeyControl key)
    {
        float currentTime = AudioManager.Instance.GetCurrentTime();

        if (key.wasPressedThisFrame)
        {
            SpawnAndRecordRushNote(lane, currentTime);
            rushTimers[lane] = currentTime + rushInterval;
        }
        else if (key.isPressed)
        {
            if (currentTime >= rushTimers[lane])
            {
                SpawnAndRecordRushNote(lane, currentTime);
                rushTimers[lane] = currentTime + rushInterval;
            }
        }
    }

    /// <summary>
    /// ラッシュノーツ（連打）：計算により100%完全な中心位置へ生成・移動させる
    /// </summary>
    private void SpawnAndRecordRushNote(int lane, float time)
    {
        RecordedNoteData note = new RecordedNoteData(time, lane, 3);
        recordedNotesList.Add(note);
        recordedNotesList.Sort((a, b) => a.time.CompareTo(b.time));

        // 左右の完全な中間座標を算出して配置
        Vector3 spawnPos = (notsSpawn.spawnPoints[0].position + notsSpawn.spawnPoints[1].position) * 0.5f;
        Vector3 targetPos = (notsSpawn.leftTargetCircle.position + notsSpawn.rightTargetCircle.position) * 0.5f;

        if (bothSpawnPoint != null) spawnPos = bothSpawnPoint.position;
        if (bothTargetCircle != null) targetPos = bothTargetCircle.position;

        SpawnSinglePreview(rushNotePrefab, lane, time, NoteDate.NotesType.Rush, spawnPos, targetPos, 0f);
    }

    private void SpawnSinglePreview(GameObject prefab, int lane, float recordTime, NoteDate.NotesType noteType, Vector3 spawnPos, Vector3 targetPos, float longDuration)
    {
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        NotesCon con = obj.GetComponent<NotesCon>();
        if (con != null)
        {
            NoteDate.Notes tempNote = new NoteDate.Notes
            {
                lane = lane,
                targetTime = recordTime + notsSpawn.preSpawnTime,
                noteType = noteType
            };
            con.Init(tempNote, notsSpawn.preSpawnTime, targetPos);

            if (noteType == NoteDate.NotesType.Long_Start)
            {
                con.SetEndTime(tempNote.targetTime + longDuration);
            }
        }
        Destroy(obj, notsSpawn.preSpawnTime + longDuration + 0.5f);
    }

    /// <summary>
    /// プレビュー自動再生：Both/Rushの中央軌道を完全に維持して流す
    /// </summary>
    private void UpdateChartPlayback(float currentTime)
    {
        if (recordedNotesList.Count == 0 || playbackIndex >= recordedNotesList.Count) return;

        if (currentTime < lastCheckedTime)
        {
            ResetPlaybackIndexToTime(currentTime);
        }

        while (playbackIndex < recordedNotesList.Count && currentTime >= recordedNotesList[playbackIndex].time)
        {
            var note = recordedNotesList[playbackIndex];

            Vector3 spawnPos;
            Vector3 targetPos;
            GameObject prefab;
            NoteDate.NotesType noteType;

            if (note.type == 4 || note.type == 3)
            {
                // 再生時も強制的に完全な中央座標を計算して流す
                spawnPos = (notsSpawn.spawnPoints[0].position + notsSpawn.spawnPoints[1].position) * 0.5f;
                targetPos = (notsSpawn.leftTargetCircle.position + notsSpawn.rightTargetCircle.position) * 0.5f;
                
                if (bothSpawnPoint != null) spawnPos = bothSpawnPoint.position;
                if (bothTargetCircle != null) targetPos = bothTargetCircle.position;

                prefab = (note.type == 4) ? (bothNotePrefab != null ? bothNotePrefab : shortNotePrefab) : rushNotePrefab;
                noteType = (note.type == 4) ? NoteDate.NotesType.Both : NoteDate.NotesType.Rush;
            }
            else
            {
                spawnPos = notsSpawn.spawnPoints[note.lane].position;
                targetPos = (note.lane == 0) ? notsSpawn.leftTargetCircle.position : notsSpawn.rightTargetCircle.position;
                
                if (note.type == 2) { prefab = longNotePrefab; noteType = NoteDate.NotesType.Long_Start; }
                else { prefab = shortNotePrefab; noteType = NoteDate.NotesType.Short; }
            }

            float duration = 0f;
            if (noteType == NoteDate.NotesType.Long_Start && (playbackIndex + 1) < recordedNotesList.Count)
            {
                // 次のデータ（LongEnd）との差分から長さを算出してプレビュー表示
                var nextNote = recordedNotesList[playbackIndex + 1];
                if (nextNote.type == 2 && nextNote.lane == note.lane)
                {
                    duration = nextNote.time - note.time;
                    playbackIndex++; // Endノーツ分も消化
                }
            }
            
            SpawnSinglePreview(prefab, note.lane, note.time, noteType, spawnPos, targetPos, duration);
            playbackIndex++;
        }
    }

    private void ResetPlaybackIndexToTime(float time)
    {
        playbackIndex = 0;
        while (playbackIndex < recordedNotesList.Count && recordedNotesList[playbackIndex].time < time)
        {
            playbackIndex++;
        }
    }

    private void RecordSingleNote(int lane, float time)
    {
        RecordedNoteData note = new RecordedNoteData(time, lane, 1);
        recordedNotesList.Add(note);
        recordedNotesList.Sort((a, b) => a.time.CompareTo(b.time));
    }

    private void RecordLongNote(int lane, float startTime, float endTime)
    {
        RecordedNoteData startNote = new RecordedNoteData(startTime, lane, 2);
        RecordedNoteData endNote = new RecordedNoteData(endTime, lane, 2);
        recordedNotesList.Add(startNote);
        recordedNotesList.Add(endNote);
        recordedNotesList.Sort((a, b) => a.time.CompareTo(b.time));
    }

    private void ForceReleaseAllHolds()
    {
        for (int i = 0; i < activeRecordingNotes.Length; i++)
        {
            if (activeRecordingNotes[i] != null)
            {
                Destroy(activeRecordingNotes[i].gameObject);
                activeRecordingNotes[i] = null;
            }
        }
    }

    public void ExportToCSV()
    {
        if (recordedNotesList.Count == 0) return;

        recordedNotesList.Sort((a, b) => a.time.CompareTo(b.time));
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Index,Time,Lane,Type,Note");

        bool[] isLaneLongActive = new bool[2];

        for (int i = 0; i < recordedNotesList.Count; i++)
        {
            var note = recordedNotesList[i];
            int index = i + 1;
            string noteText = "";

            if (note.type == 1) noteText = "";
            else if (note.type == 2)
            {
                int lane = note.lane;
                if (!isLaneLongActive[lane])
                {
                    noteText = "LongStart";
                    isLaneLongActive[lane] = true;
                }
                else
                {
                    noteText = "LongEnd";
                    isLaneLongActive[lane] = false;
                }
            }
            else if (note.type == 3) noteText = "Rush";
            else if (note.type == 4) noteText = "Both";

            sb.AppendLine($"{index},{note.time:F3},{note.lane},{note.type},{noteText}");
        }

        string savePath = "";
        AudioDataSO activeData = AudioManager.Instance.GetActiveMusicData();

#if UNITY_EDITOR
        if (activeData != null && activeData.csvChartFile != null)
        {
            savePath = AssetDatabase.GetAssetPath(activeData.csvChartFile);
        }
        else
        {
            string defaultName = activeData != null ? $"{activeData.musicTitle}_chart" : "new_chart";
            savePath = EditorUtility.SaveFilePanelInProject("譜面CSVファイルを保存", defaultName, "csv", "保存先ファイル名を入力してください");
        }
#else
        savePath = Path.Combine(Application.persistentDataPath, "RecordedChart.csv");
#endif

        if (string.IsNullOrEmpty(savePath)) return;

        try
        {
            File.WriteAllText(savePath, sb.ToString());
            Debug.Log($"<color=green>【譜面保存成功】CSVファイルを保存しました！ パス: {savePath}</color>");
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
        catch (Exception e) { Debug.LogError($"CSVエラー: {e.Message}"); }
    }

    // 譜面作成時に使用するキーや機能等をGUIとして実際の画面にて表示
    private void OnGUI()
    {
        if (AudioManager.Instance == null) return;
        AudioDataSO activeData = AudioManager.Instance.GetActiveMusicData();
        
        GUI.Box(new Rect(10, 10, 360, 290), "🎵 譜面作成エディタコントロール");
        
        if (activeData != null)
        {
            GUI.Label(new Rect(20, 35, 340, 20), $"楽曲: {activeData.musicTitle} (BPM: {activeData.bpm})");
        }
        float currentTime = AudioManager.Instance.GetCurrentTime();
        GUI.Label(new Rect(20, 55, 340, 20), $"時間: {currentTime:F3}s");
        GUI.Label(new Rect(20, 75, 340, 20), $"記録済みノーツ数: {recordedNotesList.Count}");
        string playState = isPreviewPlaying ? "<color=green>ON (再生中)</color>" : "<color=red>OFF (停止中)</color>";
        GUI.Label(new Rect(20, 95, 340, 20), $"Pキー プレビュー再生: {playState}");
        
        GUI.Label(new Rect(20, 120, 340, 20), "【操作方法】");
        GUI.Label(new Rect(20, 140, 340, 20), "Space: 全体再生 / 一時停止");
        GUI.Label(new Rect(20, 160, 340, 20), "P: 記録したノーツのプレビューON / OFF");
        GUI.Label(new Rect(20, 180, 340, 20), "F / J: 左 / 右 ノーツ（長押しでロング）");
        GUI.Label(new Rect(20, 200, 340, 20), "G: 同時押し（Both）中央ライン");
        GUI.Label(new Rect(20, 220, 340, 20), "D / K: ラッシュノーツ（中央ラインに生成）");
        GUI.Label(new Rect(20, 240, 340, 20), "← / →: タイムスキップ");
        GUI.Label(new Rect(20, 260, 340, 20), "Ctrl + S: CSVへ譜面出力");
    }
}

/// <summary>
/// エディタ上で一時的にノーツ情報を保持するためのシリアライズ用クラス
/// </summary>
[System.Serializable]
public class RecordedNoteData
{
    public float time;
    public int lane;
    public int type;

    public RecordedNoteData(float time, int lane, int type)
    {
        this.time = time;
        this.lane = lane;
        this.type = type;
    }
}