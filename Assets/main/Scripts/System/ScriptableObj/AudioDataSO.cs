using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AudioDataSO", menuName = "Scriptable Objects/AudioData")]
public class AudioDataSO : ScriptableObject
{
    [Header("楽曲基本情報")]
    public string musicTitle = "楽曲タイトル名";
    public string artistName = "アーティスト名";

    //TODO：こちらも譜面データクラスに保存するのでいらない
    public float bpm = 120;

    [Header("Audioアタッチ関連")]
    public AudioClip audioClip;    //楽曲データ本体

    //TODO:譜面CSVファイルはのちにいらなくなるので、削除する予定
    public TextAsset csvChartFile; //譜面CSVファイル

    //TODO:パース済み譜面データSOもいらない
    public NotesobjSO notesobjSO;  //パース済み譜面データSO

    [Header("エディタ用")]
    public float offsetSeconds = 0.0f; //楽曲の再生開始オフセット

    [Header("JSON譜面ファイル(ドラッグアンドドロップ用)")]
    [SerializeField] private TextAsset jsonFile;

    //JSONファイルのパス情報
    [Header("JSON譜面ファイルのパス")]
    public string JsonPath = "";

    //インスペクターの値が変更されたとき
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (jsonFile != null)
        {
            // Assets/から始まるパスを自動で文字列に変換して格納
            JsonPath = AssetDatabase.GetAssetPath(jsonFile);
        }
        else
        {
            JsonPath = "";
        }
#endif
    }

}
