using UnityEngine;

public class NoteDate
{
   /// <summary> /// ノーツの種類 /// </summary>
    public enum NotesType
    {
        Short = 0,       //短押し
        Long_Start = 1,  //長押し_開始
        Long_End = 2,    //長押し_終了
        Rush = 3,        //連打
        Both = 4,        //同時押し
    }

    /// <summary> /// ノーツ構造体 /// </summary>
    [System.Serializable]
    public struct Notes
    {
        public float targetTime;        
        public int lane;
        public Vector3 targetPosition; 
        public NotesType noteType;      //ノーツ種類
    }
}
