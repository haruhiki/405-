using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [Header("設定")]
    public CSVLoader loader;      // CSVLoaderスクリプト
    public AudioDataSO audioDataSO; 

    [ContextMenu("Load CSV Now")]
    public void RunTest()
    {
        if (loader == null || audioDataSO == null || audioDataSO.notesobjSO == null)
        {
            Debug.LogError("【テスト失敗】アタッチ漏れ、またはAudioDataSO内の譜面SOが空だぜ。");
            return;
        }

        //共通のAudioDataSOをloaderにセットして読み込み実行
        loader.audioDataSO = audioDataSO;
        List<NoteDate.Notes> result = loader.LoadChartFromAudioData();

        //AudioDataSOの中にある「本物のSO」に直接反映！
        audioDataSO.notesobjSO.notes = result;

        // コンソールに出力
        Debug.Log($"<color=green>【一本化読み込み完了】</color> 対象: {audioDataSO.musicTitle} | 合計: {result.Count}");
        foreach (var note in audioDataSO.notesobjSO.notes)
        {
            Debug.Log($"Time: {note.targetTime:F3} | Lane: {note.lane} | Type: {note.noteType}");
        }

#if UNITY_EDITOR
        // エディタ上でSOの変更を確定させて保存するぜ！
        UnityEditor.EditorUtility.SetDirty(audioDataSO.notesobjSO);
#endif
    }

    void Start()
    {
        RunTest();
    }
}
