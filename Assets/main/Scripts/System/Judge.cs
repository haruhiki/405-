using UnityEngine;

public class Judge : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int myLane;                     // 0:左, 1:右
    [SerializeField] private float perfectWindow = 0.05f;    // 良の許容時間差 (秒)
    [SerializeField] private float greatWindow = 0.12f;      // 可の許容時間差 (秒)
    [SerializeField] private float badWindow = 0.20f;        // 不可（これ以上離れていたら無視）の許容時間差
    [SerializeField] private float misstakeDamage = 10.0f;   // ミス時に受けるダメージ量

    [Header("参照")]
    [SerializeField] private Define _defineSO;
    [SerializeField] private Charactor _charactor;

    private bool isLongPress = false;
    private NotesCon currentLongNote = null;

    void Update()
    {
        if (_defineSO == null) return;

        // ロングノーツを押しっぱなしで完走し、NotesCon側が自動消滅（Destroy）した場合の検知
        if (isLongPress && currentLongNote == null)
        {
            Debug.Log("<color=orange>【ロング完走】ノーツの自動消滅を検知。フラグを正常リセットします。</color>");
            isLongPress = false;
            StopLongPressSE();
            ConsumeInput();
        }

        // そもそも全体で何の入力もなければ即スルー
        if (!_defineSO.HasInput) return;

        // myLane（0:左, 1:右）に応じて自分のレーンの入力を割り出し
        bool isMyLaneKey = (myLane == 0) ? _defineSO.isLeftKey : _defineSO.isRightKey;
        if (!isMyLaneKey) return;

        // 共通の入力状態を取得
        bool isDetected = _defineSO.isInputDetected;
        bool isHold = _defineSO.isInputHold;
        bool isRush = _defineSO.isInputRush;

        // 対象ノーツを時間軸から正確にキャッチ
        NotesCon targetNote = isLongPress ? currentLongNote : GetNearestNote();
        
        // 叩くべきノーツがもうシーンにない（null）のにキー入力だけ残っている場合の安全弁
        if (targetNote == null)
        {
            ConsumeInput();
            return;
        }

        // 音ゲーの絶対正義：時間差の計算
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;
        float timeDiff = Mathf.Abs(targetNote.GetTargetTime() - currentTime);

        // ノーツタイプごとの判定分岐
        switch (targetNote.GetNoteType())
        {
            case NoteDate.NotesType.Short:
                if (isDetected)
                {
                    ProcessShortHit(targetNote, timeDiff);
                }
                break;

            case NoteDate.NotesType.Long_Start:
                // 押し始め(isDetected)だけでなく、途中からの長押し(isHold)も引数に投げて処理するぜ！
                ProcessLongHit(targetNote, timeDiff, isDetected, isHold, isRush, currentTime);
                break;

            case NoteDate.NotesType.Rush:
                if (isDetected)
                {
                    ProcessRushHit(targetNote, timeDiff);
                }
                break;
        }
    }

    /// <summary>
    /// ? 今叩くべき、最も現在時間に近い、または「現在通過中」のノーツを1つだけ索敵
    /// </summary>
    NotesCon GetNearestNote()
    {
        NotesCon[] notes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        
        NotesCon best = null;
        float minDiff = badWindow; 
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;

        foreach (var n in notes)
        {
            if (n.GetLane() != myLane) continue;

            // 既にNotesCon側で消滅フラグが立っているゾンビはスルー
            //（※NotesConに public bool IsDestroyed のプロパティがあれば連動、なければこの行をコメントアウトでもOK）
            // if (n.IsDestroyed) continue; 

            // ? 【途中から判定をとるための超絶パワーアップ】
            // もしロングノーツで、既に始点を過ぎている（currentTime >= targetTime）が、
            // まだ終点を過ぎていない（currentTime <= endTime）通過中のノーツだった場合、最優先でロックオンする！
            if (n.GetNoteType() == NoteDate.NotesType.Long_Start && 
                currentTime >= n.GetTargetTime() && currentTime <= n.GetEndTime())
            {
                return n; // 通過中のロングノーツを発見したら即座にこれを返す！
            }

            // 通常の距離計算（ショートや、まだ判定ラインに到達していないノーツ用）
            float diff = Mathf.Abs(n.GetTargetTime() - currentTime);
            if (diff < minDiff)
            {
                minDiff = diff;
                best = n;
            }
        }
        return best;
    }

    /// <summary> 短押し判定 </summary>
    void ProcessShortHit(NotesCon note, float timeDiff)
    {
        if (timeDiff <= greatWindow)
        {
            MoveCharacterToNotePosition(transform.position);
            JudgePass(note, timeDiff);
        }
        else
        {
            HandleMissProcessing(note, timeDiff);
        }
        ConsumeInput();
    }

    /// <summary> 長押し判定（途中からの割り込み対応版） </summary>
    void ProcessLongHit(NotesCon note, float timeDiff, bool isDetected, bool isHold, bool isRush, float currentTime)
    {
        // ── 【新規実装】ロングノーツの「途中からの判定取得」処理 ──
        // まだ長押し状態になっていないが、「すでに始点を通過中」かつ「指がホールド(isHold)」された場合！
        if (!isLongPress && isHold && currentTime >= note.GetTargetTime() && currentTime <= note.GetEndTime())
        {
            isLongPress = true;
            currentLongNote = note;
            note.SetHoldVisual(true); // ノーツに長押し中であることを伝える（これで帯が縮み出すぜ！）
            MoveCharacterToNotePosition(transform.position);

            _defineSO.PlayNoteSE(NoteDate.NotesType.Long_Start, true);
            Debug.Log("<color=lime>【長押し途中復帰】ノーツの途中からホールドを検知・復帰したぜ！</color>");
            
            ConsumeInput();
            return;
        }

        // ① 通常の長押し開始（完璧に頭から叩いた瞬間）
        if (isDetected && !isLongPress)
        {
            if (timeDiff <= greatWindow)
            {
                isLongPress = true;
                currentLongNote = note;
                note.SetHoldVisual(true);  
                MoveCharacterToNotePosition(transform.position);

                _defineSO.PlayNoteSE(NoteDate.NotesType.Long_Start, true);
                Debug.Log("<color=cyan>【長押し開始】ジャストタイミングでホールド成功！</color>");
            }
            else
            {
                HandleMissProcessing(note, timeDiff);
            }
            ConsumeInput();
            return;
        }

        // これ以降は長押しロックオン（isLongPress）中の処理
        if (!isLongPress) return;

        // ② 途中で指が完全に離れてしまった場合（ホールド失敗・離脱）
        // ※ノーツが外部で勝手に消えた場合（Update側の寿命）もここで安全に外す
        if ((!isDetected && !isHold && !isRush) || note == null)
        {
            StopLongPressSE();
            Debug.Log("<color=red>【長押し失敗】ホールド中に指が離れたぜ！</color>");
            
            if (note != null)
            {
                note.SetHoldVisual(false);
                HandleMissProcessing(note, timeDiff);
            }
            
            isLongPress = false;
            currentLongNote = null;
            ForceResetAllInputFlags(); 
            return;
        }

        // ③ 長押しの完了（タイミングよく指を離した瞬間）
        if (isRush && isLongPress)
        {
            StopLongPressSE();
            float endTimeDiff = Mathf.Abs(note.GetEndTime() - currentTime);

            note.SetHoldVisual(false);

            if (endTimeDiff <= greatWindow)
            {
                JudgePass(note, endTimeDiff);
            }
            else
            {
                HandleMissProcessing(note, endTimeDiff);
            }

            isLongPress = false;
            currentLongNote = null;
            ConsumeInput();
        }
    }

    /// <summary> 連打判定 </summary>
    void ProcessRushHit(NotesCon note, float timeDiff)
    {
        JudgePass(note, timeDiff);
        ConsumeInput();
    }

    private void JudgePass(NotesCon targetNote, float caluculateTimeDiff) 
    {
        if (targetNote == null) return;

        if (caluculateTimeDiff <= perfectWindow)
        {
            Debug.Log($"<color=orange>★★ 良 (Perfect) ★★</color> 誤差: {caluculateTimeDiff:F3}s");
            _defineSO.PlayNoteSE(targetNote.GetNoteType(), true);
            targetNote.OnHit(); 
        }
        else 
        {
            Debug.Log($"<color=yellow>可 (Great) </color> 誤差: {caluculateTimeDiff:F3}s");
            _defineSO.PlayNoteSE(targetNote.GetNoteType(), true);
            targetNote.OnHit();
        }
    }

    private void HandleMissProcessing(NotesCon targetNote, float timeDiff)
    {
        Debug.Log($"<color=red>?不可 (Miss)?</color> 誤差: {timeDiff:F3}s");
        _defineSO.PlayNoteSE(targetNote.GetNoteType(), false);
        targetNote.OnMiss();

        if (_defineSO.charactorSO != null)
        {
            _defineSO.charactorSO.HPfluctuation(misstakeDamage);
        }
    }

    private void MoveCharacterToNotePosition(Vector3 position)
    {
        if (_charactor != null) _charactor.MoveToPoint(position);
    }

    private void StopLongPressSE()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSE();
    }

    private void ConsumeInput()
    {
        if (myLane == 0) _defineSO.isLeftKey = false;
        else _defineSO.isRightKey = false;

        _defineSO.isInputDetected = false;
        _defineSO.isInputRush = false;
    }

    private void ForceResetAllInputFlags()
    {
        _defineSO.isLeftKey = false;
        _defineSO.isRightKey = false;
        _defineSO.isInputDetected = false;
        _defineSO.isInputHold = false;
        _defineSO.isInputRush = false;
    }
}