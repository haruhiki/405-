using UnityEngine;

[CreateAssetMenu(fileName = "AudioDataSO", menuName = "Scriptable Objects/AudioData")]
public class AudioDataSO : ScriptableObject
{
    [Header("楽曲基本情報")]
    public string musicTitle = "楽曲タイトル名";
    public string artistName = "アーティスト名";
    public float bpm = 120;

    [Header("Audioアタッチ関連")]
    public AudioClip audioClip;    //楽曲データ本体
    public TextAsset csvChartFile; //譜面CSVファイル
    public NotesobjSO notesobjSO;  //パース済み譜面データSO

    [Header("エディタ用")]
    public float offsetSeconds = 0.0f; //楽曲の再生開始オフセット
}
