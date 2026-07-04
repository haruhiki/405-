using UnityEngine;

public class NoteDate
{
    /// <summary> /// �m�[�c�̎�� /// </summary>
    public enum NotesType
    {
        Short = 0,       //�Z����
        Long_Start = 1,  //������_�J�n
        Long_End = 2,    //������_�I��
        Rush = 3,        //�A��
    }

    /// <summary> /// �m�[�c�\���� /// </summary>
    [System.Serializable]
    public struct Notes
    {
        public float targetTime;        
        public int lane;
        public Vector3 targetPosition; 
        public NotesType noteType;      //�m�[�c���
    }
}
