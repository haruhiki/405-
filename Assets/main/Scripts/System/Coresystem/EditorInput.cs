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
    [Tooltip("この秒数以上押し続けるとロングノーツになります")]
    [SerializeField] private float longNoteThreshold = 0.32f;
    [SerializeField] private float bothNoteThreshold = 0.05f;
    [SerializeField] private float skipSeconds = 5.0f;

    private float[] pressStartTimes = new float[5];
    private bool[] isKeyPressed = new bool[5];
    private bool[] hasPromotedToLong = new bool[5];

    private void Awake()
    {
        if (editorManager == null)
        {
            editorManager = GetComponent<ChartEditorManager>();
        }
    }

    private void Update()
    {
        if (editorManager == null || AudioManager.Instance == null) return;

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

        if (Input.GetKeyDown(saveKey))
        {
            editorManager.SaveChartData();
        }

        if (Input.GetKeyDown(skipForwardKey))
        {
            AudioManager.Instance.SkipTime(skipSeconds);
            if (TryGetComponent<ChartEditorGrid>(out var grid)) grid.ResetGridState();
        }
        if (Input.GetKeyDown(skipBackwardKey))
        {
            AudioManager.Instance.SkipTime(-skipSeconds);
            if (TryGetComponent<ChartEditorGrid>(out var grid)) grid.ResetGridState();
        }

        HandleLaneInputs();
    }

    private void HandleLaneInputs()
    {
        if (!editorManager.IsAudioReady) return;

        float currentTime = AudioManager.Instance.GetCurrentTime();
        int laneCount = Mathf.Min(laneKeys.Length, 5);

        // 1. キーを押した瞬間：即座にJudgePointから仮ショートノーツを発射する（離すまでのタイムラグ消去）
        for (int i = 0; i < laneCount; i++)
        {
            if (Input.GetKeyDown(laneKeys[i]))
            {
                pressStartTimes[i] = currentTime;
                isKeyPressed[i] = true;
                hasPromotedToLong[i] = false;

                NoteDate.NotesType initialType = TryGetActiveModifierNoteType(out var modType)
                    ? modType
                    : NoteDate.NotesType.Short;

                editorManager.BeginTapOrHoldPreview(i, currentTime, initialType);
            }
        }

        // 2. 押し続けている間：longNoteThreshold を超えたらロングノーツ表示へ昇格
        for (int i = 0; i < laneCount; i++)
        {
            if (isKeyPressed[i] && !hasPromotedToLong[i])
            {
                float holdDuration = currentTime - pressStartTimes[i];
                if (holdDuration >= longNoteThreshold)
                {
                    hasPromotedToLong[i] = true;
                    editorManager.PromotePreviewToLong(i);
                }
            }
        }

        // 3. キーを離した瞬間：仮プレビューを消して正式データへ登録
        for (int i = 0; i < laneCount; i++)
        {
            if (Input.GetKeyUp(laneKeys[i]) && isKeyPressed[i])
            {
                float startTime = pressStartTimes[i];
                float holdDuration = currentTime - startTime;
                isKeyPressed[i] = false;
                hasPromotedToLong[i] = false;

                editorManager.CancelHoldPreview(i);

                if (TryGetActiveModifierNoteType(out NoteDate.NotesType specialType))
                {
                    editorManager.AddNoteAtTime(i, startTime, specialType, holdDuration >= longNoteThreshold ? holdDuration : 0f);
                    continue;
                }

                if (holdDuration >= longNoteThreshold)
                {
                    editorManager.AddLongNote(i, startTime, currentTime, holdDuration);
                }
                else
                {
                    bool isBoth = CheckIsBothNote(i, startTime, laneCount);
                    NoteDate.NotesType type = isBoth ? NoteDate.NotesType.Both : NoteDate.NotesType.Short;
                    editorManager.AddNoteAtTime(i, startTime, type, 0f);
                }
            }
        }
    }

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

    private bool CheckIsBothNote(int currentLane, float startTime, int laneCount)
    {
        for (int j = 0; j < laneCount; j++)
        {
            if (j == currentLane) continue;
            if (isKeyPressed[j] && Mathf.Abs(pressStartTimes[j] - startTime) <= bothNoteThreshold)
            {
                return true;
            }
        }
        return editorManager.TryMarkSimultaneousNoteAsBoth(startTime, bothNoteThreshold);
    }


}
