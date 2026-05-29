using System.Transactions;
using UnityEngine;

public class Judge : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int myLane; // 0:左, 1:右
    [SerializeField] private float judgeRadius = 1.5f;    // 判定が有効な円の半径
    [SerializeField] private float perfectWindow = 0.05f; // 良の許容時間差
    [SerializeField] private float greatWindow = 0.12f;   // 可の許容時間差

    private float distance = 0;   //距離判定変数

    [Header("参照")]
    [SerializeField] private Define _defineSO;
    private AudioSource audioSource;

    private bool isLongPress = false;
    private NotesCon currentLongNote = null;

    void Start() => audioSource = FindFirstObjectByType<AudioSource>();

    void Update()
    {
        if(_defineSO == null) { return; }

        bool hasInput = _defineSO.isInputDetected || _defineSO.isInputHold || _defineSO.isInputRush;
        if(!hasInput) { return; }

        //座標変換
        float camToPlaneDist = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 screenPosWithDepth = new Vector3(_defineSO.inputScreenPos.x, _defineSO.inputScreenPos.y, camToPlaneDist);
        Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(screenPosWithDepth);
        touchWorldPos.z = 0;

        //距離判定
        distance = Vector2.Distance(touchWorldPos, transform.position);
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
        if (targetNote == null) {return; }

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
        NotesCon best = null;
        float minDiff = 0.5f; // 0.5秒以上離れているものは対象外

        foreach (var n in notes)
        {
            if (n.GetLane() != myLane) continue;
            float diff = Mathf.Abs(n.GetTargetTime() - audioSource.time);
            if (diff < minDiff)
            {
                minDiff = diff;
                best = n;
            }
        }
        return best;
    }

    /// <summary> /// 短押し /// </summary>
    /// <param name="note"> ノーツタイプがshort</param>
    void ProcessShortHit(NotesCon note)
    {
        float diff = Mathf.Abs(note.GetTargetTime() - audioSource.time);
        if (diff <= greatWindow)
        {
            // 成功なら消す
            //判定パス
            JudgePass(note,diff);
        }
      
    }

    /// <summary> /// 長押し /// </summary>
    /// <param name="note"> noteTypeがLongの場合 </param>
    void ProcessLongHit(NotesCon note)
    {
        // 押し始めの判定（キーが新しく押された瞬間）
        if (_defineSO.isInputDetected && !isLongPress)
        {
            float timeDiff = Mathf.Abs(note.GetTargetTime() - audioSource.time);

            if (timeDiff <= greatWindow)
            {
                isLongPress = true;
                currentLongNote = note;
                note.SetHoldVisual(true);
                Debug.Log("<color=cyan>【長押し開始】ホールド中...</color>");
            }
            return;
        }

        // 途中で指を離してしまった場合のペナルティ（本物の長押しには必須！）
        // 押し始めに成功しているのに、入力システムが「押しっぱなし(isInputHold)」を検知しなくなった場合
        if (isLongPress && !_defineSO.isInputHold && !_defineSO.isInputRush)
        {
            // 終了時間より圧倒的に手前で離したならコンボが切れて Miss になる
            float timeDiff = Mathf.Abs(note.GetEndTime() - audioSource.time);
            if (timeDiff > greatWindow)
            {
                Debug.Log("<color=red>【長押し失敗】途中で指が離れました！</color>");
                note.SetHoldVisual(false);
                note.OnMiss(); // ノーツ消滅（失敗）
                isLongPress = false;
                currentLongNote = null;

                return;
            }
        }

         //離した時の判定（タイミングよく指を離した瞬間）
        if (_defineSO.isInputRush && isLongPress)
        {
            float timeDiff = Mathf.Abs(note.GetEndTime() - audioSource.time);
            Debug.Log($"【長押し完了】離し誤差: {timeDiff:F3}");
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
       float timeDiff = Mathf.Abs(note.GetTargetTime() - audioSource.time);
        Debug.Log("Rush Tap!");
        JudgePass(note,timeDiff);
    }

    /// <summary> /// 判定時のパス /// </summary>
    private void JudgePass(NotesCon targetNote, float caluculateTimeDiff) 
    {
        if (targetNote == null) { return; }

        //判定
        if (caluculateTimeDiff <= perfectWindow)
        {
            Debug.Log($"<color=orange>{gameObject.name} 良！</color> 誤差:{caluculateTimeDiff:F3} 距離:{distance:F2}");
            targetNote.OnHit();
        }
        else if (caluculateTimeDiff <= greatWindow)
        {
            Debug.Log($"<color=yellow>{gameObject.name} 可！</color> 誤差:{caluculateTimeDiff:F3}");
            targetNote.OnHit();
        }
        else
        {
            //デバッグ用：タイミングが早すぎる・遅すぎる場合
            Debug.Log($"範囲外 誤差:{caluculateTimeDiff:F3}");
            //仮でMiss時にオブジェクト削除
            targetNote.OnMiss();
        }
    }
}