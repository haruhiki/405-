using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ChretCreateManager : MonoBehaviour
{

    [Header("タイム操作設定")]
    [SerializeField] private float skipSeconds = 5.0f; //一回のスキップ進む/戻る時間

    [Header("プレビューノーツ生成設定")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform[] laneTargets;  //判定サークルのtransform

    //メモリ上に一時敵に記録しておくノーツのデータリスト
    private List<RecordedNoteData> recordedNotesList = new List<RecordedNoteData>();

    //現在レコーディング長押し中のノーツ情報
    private NotesCon[] activeRecordingNotes = new NotesCon[2];
    private float[] holdStartTimes = new float[2];


    private void Update()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.GetActiveMusicData() == null) { return; }
        if (Keyboard.current == null) { return; }

        HandklePlaybackCon();
        HandleSpeedControls();
        HandleTimeCon();

        if(Keyboard.current.sKey.wasPressedThisFrame &&
            (Keyboard.current.ctrlKey.isPressed || Keyboard.current.altKey.isPressed))
        {
            ExportToCSV();
        }



        if (AudioManager.Instance.IsBGMPlaying())
        {
            HandleRecorrding();
        }

    }

    /// <summary> /// 再生 / 一時停止コントロール /// </summary>
    private void HandklePlaybackCon()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (AudioManager.Instance.IsBGMPlaying())
            {
                AudioManager.Instance.BGMPause();
                Debug.Log($"[一時停止] 時間停止:{AudioManager.Instance.GetCurrentTime():F3} 秒");

                //再生を止めたら長押し中のロングノーツがあれば強制終了して確定させる
                ForceReleaseAllHolds();
            }
            else
            {
                AudioManager.Instance.PlayActiveMusic();
                Debug.Log($"[再生開始] 現在の時間: {AudioManager.Instance.GetCurrentTime():F3}秒");
            }
        }
    }

    /// <summary> /// 巻き戻し/早送りコントロール /// </summary>
    private void HandleTimeCon()
    {
        //右矢印早送り
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            AudioManager.Instance.SkipTime(skipSeconds);
            Debug.Log($"[早送り] {AudioManager.Instance.GetCurrentTime(): F3}秒");
            ForceReleaseAllHolds();

        }

        //左矢印巻き戻し
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            AudioManager.Instance.SkipTime(skipSeconds);
            Debug.Log($"[巻き戻し] {AudioManager.Instance.GetCurrentTime():F3} 秒");
            ForceReleaseAllHolds();
        }
    }
    /// <summary>
    /// 再生速度コントロール
    /// </summary>

    private void HandleSpeedControls()
    {
        // 上矢印キーで速度アップ (最大3倍速)
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            float nextPitch = Mathf.Min(AudioManager.Instance.GetCurrentPitch() + 0.25f, 3.0f);
            AudioManager.Instance.SetPitch(nextPitch);
            Debug.Log($"【再生速度アップ】倍速: {AudioManager.Instance.GetCurrentPitch():F2}x");
        }

        // 下矢印キーで速度ダウン (最低0.25倍速)
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            float nextPitch = Mathf.Max(AudioManager.Instance.GetCurrentPitch() - 0.25f, 0.25f);
            AudioManager.Instance.SetPitch(nextPitch);
            Debug.Log($"【再生速度ダウン】倍速: {AudioManager.Instance.GetCurrentPitch():F2}x");
        }
    }

    /// <summary> /// リアルタイムのノーツレコーディング処理 /// </summary>
    private void HandleRecorrding()
    {
        //左レーン
        ProcessKeyRec(0, Keyboard.current.fKey);
       //右レーン
        ProcessKeyRec(1,Keyboard.current.jKey);
    }

    private void ProcessKeyRec(int lane, KeyControl key)
    {
        float currentTime = AudioManager.Instance.GetCurrentTime();

        if (key.wasPressedThisFrame)
        {
            Debug.Log($"<color=line> [レコーディング開始] レーン: {lane} 時間：{currentTime:F3}秒</color>");

            holdStartTimes[lane] = currentTime;

            if (notePrefab != null && laneTargets[lane] != null)
            {
                GameObject previewObj = Instantiate(notePrefab, laneTargets[lane].position, Quaternion.identity);
                NotesCon notesCon = previewObj.GetComponent<NotesCon>();

                if (notesCon != null)
                {
                    NoteDate.Notes tempNote = new NoteDate.Notes();
                    tempNote.lane = lane;
                    tempNote.targetTime = currentTime;
                    tempNote.noteType = NoteDate.NotesType.Long_Start;

                    notesCon.Init(tempNote, 0.001f, laneTargets[lane].position);
                    notesCon.SetHoldVisual(true);

                    activeRecordingNotes[lane] = notesCon;
                }
            }
        }

        if (key.isPressed && activeRecordingNotes[lane] != null) 
        {
            //時間経過に併せて帯の長さを伸ばす
            activeRecordingNotes[lane].SetEndTime(currentTime);
        }

        if (key.wasReleasedThisFrame) 
        {
            float duration = currentTime - holdStartTimes[lane];

            if(duration < 0.15f) 
            {
                //シングルノーツ記録

                //単押しなので、引き伸ばしていたプレビューpぬじぇくと不要
                if (activeRecordingNotes[lane] != null) 
                {
                    Destroy(activeRecordingNotes[lane].gameObject);
                }

            }
            else 
            {
                //一定時間押してるやつ（ロングノーツ記録)

                if (activeRecordingNotes[lane] != null) 
                {
                    activeRecordingNotes[lane].SetHoldVisual(false);
                    Destroy(activeRecordingNotes[lane].gameObject, 1.0f);
                }
            }
            activeRecordingNotes[lane] = null;
        }
    }

    //通常ノーツ
    private void RecordSingleNote(int lane, float time) 
    {
        RecordedNoteData note = new RecordedNoteData(time, lane, 1);
        recordedNotesList.Add(note);
        Debug.Log($"[単押し登録] レーン: {lane} | 時間: {time:F3}秒");
    }

    private void RecordedLngNote(int lane,float startTime,float endTime) 
    {
        RecordedNoteData startNote = new RecordedNoteData(startTime, lane, 2);
        RecordedNoteData endNote = new RecordedNoteData(endTime, lane, 2);

        recordedNotesList.Add(startNote);
        recordedNotesList.Add(endNote);

        Debug.Log($"<color=cyan>[長押し登録] レーン: {lane} | 開始: {startTime:F3}秒 〜 終了: {endTime:F3}秒</color>");
    }

    private void ForceReleaseAllHolds() 
    {
        for(int i = 0; i < activeRecordingNotes.Length; i++) 
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
        if(recordedNotesList.Count == 0) 
        {
            Debug.Log("書き出すノーツが空");
            return;
        }

        //時間の昇順にデータをソート
        recordedNotesList.Sort((a, b) => a.time.CompareTo(b.time));

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Index,Time,Lane,Type,Note");

        bool[] isLaneLongActive = new bool[2];

        for(int i = 0;i < recordedNotesList.Count; i++) 
        {
            var note = recordedNotesList[i];
            int index = i + 1;
            string noteText = "";

            if(note.type == 1) 
            {
                noteText = "";
            }
            else if(note.type == 2) 
            {
                int lane = note.lane;
                if (!isLaneLongActive[lane]) 
                {
                    noteText = "LngStart";
                    isLaneLongActive[lane] = true;
                }
                else 
                {
                    noteText = "LongEnd";
                    isLaneLongActive[lane] = false;
                }
            }

            //CSV文字列を一行追加
            sb.AppendLine($"{index},{note.time:F3},{note.lane},{note.type},{noteText}");

        }

        //保存パス
        string savePath = "";
        AudioDataSO activeData = AudioManager.Instance.GetActiveMusicData();
#if UNITY_EDITOR
        // AudioDataSOにすでにCSVファイルが設定されていれば、そのパスに自動上書き保存する
        if (activeData != null && activeData.csvChartFile != null)
        {
            savePath = AssetDatabase.GetAssetPath(activeData.csvChartFile);
        }
        else
        {
            // 設定されていない場合はファイル保存ダイアログを表示する
            string defaultName = activeData != null ? $"{activeData.musicTitle}_chart" : "new_chart";
            savePath = EditorUtility.SaveFilePanelInProject("譜面CSVファイルを保存", defaultName, "csv", "保存先ファイル名を入力してください");
        }
#else
        // ビルド後（実機）の場合は persistentDataPath に保存
        savePath = Path.Combine(Application.persistentDataPath, "RecordedChart.csv");
#endif

        if (string.IsNullOrEmpty(savePath)) 
        {
            Debug.LogWarning("CSV保存がキャンセルされました。");
            return;
        }

        try
        {
            // ファイル書き出し
            File.WriteAllText(savePath, sb.ToString());
            Debug.Log($"<color=green>【譜面保存成功】CSVファイルを保存しました！ パス: {savePath}</color>");

#if UNITY_EDITOR
            // Unityエディタにファイルを再読み込みさせて即座に反映
            AssetDatabase.Refresh();

            // 保存したCSVファイルを自動的にScriptableObjectに再紐付けする親切機能
            if (activeData != null && activeData.csvChartFile == null)
            {
                TextAsset newCsvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(savePath);
                if (newCsvAsset != null)
                {
                    activeData.csvChartFile = newCsvAsset;
                    EditorUtility.SetDirty(activeData);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"自動紐付け: {activeData.name} に CSVアセットをアタッチしました。");
                }
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV書き出し中にエラーが発生しました: {e.Message}");
        }
}

    /// <summary>
    /// エディタ上で一時的に記録状態を確認するためのデバッグコマンド
    /// </summary>
    [ContextMenu("Show Recorded Notes")]
    public void ShowRecordedNotes()
    {
        Debug.Log($"=== 現在記録されているノーツ数: {recordedNotesList.Count} ===");
        recordedNotesList.Sort((a, b) => a.time.CompareTo(b.time));
        foreach (var note in recordedNotesList)
        {
            Debug.Log($"時間: {note.time:F3} | レーン: {note.lane} | タイプ: {note.type}");
        }
    }

    /// <summary>
    /// 画面上に操作方法やステータスを表示する簡易GUIオーバーレイ
    /// </summary>
    private void OnGUI()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.GetActiveMusicData() == null) return;

        AudioDataSO activeData = AudioManager.Instance.GetActiveMusicData();

        // UIボックスの背景
        GUI.Box(new Rect(10, 10, 360, 240), "🎵 譜面作成エディタコントロール");

        // 情報表示
        GUI.Label(new Rect(20, 35, 340, 20), $"楽曲: {activeData.musicTitle} (BPM: {activeData.bpm})");

        float currentTime = AudioManager.Instance.GetCurrentTime();
        float totalTime = AudioManager.Instance.GetTotalDuration();
        GUI.Label(new Rect(20, 55, 340, 20), $"時間: {currentTime:F3}s / {totalTime:F3}s (倍速: {AudioManager.Instance.GetCurrentPitch():F2}x)");
        GUI.Label(new Rect(20, 75, 340, 20), $"記録済みノーツ数: {recordedNotesList.Count}");

        // 操作ガイド
        GUI.Label(new Rect(20, 105, 340, 20), "【操作キー一覧】");
        GUI.Label(new Rect(20, 125, 340, 20), "• [Space] : 楽曲の再生 / 一時停止");
        GUI.Label(new Rect(20, 145, 340, 20), "• [←] / [→] : 5秒 巻き戻し / 早送り");
        GUI.Label(new Rect(20, 165, 340, 20), "• [↑] / [↓] : 再生速度を上げる / 下げる");
        GUI.Label(new Rect(20, 185, 340, 20), "• [F] / [J] : 左レーン / 右レーン リアルタイム記録");

        // CSV出力ボタン
        if (GUI.Button(new Rect(20, 210, 340, 30), "💾 CSVファイルへ書き出し (Ctrl + S)"))
        {
            ExportToCSV();
        }
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

    public RecordedNoteData(float time,int lane,int type) 
    {
        this.time = time;
        this.lane = lane;
        this.type = type;
    }
}
