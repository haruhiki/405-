using System;
using System.Collections.Generic;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
    [SerializeField] public NotesobjSO notesSO;
    public List<NoteDate.Notes> LoadChart(TextAsset file, float bpm, float offset)
    {
        List<NoteDate.Notes> notes = new List<NoteDate.Notes>();
        if (bpm <= 0) bpm = 120f;
        if (notesSO.division <= 0) notesSO.division = 4;

        float secondsPerRow = 60f / (bpm * notesSO.division);
        string[] lines = file.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');

            // columns[2]‚ªB—ñ(¶)Acolumns[3]‚ªC—ñ(‰E)
            for (int j = 0; j <= 1; j++)
            {
                int colIndex = j + 2; // 2 ‚Ü‚½‚Í 3
                if (columns.Length <= colIndex) continue;

                string cellValue = columns[colIndex].Trim();

                if (string.IsNullOrEmpty(cellValue) || cellValue == "0") continue;
                if (int.TryParse(cellValue, out int noteTypeInt))
                {
                    NoteDate.Notes nots = new NoteDate.Notes();
                    nots.targetTime = offset + ((i - 1) * secondsPerRow);
                    nots.noteType = ParseType(cellValue);
                    nots.lane = j; 
                    if (j == 0) //¶ƒŒ[ƒ“
                    {
                        nots.targetPosition = new Vector3(-3f, -1f, 0);
                    }
                    else //‰EƒŒ[ƒ“
                    {
                        nots.targetPosition = new Vector3(3f, -1f, 0);
                    }

                    notes.Add(nots);
                }
            }
        }
        return notes;
    }

    private NoteDate.NotesType ParseType(string value)
    {
        if (value.Contains("1") || value.Contains("‚P")) return NoteDate.NotesType.Short;
        if (value.Contains("2") || value.Contains("‚Q")) return NoteDate.NotesType.Long;
        if (value.Contains("3") || value.Contains("‚R")) return NoteDate.NotesType.Rush;

        return NoteDate.NotesType.Short;
    }

    public float CalculateSecondsPerRow(NotesobjSO data)
    {
        return 60f / (data.bpm * data.division);
    }


}
