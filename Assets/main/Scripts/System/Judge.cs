using UnityEngine;

public class Judge : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int myLane; // 0:左, 1:右
    [SerializeField] private float judgeRadius = 1.5f;    // 判定が有効な円の半径
    [SerializeField] private float perfectWindow = 0.05f; // 良の許容時間差
    [SerializeField] private float greatWindow = 0.12f;   // 可の許容時間差

    private float timeDiff = 0;   //時間差変数
    private float distance = 0;   //距離判定変数

    [Header("参照")]
    [SerializeField] private Define _defineSO;
    private AudioSource audioSource;

    private bool isLongPress = false;

    void Start() => audioSource = FindFirstObjectByType<AudioSource>();

    void Update()
    {
        if (_defineSO == null || !_defineSO.isInputDetected) { return; }

        //座標変換
        float camToPlaneDist = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 screenPosWithDepth = new Vector3(_defineSO.inputScreenPos.x, _defineSO.inputScreenPos.y, camToPlaneDist);
        Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(screenPosWithDepth);
        touchWorldPos.z = 0;

        //距離判定
        distance = Vector2.Distance(touchWorldPos, transform.position);
        if (distance > judgeRadius) { return; }

        //このレーンのノーツを取得
        NotesCon targetNote = GetNearestNote();
        if (targetNote == null) {return; }

        //時間差（タイミング）の計算
        timeDiff = Mathf.Abs(targetNote.GetTargetTime() - audioSource.time);

        //ノーツの判定
        switch (targetNote.GetNoteType())
        {
            case NoteDate.NotesType.Short:
                if (_defineSO.isInputDetected) ProcessShortHit(targetNote);
                break;

            case NoteDate.NotesType.Long_Start:
                ProcessLongHit(targetNote);
                break;
            case NoteDate.NotesType.Long_End:
                //TODO:処理を作成
                break;

            case NoteDate.NotesType.Rush:
                ProcessRushHit(targetNote);
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
            JudgePass();
        }
      
    }

    /// <summary> /// 長押し /// </summary>
    /// <param name="note"> noteTypeがLongの場合 </param>
    void ProcessLongHit(NotesCon note)
    {
        float diff = Mathf.Abs(note.GetTargetTime() - audioSource.time);

        //押し始め
        if (_defineSO.isInputDetected && diff <= greatWindow)
        {
            isLongPress = true;
            Debug.Log("Long Start!");
            //TODO:ノーツの見た目を「押下中」に変えるなどの処理
        }

        //離した時
        if (_defineSO.isInputRush)
        {
            JudgePass();
            if (isLongPress)
            {
                Debug.Log("Long Success!");
                note.OnHit(); //ノーツ削除
            }
            isLongPress = false;
        }
    }

    /// <summary> /// ラッシュ /// </summary>
    /// <param name="note"> noteTypeがラッシュの際のみ　</param>
    void ProcessRushHit(NotesCon note)
    {
        if (_defineSO.isInputDetected)
        {
            Debug.Log("Rush Tap!");
            JudgePass();

        }
    }

    /// <summary> /// 判定時のパス /// </summary>
    private void JudgePass() 
    {
        //このレーンのノーツを取得
        NotesCon targetNote = GetNearestNote();
        if (targetNote == null) { return; }

        //判定
        if (timeDiff <= perfectWindow)
        {
            Debug.Log($"<color=orange>{gameObject.name} 良！</color> 誤差:{timeDiff:F3} 距離:{distance:F2}");
            targetNote.OnHit();
        }
        else if (timeDiff <= greatWindow)
        {
            Debug.Log($"<color=yellow>{gameObject.name} 可！</color> 誤差:{timeDiff:F3}");
            targetNote.OnHit();
        }
        else
        {
            //デバッグ用：タイミングが早すぎる・遅すぎる場合
            Debug.Log($"範囲外 誤差:{timeDiff:F3}");
            //仮でMiss時にオブジェクト削除
            targetNote.OnMiss();
        }
    }
}