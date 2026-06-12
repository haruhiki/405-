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

    [Header("シーン上のオブジェクト参照")]
    [SerializeField] private NotsSpawn notsSpawn;

    [Header("ラッシュ自動連打設定")]
    [Tooltip("D/Kキー長押し時に、ラッシュノーツを自動生成して記録する間隔（秒数）")]
    [SerializeField] private float rushInterval = 0.1f;

    private List<RecordedNoteData> recordedNotesList = new List<RecordedNoteData>();

    // 現在生成されて、判定円に向かって降下中のアクティブなロングノーツ（ホールド中）
    private NotesCon[] activeRecordingNotes = new NotesCon[2];
    private float[] holdStartTimes = new float[2];
    private float[] rushTimers = new float[2];

    private void Update()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.GetActiveMusicData() == null) { return; }
        if (Keyboard.current == null) { return; }

        HandklePlaybackCon();
        HandleSpeedControls();
        HandleTimeCon();

        if (Keyboard.current.sKey.wasPressedThisFrame &&
            (Keyboard.current.ctrlKey.isPressed || Keyboard.current.altKey.isPressed))
        {
            ExportToCSV();
        }

        if (notsSpawn != null)
        {
            HandleRecorrding();
        }
    }

    private void HandklePlaybackCon()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (AudioManager.Instance.IsBGMPlaying())
            {
                AudioManager.Instance.BGMPause();
                Debug.Log($"[一時停止] 時間停止:{AudioManager.Instance.GetCurrentTime():F3} 秒");
                ForceReleaseAllHolds();
            }
            else
            {
                AudioManager.Instance.PlayActiveMusic();
                Debug.Log($"[再生開始] 現在の時間: {AudioManager.Instance.GetCurrentTime():F3}秒");
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
            ForceReleaseAllHolds();
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            AudioManager.Instance.SkipTime(-skipSeconds);
            float newTime = AudioManager.Instance.GetCurrentTime();
            if (notsSpawn != null) notsSpawn.ResetSpawnIndexToTime(newTime);
            ForceReleaseAllHolds();
        }
    }

    private void HandleSpeedControls()
    {
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            float nextPitch = Mathf.Min(AudioManager.Instance.GetCurrentPitch() + 0.25f, 3.0f);
            AudioManager.Instance.SetPitch(nextPitch);
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            float nextPitch = Mathf.Max(AudioManager.Instance.GetCurrentPitch() - 0.25f, 0.25f);
            AudioManager.Instance.SetPitch(nextPitch);
        }
    }

    private void HandleRecorrding()
    {
        ProcessKeyRec(0, Keyboard.current.fKey);
        ProcessKeyRec(1, Keyboard.current.jKey);

        ProcessRushKeyRec(0, Keyboard.current.dKey);
        ProcessRushKeyRec(1, Keyboard.current.kKey);
    }

    /// <summary>
    /// F/Jキー: 叩いたその瞬間、「上（湧き点）」に生成され、本編のNotesConの動きで「下（サークル）」へ下がっていく！
    /// </summary>
    private void ProcessKeyRec(int lane, KeyControl key)
    {
        float currentTime = AudioManager.Instance.GetCurrentTime();
        Transform spawnPoint = notsSpawn.spawnPoints[lane];
        Transform targetCircle = (lane == 0) ? notsSpawn.leftTargetCircle : notsSpawn.rightTargetCircle;

        // 1. キーが押された瞬間（ノーツを上に生成して、本編と同じ挙動で降下開始）
        if (key.wasPressedThisFrame)
        {
            // CSVに記録するための、リアルタイムの「本当の打鍵時間」
            holdStartTimes[lane] = currentTime;

            if (longNotePrefab != null)
            {
                // お前の言う通り、本来の「上部生成ポイント（spawnPoint）」にダイレクト生成！
                GameObject previewObj = Instantiate(longNotePrefab, spawnPoint.position, Quaternion.identity);
                NotesCon notesCon = previewObj.GetComponent<NotesCon>();

                if (notesCon != null)
                {
                    NoteDate.Notes tempNote = new NoteDate.Notes();
                    tempNote.lane = lane;

                    // 【本編と同じ挙動をさせるためのターゲットタイム設定】
                    // 今（currentTime）上から降らせて、preSpawnTime秒後に判定円にジャストで届くように設定する
                    tempNote.targetTime = currentTime + notsSpawn.preSpawnTime;
                    tempNote.noteType = NoteDate.NotesType.Long_Start;

                    // ★NotesConの自動移動Update（本編の動き）をそのまま生かす（enabled = true）
                    notesCon.Init(tempNote, notsSpawn.preSpawnTime, targetCircle.position);
                    notesCon.SetHoldVisual(true);

                    activeRecordingNotes[lane] = notesCon;
                }
            }
        }

        // 2. 押しっぱなし中：本編のロングノーツと同じように、お尻（末端）を伸ばしながら下に下がっていく
        if (key.isPressed && activeRecordingNotes[lane] != null)
        {
            // 押している長さに応じて、ロングの末端時間を未来に更新し続ける
            float elapsed = currentTime - holdStartTimes[lane];
            activeRecordingNotes[lane].SetEndTime(holdStartTimes[lane] + notsSpawn.preSpawnTime + elapsed);
        }

        // 3. 離された瞬間
        if (key.wasReleasedThisFrame)
        {
            float duration = currentTime - holdStartTimes[lane];

            if (duration < 0.15f)
            {
                // 【単押し確定】CSVには実際の打鍵時間を記録
                RecordSingleNote(lane, holdStartTimes[lane]);

                // ロング用プレビューは即座に破棄
                if (activeRecordingNotes[lane] != null)
                {
                    Destroy(activeRecordingNotes[lane].gameObject);
                }

                // 単押しノーツを「上」から本編と同じ挙動で降らせる！
                if (shortNotePrefab != null)
                {
                    GameObject shortObj = Instantiate(shortNotePrefab, spawnPoint.position, Quaternion.identity);
                    NotesCon shortCon = shortObj.GetComponent<NotesCon>();
                    if (shortCon != null)
                    {
                        NoteDate.Notes tempNote = new NoteDate.Notes();
                        tempNote.lane = lane;
                        tempNote.targetTime = currentTime + notsSpawn.preSpawnTime; // preSpawnTime秒後に判定円へ
                        tempNote.noteType = NoteDate.NotesType.Short;

                        shortCon.Init(tempNote, notsSpawn.preSpawnTime, targetCircle.position);
                    }
                    // 判定円を通り過ぎて画面外に消える頃に自動破棄
                    Destroy(shortObj, notsSpawn.preSpawnTime + 0.5f);
                }
            }
            else
            {
                // 【ロングノーツ確定】CSVに記録
                RecordLongNote(lane, holdStartTimes[lane], currentTime);

                if (activeRecordingNotes[lane] != null)
                {
                    activeRecordingNotes[lane].SetHoldVisual(false);
                    // そのまま判定円を通り過ぎて流れきるまで生かしてから自動破棄
                    Destroy(activeRecordingNotes[lane].gameObject, notsSpawn.preSpawnTime + 1.0f);
                }
            }
            activeRecordingNotes[lane] = null;
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
    /// 連打（Rush）ノーツも、叩いた瞬間に「上」に生成され、本編と同じ挙動で下がっていく！
    /// </summary>
    private void SpawnAndRecordRushNote(int lane, float time)
    {
        RecordedNoteData note = new RecordedNoteData(time, lane, 3);
        recordedNotesList.Add(note);

        if (rushNotePrefab != null)
        {
            Transform spawnPoint = notsSpawn.spawnPoints[lane];
            Transform targetCircle = (lane == 0) ? notsSpawn.leftTargetCircle : notsSpawn.rightTargetCircle;

            // 上の生成ポイントに生成
            GameObject rushObj = Instantiate(rushNotePrefab, spawnPoint.position, Quaternion.identity);
            NotesCon rushCon = rushObj.GetComponent<NotesCon>();
            if (rushCon != null)
            {
                NoteDate.Notes tempNote = new NoteDate.Notes();
                tempNote.lane = lane;
                tempNote.targetTime = time + notsSpawn.preSpawnTime; // preSpawnTime秒後に判定円へ
                tempNote.noteType = NoteDate.NotesType.Short;

                // 本編の動きのまま降らせる
                rushCon.Init(tempNote, notsSpawn.preSpawnTime, targetCircle.position);
            }
            // 判定円を通過した後に自動破棄
            Destroy(rushObj, notsSpawn.preSpawnTime + 0.5f);
        }
    }

    private void RecordSingleNote(int lane, float time)
    {
        RecordedNoteData note = new RecordedNoteData(time, lane, 1);
        recordedNotesList.Add(note);
        Debug.Log($"[単押し登録] レーン: {lane} | 時間: {time:F3}秒");
    }

    private void RecordLongNote(int lane, float startTime, float endTime)
    {
        RecordedNoteData startNote = new RecordedNoteData(startTime, lane, 2);
        RecordedNoteData endNote = new RecordedNoteData(endTime, lane, 2);
        recordedNotesList.Add(startNote);
        recordedNotesList.Add(endNote);
        Debug.Log($"<color=cyan>[長押し登録] レーン: {lane} | 開始: {startTime:F3}秒 〜 終了: {endTime:F3}秒</color>");
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

    private void OnGUI()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.GetActiveMusicData() == null) return;
        AudioDataSO activeData = AudioManager.Instance.GetActiveMusicData();
        GUI.Box(new Rect(10, 10, 360, 270), "🎵 譜面作成エディタコントロール");
        GUI.Label(new Rect(20, 35, 340, 20), $"楽曲: {activeData.musicTitle} (BPM: {activeData.bpm})");
        float currentTime = AudioManager.Instance.GetCurrentTime();
        GUI.Label(new Rect(20, 55, 340, 20), $"時間: {currentTime:F3}s");
        GUI.Label(new Rect(20, 75, 340, 20), $"記録済みノーツ数: {recordedNotesList.Count}");
    }
}

/// <summary>
/// エディタ上で一次的にノーツ情報を保持するためのシリアライズ用クラス
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