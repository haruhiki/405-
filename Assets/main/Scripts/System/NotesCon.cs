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

    private float notesSpeed = 5.0f;
    private bool isHolding = false;

    // ? 【追加】多重Destroyによるフリーズ・ゾンビ化を防ぐための削除フラグ
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

        // 生成位置と目的地の距離からノーツスピード計算
        float distance = Vector3.Distance(spawnPos, targetWorldPos);
        notesSpeed = distance / moveDuration;

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
        
        // ? すでに破棄フラグが立っているなら、このフレームのUpdate処理はすべてスキップ
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
        // ? 多重呼び出しガード（フレーム内連打のシャットアウト）
        if (_isDestroyed) return;
        _isDestroyed = true;

        Debug.Log($"<color=green>[NotesCon] OnHitを実行。オブジェクトを完全に破棄します: {gameObject.name}</color>");

        // ? 描画ゾンビ化対策：Destroyが完了する前に全ての画像を物理的に非表示にする
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
}