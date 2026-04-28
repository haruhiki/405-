using System.Collections.Generic;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
    [SerializeField] public NotesobjSO notesSO;
    public List<NoteDate.Notes> LoadChart(TextAsset file, float bpm, float offset)
    {
        List<NoteDate.Notes> notes = new List<NoteDate.Notes>();
        string[] lines = file.text.Split('\n');

        //一行当たりの秒数
        float secondsPerRow = 60f / (bpm * notesSO.division);
        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i])) { continue; }
            string[] columns = lines[i].Split(',');
            // レーンをループ（0:左レーン, 1:右レーン）
            // スプレッドシートのB列が index[1], C列が index[2] なので、j + 1 する
            for (int j = 0; j <= 1; j++)
            {
                // インデックス外エラー防止
                if (columns.Length <= j + 1) { continue; }
                string cellValue = columns[j + 1].Trim();
                if (!string.IsNullOrEmpty(cellValue))
                {
                    NoteDate.Notes nots = new NoteDate.Notes();
                    nots.targetTime = offset + (i * secondsPerRow);
                    nots.lane = i;
                    nots.noteType = ParseType(cellValue);
                    notes.Add(nots);
                }
            }
        }
        return notes;
    }

    private NoteDate.NotesType ParseType(string value)
    {
        if (value.Contains("1")) return NoteDate.NotesType.Short;
        if (value.Contains("2")) return NoteDate.NotesType.Long;
        if (value.Contains("3")) return NoteDate.NotesType.Rush;
        return NoteDate.NotesType.Short; // デフォルト
    }

    public float CalculateSecondsPerRow(NotesobjSO data)
    {
        return 60f / (data.bpm * data.division);
    }


}
