using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [Header("設定")]
    public CSVLoader loader;      // CSVLoaderスクリプト
    public TextAsset csvFile;     // スプレッドシートから出したCSV
    public NotesobjSO targetSO;   // データを流し込むScriptableObject

    [ContextMenu("Load CSV Now")] // インスペクターのコンポーネント名を右クリックで実行可能
    public void RunTest()
    {
        if (loader == null || csvFile == null || targetSO == null)
        {
            Debug.LogError("【テスト失敗】アタッチされていない項目があります。");
            return;
        }

        // 1. 読み込み実行
        List<NoteDate.Notes> result = loader.LoadChart(csvFile, targetSO.bpm, targetSO.offset);

        // 2. SOに反映
        targetSO.notes = result;

        // 3. コンソールに出力（確認用）
        Debug.Log($"<color=green>【読み込みテスト完了】</color> 合計ノーツ数: {result.Count}");

        // 最初の5件だけ中身を表示して計算が合っているか確認
        int displayCount = Mathf.Min(result.Count, 5);
        for (int i = 0; i < displayCount; i++)
        {
            var n = result[i];
            Debug.Log($"Index[{i}] Time: {n.targetTime:F3}s | Lane: {n.lane} | Type: {n.noteType}");
        }
    }

    // ゲーム開始時にも自動でテスト実行
    void Start()
    {
        RunTest();
    }
}
