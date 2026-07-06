using UnityEngine;

[CreateAssetMenu(fileName = "StageDataSO", menuName = "Scriptable Objects/StageData")]
public class StageDataSO : ScriptableObject
{
    [Header("Stage Identity")]
    public string stageName = "New Stage";
    public string description = "Stage description";

    [Header("Music Data")]
    public AudioDataSO musicData;

    [Header("Stage Visual")]
    public Sprite backgroundSprite;
    public Color backgroundColor = Color.white;

    [Header("Gameplay Rules")]
    [Min(0f)] public float initialHP = 200f;
    [Min(0f)] public float missDamage = 10f;
    [Min(0.1f)] public float noteSpeedMultiplier = 1f;

    [Header("Result Settings")]
    public string clearMessage = "Clear!";
    public string failMessage = "Try Again";

    public string DisplayName => string.IsNullOrEmpty(stageName) ? (musicData != null ? musicData.musicTitle : "Stage") : stageName;
    public string DisplayMusicTitle => musicData != null ? musicData.musicTitle : stageName;
}
