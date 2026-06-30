using System.Transactions;
using UnityEngine;

public class Judge : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int myLane; // 0:左, 1:右
    [SerializeField] private float judgeRadius = 1.5f;     // 判定が有効な円の半径
    [SerializeField] private float perfectWindow = 0.05f;  // 良の許容時間差
    [SerializeField] private float greatWindow = 0.12f;    // 可の許容時間差
    [SerializeField] private float misstakeDamage = 10.0f; // ミス時に受けるダメージ量

    private float distance = 0;   //距離判定変数

    [Header("参照")]
    [SerializeField] private Define _defineSO;
    [SerializeField] private CharactorSO _charaSO;
    [SerializeField] private Charactor _charactor;
    [SerializeField] private Transform leftTargetCircle;
    [SerializeField] private Transform rightTargetCircle;
    private AudioSource audioSource;

    private bool isLongPress = false;
    private NotesCon currentLongNote = null;

    void Start() => audioSource = FindFirstObjectByType<AudioSource>();

    void Update()
    {
        if(_defineSO == null) { return; }

        if(!_defineSO.HasInput) { return; }

        // キー入力かタッチ入力か判定
        bool isKeyInput = _defineSO.isRightKey || _defineSO.isLeftKey;
        
        Vector3 judgePos = transform.position;
        
        // キー入力の場合は判定円の位置を直接使用
        if (isKeyInput)
        {
            if (myLane == 0 && leftTargetCircle != null)
                judgePos = leftTargetCircle.position;
            else if (myLane == 1 && rightTargetCircle != null)
                judgePos = rightTargetCircle.position;
        }

        //座標変換
        float camToPlaneDist = Mathf.Abs(Camera.main.transform.position.z - judgePos.z);
        Vector3 screenPosWithDepth = new Vector3(_defineSO.inputScreenPos.x, _defineSO.inputScreenPos.y, camToPlaneDist);
        Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(screenPosWithDepth);
        touchWorldPos.z = 0;

        //距離判定
        distance = Vector2.Distance(touchWorldPos, judgePos);
        if (distance > judgeRadius)
        {
            if (_defineSO.isInputDetected) 
            {
                Debug.LogWarning($"<color=red>[押し始め脱落] 判定円の外です。距離: {distance:F2} / 許容: {judgeRadius}</color>");
            }
            return; 
        }

        //このレーンのノーツを取得
        NotesCon targetNote = isLongPress ? currentLongNote : GetNearestNote();
        if (targetNote == null) { return; }

        //ノーツの判定
        switch (targetNote.GetNoteType())
        {
            case NoteDate.NotesType.Short:
                if (_defineSO.isInputDetected) ProcessShortHit(targetNote);
                break;

            case NoteDate.NotesType.Long_Start:
                ProcessLongHit(targetNote);
                break;

            case NoteDate.NotesType.Rush:
                if(_defineSO.isInputDetected) ProcessRushHit(targetNote);
                break;
        }
    }

    /// <summary> /// 近くのノーツを取得 /// </summary>
    /// <returns></returns>
    NotesCon GetNearestNote()
    {
        NotesCon[] notes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        Debug.Log($"[Judge] シーン内のノーツ数: {notes.Length}");
        
        NotesCon best = null;
        float minDiff = 0.5f; // 0.5秒以上離れているものは対象外
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;

        foreach (var n in notes)
        {
            if (n.GetLane() != myLane)
            {
                Debug.Log($"[Judge] ノーツのレーン {n.GetLane()} は対象外（myLane: {myLane}）");
                continue;
            }
            float diff = Mathf.Abs(n.GetTargetTime() - currentTime);
            Debug.Log($"[Judge] ノーツ見つかった: targetTime={n.GetTargetTime():F3}, currentTime={currentTime:F3}, diff={diff:F3}");
            if (diff < minDiff)
            {
                minDiff = diff;
                best = n;
            }
        }
        Debug.Log($"[Judge] 最終選択ノーツ: {(best != null ? "有" : "無")} (minDiff={minDiff:F3})");
        return best;
    }

    /// <summary> /// 短押し /// </summary>
    /// <param name="note"> ノーツタイプがshort</param>
    void ProcessShortHit(NotesCon note)
    {
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;
        float diff = Mathf.Abs(note.GetTargetTime() - currentTime);
        Debug.Log($"[Judge] Short 判定: diff={diff:F3}, greatWindow={greatWindow}");
        if (diff <= greatWindow)
        {
            Debug.Log($"[Judge] Short 判定範囲内！JudgePass呼び出し");
            // 成功なら消す
            //判定パス
            JudgePass(note,diff);
        }
        else
        {
            Debug.Log($"[Judge] Short 判定範囲外");
        }
    }

    /// <summary> /// 長押し /// </summary>
    /// <param name="note"> noteTypeがLongの場合 </param>
    void ProcessLongHit(NotesCon note)
    {
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;
        
        // 押し始めの判定（キーが新しく押された瞬間）
        if (_defineSO.isInputDetected && !isLongPress)
        {
            float timeDiff = Mathf.Abs(note.GetTargetTime() - currentTime);

            if (timeDiff <= greatWindow)
            {
                isLongPress = true;
                currentLongNote = note;
                note.SetHoldVisual(true);
                
                try
                {
                    // ロングSEループ開始
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayLoopSE(_defineSO.seCategory, _defineSO.longHitSE);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Judge] SE再生エラー: {ex.Message}");
                }
                
                Debug.Log("<color=cyan>【長押し開始】ホールド中...</color>");
            }
            return;
        }

        // ホールド中に指が離れた場合
        if (isLongPress && !_defineSO.isInputHold && !_defineSO.isInputRush)
        {
            try
            {
                // SEループ停止
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopLoopSE();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Judge] SE停止エラー: {ex.Message}");
            }
            
            float timeDiff = Mathf.Abs(note.GetEndTime() - currentTime);
            
            // 終了時間まであと少しなら成功判定を期待
            if (timeDiff <= greatWindow)
            {
                Debug.Log("<color=yellow>【長押し途中離し】判定待機中...</color>");
                // ホールド解除のみ、まだノーツは消さない
                note.SetHoldVisual(false);
                isLongPress = false;
                currentLongNote = null;
                return;
            }
            else
            {
                // 終了時間より大幅に手前で離したなら失敗
                Debug.Log("<color=red>【長押し失敗】途中で指が離れました！</color>");
                note.SetHoldVisual(false);
                note.OnMiss();
                isLongPress = false;
                currentLongNote = null;
                
                // ダメージ処理
                if (GameSystem1.Instance != null)
                {
                    GameSystem1.Instance.ApplyDamageToCharacter(misstakeDamage);
                }
                else if (_charaSO != null)
                {
                    _charaSO.HPfluctuation(misstakeDamage);
                }
                return;
            }
        }

        // 離した時の判定（タイミングよく指を離した瞬間）
        if (_defineSO.isInputRush && isLongPress)
        {
            float timeDiff = Mathf.Abs(note.GetEndTime() - currentTime);
            Debug.Log($"【長押し完了】離し誤差: {timeDiff:F3}");
            
            try
            {
                // SEループ停止
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopLoopSE();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Judge] SE停止エラー: {ex.Message}");
            }
            
            note.SetHoldVisual(false);

            JudgePass(note, timeDiff); // 成功判定なら OnHit() でノーツ消滅

            isLongPress = false;
            currentLongNote = null;
        }
    }

    /// <summary> /// ラッシュ /// </summary>
    /// <param name="note"> noteTypeがラッシュの際のみ　</param>
    void ProcessRushHit(NotesCon note)
    {
       float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;
       float timeDiff = Mathf.Abs(note.GetTargetTime() - currentTime);
        Debug.Log("Rush Tap!");
        JudgePass(note,timeDiff);
    }

    /// <summary> /// 判定時のパス /// </summary>
    private void JudgePass(NotesCon targetNote, float caluculateTimeDiff) 
    {
        if (targetNote == null) { return; }

        Debug.Log($"[Judge] JudgePass呼び出し: timeDiff={caluculateTimeDiff:F3}, perfectWindow={perfectWindow}, greatWindow={greatWindow}");

        //判定
        if (caluculateTimeDiff <= perfectWindow)
        {
            Debug.Log($"<color=orange>{gameObject.name} 良！</color> 誤差:{caluculateTimeDiff:F3} 距離:{distance:F2}");
            _defineSO.PlayNoteSE(targetNote.GetNoteType(), true);
            targetNote.OnHit();
        }
        else if (caluculateTimeDiff <= greatWindow)
        {
            Debug.Log($"<color=yellow>{gameObject.name} 可！</color> 誤差:{caluculateTimeDiff:F3}");
            _defineSO.PlayNoteSE(targetNote.GetNoteType(), true);
            targetNote.OnHit();
        }
        else
        {
            //デバッグ用：タイミングが早すぎる・遅すぎる場合
            Debug.Log($"範囲外 誤差:{caluculateTimeDiff:F3}");
            _defineSO.PlayNoteSE(targetNote.GetNoteType(), false);
            //仮でMiss時にオブジェクト削除
            targetNote.OnMiss();

            // ゲームシステム経由でダメージ処理を行う
            if (GameSystem1.Instance != null)
            {
                GameSystem1.Instance.ApplyDamageToCharacter(misstakeDamage);
            }
            else if (_charaSO != null)
            {
                _charaSO.HPfluctuation(misstakeDamage);
            }
        }
    }

    private void MoveCharacterToNotePosition(Vector3 position)
    {
        if (_charactor != null)
        {
            _charactor.MoveToPoint(position);
        }
    }

}
