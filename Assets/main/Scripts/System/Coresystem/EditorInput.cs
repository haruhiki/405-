using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoteModifierMapping
{
    public KeyCode modifierKey;
    public NoteDate.NotesType noteType;
}


public class EditorInput : MonoBehaviour
{
   [Header("連携先マネージャー")]
    [SerializeField] private ChartEditorManager editorManager;

    [Header("再生・保存キー設定")]
    [SerializeField] private KeyCode playPauseKey = KeyCode.Space;
    [SerializeField] private KeyCode saveKey = KeyCode.F5;
    [SerializeField] private KeyCode skipForwardKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode skipBackwardKey = KeyCode.LeftArrow;

    [Header("5レーン入力キー (Lane 0 ? 4)")]
    [SerializeField] private KeyCode[] laneKeys = new KeyCode[]
    {
        KeyCode.D, // Lane 0
        KeyCode.F, // Lane 1
        KeyCode.G, // Lane 2
        KeyCode.H, // Lane 3
        KeyCode.J  // Lane 4
    };

    [Header("特殊ノーツの修飾キー設定（Inspectorから拡張可能）")]
    [SerializeField] private List<NoteModifierMapping> modifierMappings = new List<NoteModifierMapping>()
    {
        new NoteModifierMapping { modifierKey = KeyCode.LeftShift, noteType = NoteDate.NotesType.Damage },
        new NoteModifierMapping { modifierKey = KeyCode.LeftControl, noteType = NoteDate.NotesType.Flick }
    };

    [Header("ノーツ自動判定のしきい値 (秒)")]
    [Tooltip("この時間以上押し続けたらロングノーツとして登録")]
    [SerializeField] private float longNoteThreshold = 0.20f;
    [Tooltip("この時間差以内の複数押しを同時押し(Both)として判定")]
    [SerializeField] private float bothNoteThreshold = 0.05f;
    [SerializeField] private float skipSeconds = 5.0f;

    // 各レーンの押下状態管理
    private float[] pressStartTimes = new float[5];
    private bool[] isKeyPressed = new bool[5];

    private void Update()
    {
        if (editorManager == null || AudioManager.Instance == null) return;

        // 1. BGMの再生 / 一時停止 (Space)
        if (Input.GetKeyDown(playPauseKey))
        {
            if (editorManager.IsAudioReady)
            {
                AudioManager.Instance.TogglePlayPause();
            }
            else
            {
                Debug.LogWarning("[ChartEditorInput] 楽曲のロードが完了していないため再生できません。");
            }
        }

        // 2. 譜面データの保存 (F5)
        if (Input.GetKeyDown(saveKey))
        {
            editorManager.SaveChartData();
        }

        // 3. 早送り / 巻き戻し (左右矢印キー)
        if (Input.GetKeyDown(skipForwardKey))
        {
            AudioManager.Instance.SkipTime(skipSeconds);
        }
        if (Input.GetKeyDown(skipBackwardKey))
        {
            AudioManager.Instance.SkipTime(-skipSeconds);
        }

        // 4. 5レーン入力（Short / Long / Both / 特殊ノーツ判定）
        HandleLaneInputs();
    }

    private void HandleLaneInputs()
    {
        if (!editorManager.IsAudioReady) return;

        float currentTime = AudioManager.Instance.GetCurrentTime();
        int laneCount = Mathf.Min(laneKeys.Length, 5);

        // キーを押した瞬間：開始時刻を記録
        for (int i = 0; i < laneCount; i++)
        {
            if (Input.GetKeyDown(laneKeys[i]))
            {
                pressStartTimes[i] = currentTime;
                isKeyPressed[i] = true;
            }
        }

        // キーを離した瞬間：押下時間・修飾キー・同時押し状況に応じて確定
        for (int i = 0; i < laneCount; i++)
        {
            if (Input.GetKeyUp(laneKeys[i]) && isKeyPressed[i])
            {
                float startTime = pressStartTimes[i];
                float holdDuration = currentTime - startTime;
                isKeyPressed[i] = false;

                // A. 修飾キー（Damage / Flick / 将来追加ノーツ等）が押されているかチェック
                if (TryGetActiveModifierNoteType(out NoteDate.NotesType specialType))
                {
                    editorManager.AddNoteAtTime(i, startTime, specialType, holdDuration >= longNoteThreshold ? holdDuration : 0f);
                    continue;
                }

                // B. ロングノーツ判定（一定時間以上押し続けた場合）
                if (holdDuration >= longNoteThreshold)
                {
                    editorManager.AddLongNote(i, startTime, currentTime, holdDuration);
                }
                // C. 単押し / 同時押し判定
                else
                {
                    bool isBoth = CheckIsBothNote(i, startTime, laneCount);
                    NoteDate.NotesType type = isBoth ? NoteDate.NotesType.Both : NoteDate.NotesType.Short;

                    editorManager.AddNoteAtTime(i, startTime, type, 0f);
                }
            }
        }
    }

    /// <summary>
    /// 現在押下中の修飾キーに対応する NotesType があれば取得する
    /// </summary>
    private bool TryGetActiveModifierNoteType(out NoteDate.NotesType matchedType)
    {
        for (int i = 0; i < modifierMappings.Count; i++)
        {
            if (Input.GetKey(modifierMappings[i].modifierKey))
            {
                matchedType = modifierMappings[i].noteType;
                return true;
            }
        }
        matchedType = NoteDate.NotesType.Short;
        return false;
    }

    /// <summary>
    /// 同時押し（Both）かどうかを判定
    /// </summary>
    private bool CheckIsBothNote(int currentLane, float startTime, int laneCount)
    {
        // 1. 現在ほかに押されているレーンの開始時刻が近い場合
        for (int j = 0; j < laneCount; j++)
        {
            if (j == currentLane) continue;
            if (isKeyPressed[j] && Mathf.Abs(pressStartTimes[j] - startTime) <= bothNoteThreshold)
            {
                return true;
            }
        }

        // 2. 先に離されて登録済みの直前ノーツと開始時刻がほぼ同じ場合、既存ノーツも Both に更新
        return editorManager.TryMarkSimultaneousNoteAsBoth(startTime, bothNoteThreshold);
    }

}
