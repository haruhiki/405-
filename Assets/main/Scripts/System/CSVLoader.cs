using System;
using System.Collections.Generic;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
    [SerializeField] public NotesobjSO notesSO;
    public List<NoteDate.Notes> LoadChart(TextAsset file, float bpm, float offset)
    {
        List<NoteDate.Notes> notes = new List<NoteDate.Notes>();

        //ガード：BPMやDivisionが0だと計算できないのでデフォルト値を設定
        if (bpm <= 0) bpm = 120f;
        if (notesSO.division <= 0) notesSO.division = 4; 

        //1行あたりの秒数計算
        float secondsPerRow = 60f / (bpm * notesSO.division);

        string[] lines = file.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');

            for (int j = 0; j <= 1; j++)
            {
                int colIndex = j + 2;
                if (columns.Length <= colIndex) { continue; }

                string cellValue = columns[colIndex].Trim();

                // 数値（1, 2, 3）が入っているときだけ処理
                if (!string.IsNullOrEmpty(cellValue) && cellValue != "0")
                {
                    NoteDate.Notes nots = new NoteDate.Notes();

                    // 計算結果を代入
                    nots.targetTime = offset + ((i - 1) * secondsPerRow);
                    nots.lane = j;
                    nots.noteType = ParseType(cellValue);

                    notes.Add(nots);
                }
            }
        }
        return notes;
    }

    private NoteDate.NotesType ParseType(string value)
    {
        if (value.Contains("1") || value.Contains("１")) return NoteDate.NotesType.Short;
        if (value.Contains("2") || value.Contains("２")) return NoteDate.NotesType.Long;
        if (value.Contains("3") || value.Contains("３")) return NoteDate.NotesType.Rush;

        return NoteDate.NotesType.Short;
    }

    public float CalculateSecondsPerRow(NotesobjSO data)
    {
        return 60f / (data.bpm * data.division);
    }


}
