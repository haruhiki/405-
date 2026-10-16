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
        Flick = 5,         //フリック
        Damage = 6,        //ダメージノーツ
    }

    /// <summary> /// ノーツ構造体 /// </summary>
    [System.Serializable]
    public struct Notes
    {
        public int id;            //ノーツID
        public float targetTime;  //判定ライン到達時間        
        public int lane;          //ノーツレーン(0,1,2,3,4)
        public Vector3 targetPosition; 
        public NotesType noteType;      //ノーツ種類
        public float Length;    //ロングノーツの長さ
    }

    /// <summary> /// ノーツデータ /// </summary>
    public Notes notesData;
    /// <summary> /// ノーツデータを複製するメソッド /// </summary>
    public Notes CloneNote()
    {
        return this.notesData;
    }
}
