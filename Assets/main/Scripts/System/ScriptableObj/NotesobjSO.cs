using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NotesobjSO", menuName = "Scriptable Objects/NotesobjSO")]
public class NotesobjSO : ScriptableObject
{
    [SerializeField] public float bpm = 120f;
    [SerializeField] public float  offset = 4;
    [SerializeField] public int division = 0;

    public List<NoteDate> notes;
}
