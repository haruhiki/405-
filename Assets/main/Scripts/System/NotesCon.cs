using UnityEngine;

public class NotesCon : MonoBehaviour
{
    private NoteDate.Notes myData;
    private float moveDuration;
    private float startTime;
    private Vector3 spawnPos;
    private bool isInitialized = false;

    private Vector3 targetWorldPos;

    private float endTime;
    private bool isEndTimeSet = false;

    [SerializeField] private Transform trailObject;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer trailRenderer;

    // 色変化用のレンダラー
    [SerializeField] private SpriteRenderer headRend, TailRend;

    private float calculatedSpeed = 5.0f;  // 計算されたデフォルトスピード
    private float notesSpeed = 5.0f;       // 実際に使用されるスピード（倍率が適用される）
    [SerializeField] private float noteSpeedMultiplier = 1.0f; // UIから調整可能な倍率（デフォルト=1.0）
    
    private bool isHolding = false;
    private bool _isDestroyed = false;

    private void Start()
    {
        if (spriteRenderer == null) { spriteRenderer = GetComponent<SpriteRenderer>(); }

        // 帯用のレンダラーが未アタッチの場合Trailから取得を試みる
        if (trailRenderer == null && trailObject != null)
        {
            trailRenderer = trailObject.GetComponentInChildren<SpriteRenderer>();
        }
    }

    /// <summary>
    /// ノーツの初期化処理
    /// </summary>
    public void Init(NoteDate.Notes data, float duration, Vector3 realTarget)
    {
        myData = data;
        moveDuration = duration;
        targetWorldPos = realTarget; // 本物の円の座標を保存

        spawnPos = transform.position;
        startTime = myData.targetTime - moveDuration;

        // 生成位置と目的地の距離からノーツスピード計算（デフォルト値）
        float distance = Vector3.Distance(spawnPos, targetWorldPos);
        calculatedSpeed = distance / moveDuration;
        
        // 倍率を適用したスピードを算出
        notesSpeed = calculatedSpeed * noteSpeedMultiplier;

        isInitialized = true;

        if (myData.noteType != NoteDate.NotesType.Long_Start)
        {
            endTime = myData.targetTime;
            isEndTimeSet = true;
        }
        else
        {
            // 初期状態ではロングの帯の長さを0にしておく
            if (trailObject != null)
            {
                Vector3 localScale = trailObject.localScale;
                localScale.y = 0;
                trailObject.localScale = localScale;
            }
        }
    }

    void Update()
    {
        if (!isInitialized) return;
        if (AudioManager.Instance == null) return;
        if (_isDestroyed) return;

        float currentTime = AudioManager.Instance.GetCurrentTime();

        if (myData.noteType == NoteDate.NotesType.Long_Start && isEndTimeSet)
        {
            // ロングノーツ専用の移動・収縮処理
            if (isHolding)
            {
                // 長押し判定成功中
                transform.position = targetWorldPos;

                // 終了時間に向けて帯が尻から判定ラインに向かって段々縮む
                float remainingTime = endTime - currentTime;
                if (remainingTime > 0)
                {
                    float currentLength = remainingTime * notesSpeed;
                    SetTrailHeight(currentLength);
                }
                else
                {
                    SetTrailHeight(0);
                    OnHit(); // ? ここで呼ばれた後、即座に _isDestroyed でガードされるぜ！
                }
            }
            else
            {
                // 長押し前 生成位置から判定ラインに向かって通常移動
                float prgress = (currentTime - startTime) / moveDuration;
                transform.position = Vector3.LerpUnclamped(spawnPos, targetWorldPos, prgress);
            }

            if (currentTime > endTime + 0.5f)
            {
                OnMiss();
            }
        }
        else
        {
            // 通常ノーツ（ShortやRush）の移動処理
            float progress = (currentTime - startTime) / moveDuration;
            transform.position = Vector3.LerpUnclamped(spawnPos, targetWorldPos, progress);

            if (isEndTimeSet)
            {
                // 判定ラインを過ぎて 0.5秒後に消える（突き抜け演出）
                if (currentTime > myData.targetTime + 0.5f) { OnMiss(); }
            }
        }
    }

    public float GetTargetTime() => myData.targetTime;
    public float GetEndTime() => endTime;
    public int GetLane() => myData.lane;

    /// <summary> /// ノーツヒット時（成功） /// </summary>
    public void OnHit()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        Debug.Log($"<color=green>[NotesCon] OnHitを実行。オブジェクトを完全に破棄します: {gameObject.name}</color>");
        HideAllRenderers();

        // プレハブの最親（LongNotes）を取得して一網打尽に削除
        Destroy(this.gameObject);
    }

    /// <summary> /// ノーツヒットミス時（失敗） /// </summary>
    public void OnMiss()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        Debug.Log($"<color=red>[NotesCon] OnMissを実行。オブジェクトを完全に破棄します: {gameObject.name}</color>");

        // ミス時も即座に画像を非表示にして画面から消し去る
        HideAllRenderers();

        Destroy(this.gameObject);
    }

    /// <summary>
    /// プレハブに含まれるすべてのスプライトを強制非表示にする安全弁
    /// </summary>
    private void HideAllRenderers()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (trailRenderer != null)  trailRenderer.enabled = false;
        if (headRend != null)       headRend.enabled = false;
        if (TailRend != null)       TailRend.enabled = false;
    }

    public void SetEndTime(float time)
    {
        endTime = time;
        isEndTimeSet = true;
        UpdateTrailScale();
    }

    private void UpdateTrailScale()
    {
        if (trailObject == null) return;

        // ロングノーツの長さ（秒数）
        float longDuration = endTime - myData.targetTime;

        // 物理的な長さを算出して適用
        float targetHeight = longDuration * notesSpeed;
        SetTrailHeight(targetHeight);
    }

    /// <summary>
    /// 帯オブジェクトのYスケールを設定する
    /// </summary>
    private void SetTrailHeight(float height)
    {
        if (trailObject == null) { return; }
        Vector3 localScale = trailObject.localScale;
        localScale.y = height;
        trailObject.localScale = localScale;
    }

    public void SetHoldVisual(bool holding)
    {
        isHolding = holding; // フラグも更新
        
        if (spriteRenderer == null) return;
        if (holding)
        {
            spriteRenderer.color = new Color(1f, 0.92f, 0.016f, 0.6f);
        }
        else
        {
            // 通常時（デフォルトの白）に戻す
            spriteRenderer.color = Color.white;
        }

        // 帯（ボディ）の色を変化
        if (trailRenderer != null)
        {
            if (holding)
            {
                trailRenderer.color = new Color(1f, 0.92f, 0.016f, 0.6f);
            }
            else
            {
                trailRenderer.color = Color.white;
            }
        }
    }

    /// <summary> /// ノーツタイプを取得 /// </summary>
    public NoteDate.NotesType GetNoteType() => myData.noteType;

    /// <summary>
    /// ノーツスピード倍率をUIから変更
    /// </summary>
    /// <param name="multiplier">倍率（1.0=デフォルト, 1.2=20%速く, 0.8=20%遅く）</param>
    public void SetNoteSpeedMultiplier(float multiplier)
    {
        noteSpeedMultiplier = Mathf.Max(0.1f, multiplier); // 最小値は0.1に制限
        notesSpeed = calculatedSpeed * noteSpeedMultiplier;
        Debug.Log($"<color=yellow>[NotesCon] スピード倍率: {noteSpeedMultiplier:F2}x → notesSpeed: {notesSpeed:F2}</color>");
    }

    /// <summary>
    /// 現在のノーツスピード倍率を取得
    /// </summary>
    public float GetNoteSpeedMultiplier() => noteSpeedMultiplier;
}