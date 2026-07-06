using UnityEngine;
public class Judge : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int myLane = 0;
    [SerializeField] private float perfectWindow = 0.05f;
    [SerializeField] private float greatWindow = 0.12f;
    [SerializeField] private float badWindow = 0.20f;
    [SerializeField] private float misstakeDamage = 10.0f;
    [SerializeField] private float longStartGrace = 0.18f;
    
    [Tooltip("Rushノーツを消すのに必要な連打回数")]
    [SerializeField] private int requiredRushHits = 5; 

    [Header("参照")]
    [SerializeField] private Define _defineSO;
    [SerializeField] private Charactor _charactor;

    private bool isLongPress;
    private NotesCon currentLongNote;
    private bool isLoopSEPlaying = false; 

    private void LateUpdate()
    {
        // Respect global pause
        if (PauseManager.Instance != null && PauseManager.IsPaused) return;

        if (_defineSO == null) return;

        if (isLongPress && currentLongNote == null)
        {
            ResetLongPressState();
            return;
        }

        // 生の入力状態を取得
        bool isMyLaneKey = (myLane == 0) ? _defineSO.isLeftKey : _defineSO.isRightKey;
        bool isBothKeys = _defineSO.isLeftKey && _defineSO.isRightKey; 
        
        bool isDetected = _defineSO.isInputDetected;
        bool isHold = _defineSO.isInputHold;

        // ロングノーツ中の生入力による補正
        if (isLongPress && !isMyLaneKey) isHold = false;

        // --- Rushノーツの最優先判定処理 ---
        if (isDetected)
        {
            NotesCon rushNote = GetActiveRushNote();
            if (rushNote != null)
            {
                if (isMyLaneKey)
                {
                    ProcessRushHit(rushNote);
                    return; 
                }
            }
        }

        // 判定対象のノーツ取得
        NotesCon targetNote = isLongPress ? currentLongNote : GetNearestNote();
        if (targetNote == null)
        {
            if (isLoopSEPlaying) StopLongPressSE();
            return;
        }

        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;
        float timeDiff = Mathf.Abs(targetNote.GetTargetTime() - currentTime);

        switch (targetNote.GetNoteType())
        {
            case NoteDate.NotesType.Short:
                if (isDetected) ProcessShortHit(targetNote, timeDiff);
                break;

            case NoteDate.NotesType.Both:

                if (isDetected && isBothKeys)
                {
                    if (myLane == 0)
                    {
                        ProcessShortHit(targetNote, timeDiff);
                    }
                    else
                    {
                        // 右レーン側は判定成功時にキャラクターの移動など同期演出だけを合わせて行う
                        if (timeDiff <= greatWindow)
                        {
                            MoveCharacterToNotePosition(transform.position);
                        }
                    }
                }
                break;

            case NoteDate.NotesType.Long_Start:
                ProcessLongHit(targetNote, currentTime, isDetected, isHold, isMyLaneKey);
                break;
        }
    }

    /// <summary>
    /// activeなRushノーツを取得するメソッド
    /// </summary>
    /// <returns></returns>
    private NotesCon GetActiveRushNote()
    {
        NotesCon[] notes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        foreach (var n in notes)
        {
            if (n != null && n.GetNoteType() == NoteDate.NotesType.Rush && n.IsRushReady)
            {
                return n;
            }
        }
        return null;
    }

    /// <summary>
    /// 最も近いノーツを取得するメソッド（Rushノーツは除外）
    /// </summary>
    /// <returns></returns>
    private NotesCon GetNearestNote()
    {
        NotesCon[] notes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        NotesCon best = null;
        float minDiff = badWindow;
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;

        foreach (var n in notes)
        {
            if (n == null) continue;
            
            if (n.GetNoteType() == NoteDate.NotesType.Rush) continue;
            if (isLongPress && n == currentLongNote) continue;

            // 【修正】同時押し（Both）ノーツは中央にあるため、レーンに関係なく引き受ける
            if (n.GetNoteType() != NoteDate.NotesType.Both)
            {
                // 通常ノーツやロングノーツは、自分のレーンのものだけを対象にする
                if (n.GetLane() != myLane) continue;
            }

            if (n.GetNoteType() == NoteDate.NotesType.Long_Start && currentTime >= n.GetTargetTime() && currentTime <= n.GetEndTime())
            {
                return n;
            }

            float diff = Mathf.Abs(n.GetTargetTime() - currentTime);
            if (diff < minDiff)
            {
                minDiff = diff;
                best = n;
            }
        }
        return best;
    }

    /// <summary>
    /// ショートノーツの判定処理
    /// </summary>
    /// <param name="note"></param>
    /// <param name="timeDiff"></param>
    private void ProcessShortHit(NotesCon note, float timeDiff)
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
    }

    /// <summary>
    /// ロングノーツの判定処理
    /// </summary>
    /// <param name="note"></param>
    /// <param name="currentTime"></param>
    /// <param name="isDetected"></param>
    /// <param name="isHold"></param>
    /// <param name="isMyLaneKey"></param>
    private void ProcessLongHit(NotesCon note, float currentTime, bool isDetected, bool isHold, bool isMyLaneKey)
    {
        if (!isLongPress)
        {
            if (isMyLaneKey && isDetected && currentTime >= note.GetTargetTime() - longStartGrace && currentTime <= note.GetEndTime())
            {
                BeginLongPress(note);
            }
            return;
        }

        note.SetHoldVisual(isMyLaneKey && isHold);

        if (isMyLaneKey && isHold && !isLoopSEPlaying)
        {
            PlayLongPressSE();
        }

        if (currentTime >= note.GetEndTime() - greatWindow)
        {
            FinishLongPress(note, currentTime);
            return;
        }

        if (!isMyLaneKey || !isHold)
        {
            Debug.Log($"<color=orange>[Judge] ホールドが途切れました（再ホールド受付中）</color>");
            StopLongPressSE();
            isLongPress = false;
            currentLongNote = null;
            note.SetHoldVisual(false);
            
            //  ミス判定の処理を行う
            if (GameSystem1.Instance != null && GameSystem1.Instance.RuntimeCharaSO != null)
            {
                GameSystem1.Instance.ApplyDamageToCharacter(misstakeDamage * Time.deltaTime * 2f);
            }
        }
    }

    /// <summary>
    /// ロングノーツの判定開始処理
    /// </summary>
    /// <param name="note"></param>
    private void BeginLongPress(NotesCon note)
    {
        isLongPress = true;
        currentLongNote = note;
        note.SetHoldVisual(true);
        MoveCharacterToNotePosition(transform.position);
        PlayLongPressSE();
    }

    /// <summary>
    ///  ロングノーツの判定終了処理
    /// </summary>
    /// <param name="note"></param>
    /// <param name="currentTime"></param>
    private void FinishLongPress(NotesCon note, float currentTime)
    {
        if (note == null) return;
        float endDiff = Mathf.Abs(note.GetEndTime() - currentTime);
        
        if (endDiff <= greatWindow) JudgePass(note, endDiff);
        else HandleMissProcessing(note, endDiff);

        ResetLongPressState();
    }
    
    /// ロングノーツの状態をリセットするメソッド
    private void ResetLongPressState()
    {
        if (currentLongNote != null) currentLongNote.SetHoldVisual(false);
        StopLoopSE();
        isLongPress = false;
        currentLongNote = null;
    }
    
    /// Rushノーツの判定処理
    private void ProcessRushHit(NotesCon note)
    {
        note.AddRushCount();
        _defineSO.PlayNoteSE(note.GetNoteType(), true);

        if (note.GetRushCount() >= requiredRushHits)
        {
            note.OnHit(); 
        }
    }

    /// 判定成功時の処理
    private void JudgePass(NotesCon targetNote, float calculateTimeDiff)
    {
        if (targetNote == null) return;
        _defineSO.PlayNoteSE(targetNote.GetNoteType(), true);
        targetNote.OnHit();
        // Score & combo handling
        if (ScoreManager.Instance != null)
        {
            string judgement = calculateTimeDiff <= perfectWindow ? "Perfect" : (calculateTimeDiff <= greatWindow ? "Great" : "Bad");
            int scoreAdd = ScoreManager.Instance.ScoreForJudgement(judgement);
            ScoreManager.Instance.AddHitScore(scoreAdd, targetNote.transform.position);
        }
    }
    
    /// ミス判定時の処理
    private void HandleMissProcessing(NotesCon targetNote, float timeDiff)
    {
        if (targetNote == null) return;
        _defineSO.PlayNoteSE(targetNote.GetNoteType(), false);
        targetNote.OnMiss();

        if (GameSystem1.Instance != null && GameSystem1.Instance.RuntimeCharaSO != null)
        {
            GameSystem1.Instance.ApplyDamageToCharacter(misstakeDamage);
        }
        // notify score manager of miss (resets combo)
        if (ScoreManager.Instance != null) ScoreManager.Instance.OnMiss();
    }

    // キャラクターの移動をノーツの位置に合わせるメソッドs
    private void MoveCharacterToNotePosition(Vector3 position)
    {
        if (_charactor != null) _charactor.MoveToPoint(position);
    }

    // ロングノーツの判定中にループSEを再生するメソッド
    private void PlayLongPressSE()
    {
        if (AudioManager.Instance != null && !isLoopSEPlaying)
        {
            AudioManager.Instance.PlayLoopSE(_defineSO.seCategory, _defineSO.longHitSE);
            isLoopSEPlaying = true;
        }
    }
    
    // ロングノーツの判定中にループSEを停止するメソッド
    private void StopLongPressSE()
    {
        if (AudioManager.Instance != null && isLoopSEPlaying)
        {
            AudioManager.Instance.StopLoopSE();
            isLoopSEPlaying = false;
        }
    }
    
    // 既存のメソッド名修正合わせ用
    private void StopLoopSE() => StopLongPressSE();
}