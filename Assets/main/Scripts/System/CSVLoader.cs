using System.Collections.Generic;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
    [SerializeField] public NotesobjSO notesSO;
    public List<NoteDate> LoadChart(TextAsset file,float bpm,float offset) 
    {
        List<NoteDate> notes = new List<NoteDate>();
        string[] lines = file.text.Split('\n');

        //àÍçsìñÇΩÇËÇÃïbêî
        float secondsPerRow = 60f / (bpm * notesSO.division);
        for(int i = 0; i < lines.Length; i++) 
        {
            if (string.IsNullOrEmpty(lines[i])) { continue; }
            string[] columns = lines[i].Split(',');




        }



        return notes;
    }

    public float CalculateSecondsPerRow(NotesobjSO data)
    {
        return 60f / (data.bpm * data.division);
    }


}
