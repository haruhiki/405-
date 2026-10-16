using UnityEngine;
using System.Collections.Generic;
public class ChartData : MonoBehaviour
{
    //JSON形式の譜面データを格納するためのクラス
    public string title = "Default Title";   //譜面のタイトル
    public string artist = "Default Artist"; //譜面のアーティスト
    public string audiofileName = "Song.ogg"; //譜面のオーディオファイル名
    public float bpm = 120f;               //譜面のBPM
    public float offset = 0f;              //曲の頭から曲の開始までのオフセット時間（秒）
    
    //ノーツデータのリスト
    public List<NoteDate.Notes> notes = new List<NoteDate.Notes>();

    //ノーツのリストを到達時間（昇順）でソートするメソッド
    public void SortNotesByTargetTime()
    {
      //ノーツのリストを到達時間で昇順にソート
      notes.Sort((a,b) => a.targetTime.CompareTo(b.targetTime));
    }

    //1拍あたりの秒数を取得
    public float GetSecondsPerBeat => 60f / Mathf.Max(bpm, 1.0f); // BPMが0の場合の除算エラーを防ぐために最大値を使用
   
}
