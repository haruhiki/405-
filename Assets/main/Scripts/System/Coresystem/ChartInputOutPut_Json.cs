using UnityEngine;
using System.IO;

//JSONファイルからの譜面データの読み込みと書き込みを行うクラス
public class ChartInputOutPut_Json : MonoBehaviour
{
    
    /// <summary>  /// JSONファイルから譜面データを読み込む  /// </summary>
    /// <param name="jsonFilePath"></param>
    /// <returns></returns>
    public static ChartData LoadChartData(string jsonFilePath)
    {
        //JSONファイルの存在確認
        if(!File.Exists(jsonFilePath))
        {
            Debug.LogError("JSONファイルが見つかりません: " + jsonFilePath);
            return null;
        }

        //JSONファイルの読み込み
        string jsonString = File.ReadAllText(jsonFilePath);
        //JSON文字列をChartDataオブジェクトに変換
        ChartData chartData = JsonUtility.FromJson<ChartData>(jsonString);

        //読み込み時に時間順にソートする
        chartData.SortNotesByTargetTime();

        Debug.Log("Jsonファイルから譜面データを読み込んだ " + jsonFilePath);

        return chartData;
    }

    /// <summary> /// 譜面データをJSONファイルに書き込む /// </summary>
    /// <param name="jsonFilePath"></param>
    /// <param name="chartData"></param>
    public static void SaveChartData(string jsonFilePath, ChartData chartData)
    {

        //時間順にソート
        chartData.SortNotesByTargetTime();

        //ChartDataオブジェクトをJSON文字列に変換
        string jsonString = JsonUtility.ToJson(chartData, true);

        //JSONファイルに書き込み
        File.WriteAllText(jsonFilePath, jsonString);

        Debug.Log("譜面データをJsonファイルに書き込んだ " + jsonFilePath);
    }

}
