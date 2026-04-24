using UnityEngine;

public class NoteDate : MonoBehaviour
{
    /// <summary> /// ノーツの種類 /// </summary>
    public enum NotesType
    {
        Short = 0, //短押し
        Long = 1,  //長押し
        Rush = 2,  //連打
    }

    /// <summary> /// ノーツ構造体 /// </summary>
    struct Notes
    {
        float targetTime;
        int lane;
        NotesType noteType;
    }
}
