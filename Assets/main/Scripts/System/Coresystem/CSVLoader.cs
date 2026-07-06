using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
    public AudioDataSO audioDataSO;

    /// <summary>
    /// AudioDataSOに登録されているCSVファイルと楽曲設定から譜面を生成する
    /// </summary>
    public List<NoteDate.Notes> LoadChartFromAudioData()
    {
        List<NoteDate.Notes> notes = new List<NoteDate.Notes>();

        if (audioDataSO == null || audioDataSO.notesobjSO == null || audioDataSO.csvChartFile == null)
        {
            Debug.LogError("[CSVLoader] AudioDataSO、または内部のSO・CSVファイルがセットされていません！");
            return notes;
        }

        TextAsset file = audioDataSO.csvChartFile;
        string[] lines = file.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');
            if (columns.Length < 4) { continue; }

            try
            {
                //各列からデータをパース
                float targetTime = float.Parse(columns[1].Trim());
                int lane = int.Parse(columns[2].Trim());
                int typeInt = int.Parse(columns[3].Trim());

                //構造体にデータを詰め込む
                NoteDate.Notes note = new NoteDate.Notes();
                note.targetTime = targetTime;
                note.lane = lane;

                if (typeInt == 1)
                {
                    note.noteType = NoteDate.NotesType.Short;
                }
                else if (typeInt == 2)
                {
                    //CSVの末尾コメント（5列目）を見て、StartかEndかを完璧に見極める
                    string noteComment = columns.Length > 4 ? columns[4].Trim() : "";
                    if (noteComment.Contains("LongEnd"))
                    {
                        note.noteType = NoteDate.NotesType.Long_End;
                    }
                    else
                    {
                        note.noteType = NoteDate.NotesType.Long_Start;
                    }
                }
                else if (typeInt == 3)
                {
                    note.noteType = NoteDate.NotesType.Rush;
                }

                //レーンに応じたターゲットの割り当て
                if (lane == 0)
                {
                    note.targetPosition = new Vector3(-3f, -1f, 0); // 左
                }
                else
                {
                    note.targetPosition = new Vector3(3f, -1f, 0);  // 右
                }

                notes.Add(note);
            }

            catch (Exception ex) 
            {
                Debug.LogWarning($"[CSVLoader] 行{i + 1}のパースに失敗):{ex.Message}");
            }
        }
        //時間順にソート
        notes.Sort((a, b) => a.targetTime.CompareTo(b.targetTime));
        return notes;
    }

}
