using UnityEngine;

[CreateAssetMenu(fileName = "StageDataSO", menuName = "Scriptable Objects/StageData")]
public class StageDataSO : ScriptableObject
{
    // Stageの基本情報
    [Header("Stage Identity")]
    public string stageName = "New Stage";             // Stageの名前
    public string description = "Stage description";   // Stageの説明

    [Header("Music Data")]
    public AudioDataSO musicData;                      // 音楽データSOへの参照

    [Header("Stage Visual")]
    public Sprite backgroundSprite;                    // 背景画像
    public Color backgroundColor = Color.white;        // 背景色

    [Header("Gameplay Rules")]
    [Min(0f)] public float initialHP = 200f;             // 初期HP
    [Min(0f)] public float missDamage = 10f;             // ミス時のダメージ
    [Min(0.1f)] public float noteSpeedMultiplier = 1f;   // ノーツの速度倍率

    [Header("Result Settings")]
    public string clearMessage = "Clear!";              // クリアメッセージ
    public string failMessage = "Try Again";            // 失敗メッセージ

    // プロパティでのアクセス
    public string DisplayName => string.IsNullOrEmpty(stageName) ? (musicData != null ? musicData.musicTitle : "Stage") : stageName;

    // 音楽タイトルを表示するプロパティ
    public string DisplayMusicTitle => musicData != null ? musicData.musicTitle : stageName;
}
