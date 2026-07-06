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

    [Header("Both / Rush ノーツ用の中央基準設定")]
    [Tooltip("左判定ラインのトランスフォーム（インスペクターでアタッチするか、自動取得）")]
    [SerializeField] private Transform leftTargetTransform;
    [Tooltip("右判定ラインのトランスフォーム")]
    [SerializeField] private Transform rightTargetTransform;

    [Header("Rushノーツの停止位置調整")]
    [Tooltip("0.0〜1.0の間。1.0で完全に判定円中央。0.88にすると判定円の少し手前(88%の位置)で止まります")]
    [SerializeField] private float rushStopThreshold = 0.88f;
    
    //  --- ミス判定時のダメージ量 ---
    [SerializeField] private float misstakeDamage = 10f;

    [SerializeField] private Transform trailObject;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer trailRenderer;
    [SerializeField] private SpriteRenderer headRend, TailRend;

    private Color originalSpriteColor = Color.white;
    private Color originalTrailColor = Color.white;
    private Color originalHeadColor = Color.white;
    private Color originalTailColor = Color.white;

    private float notesSpeed = 5.0f;       
    private bool isHolding = false;
    private bool _isDestroyed = false;

    private int currentRushCount = 0;
    private bool hasStartedHolding = false;

    // Rushノーツの状態管理用フラグ
    public bool IsRushReady { get; private set; } = false;
    private bool isRushLeaving = false; 
    private float rushWaitStartTime = 0f;
    private Vector3 rushStopPos;

    private void Start()
    {
        if (spriteRenderer == null) { spriteRenderer = GetComponent<SpriteRenderer>(); }
        if (trailRenderer == null && trailObject != null)
        {
            trailRenderer = trailObject.GetComponentInChildren<SpriteRenderer>();
        }
        CaptureOriginalColors();
    }

    private void CaptureOriginalColors()
    {
        if (spriteRenderer != null) originalSpriteColor = spriteRenderer.color;
        if (trailRenderer != null) originalTrailColor = trailRenderer.color;
        if (headRend != null) originalHeadColor = headRend.color;
        if (TailRend != null) originalTailColor = TailRend.color;
    }

    public void Init(NoteDate.Notes data, float duration, Vector3 realTarget)
    {
        myData = data;
        moveDuration = duration;
        isHolding = false;
        _isDestroyed = false;
        currentRushCount = 0;
        hasStartedHolding = false;
        IsRushReady = false;
        isRushLeaving = false;
        CaptureOriginalColors();
        
        targetWorldPos = realTarget; 
        spawnPos = transform.position;

        // Both または Rush ノーツの場合は強制的に左右の中央ラインに軌道を補正する
        if (myData.noteType == NoteDate.NotesType.Both || myData.noteType == NoteDate.NotesType.Rush)
        {
            // もしインスペクターで左右のターゲットが指定されていればそれを使う
            if (leftTargetTransform != null && rightTargetTransform != null)
            {
                Vector3 centerTarget = (leftTargetTransform.position + rightTargetTransform.position) * 0.5f;
                targetWorldPos = centerTarget;
                // 湧き位置のX座標（またはZ座標など、奥からの直進方向）も中央に合わせる
                spawnPos = new Vector3(centerTarget.x, transform.position.y, transform.position.z);
                transform.position = spawnPos;
            }
        }

        startTime = myData.targetTime - moveDuration;

        // 指定の割合の手前停止位置を割り出しておく
        rushStopPos = Vector3.Lerp(spawnPos, targetWorldPos, rushStopThreshold);

        float distance = Vector3.Distance(spawnPos, targetWorldPos);
        notesSpeed = distance / duration;
        isInitialized = true;

        if (myData.noteType != NoteDate.NotesType.Long_Start)
        {
            endTime = myData.targetTime;
            isEndTimeSet = true;
        }
    }

    void Update()
    {
        //  --- ノーツの移動処理 ---
        if (!isInitialized || AudioManager.Instance == null || _isDestroyed) return;

        float currentTime = AudioManager.Instance.GetCurrentTime();

        // --- Rushノーツの特殊移動処理 ---
        if (myData.noteType == NoteDate.NotesType.Rush)
        {
            if (!IsRushReady && !isRushLeaving)
            {
                float progress = (currentTime - startTime) / (moveDuration * rushStopThreshold);
                progress = Mathf.Clamp01(progress);
                transform.position = Vector3.Lerp(spawnPos, rushStopPos, progress);

                if (progress >= 1.0f)
                {
                    IsRushReady = true;
                    rushWaitStartTime = currentTime; 
                }
            }
            else if (IsRushReady && !isRushLeaving)
            {
                transform.position = rushStopPos;

                if (currentTime > rushWaitStartTime + 3.0f)
                {
                    IsRushReady = false;
                    isRushLeaving = true;
                    startTime = currentTime; 
                }
            }
            else if (isRushLeaving)
            {
                float leaveProgress = (currentTime - startTime) / (moveDuration * (1.0f - rushStopThreshold));
                transform.position = Vector3.LerpUnclamped(rushStopPos, targetWorldPos, leaveProgress);

                if (leaveProgress >= 1.3f) 
                {
                    OnMiss();
                }
            }
            return;
        }

        // --- 通常・同時押し・ロングノーツの移動 ---
        if (myData.noteType == NoteDate.NotesType.Long_Start && isEndTimeSet)
        {
            float totalLongDuration = endTime - myData.targetTime;

            if (isHolding)
            {
                transform.position = targetWorldPos;
                float remainingTime = endTime - currentTime;
                SetTrailHeight(remainingTime > 0 ? remainingTime * notesSpeed : 0f);
                
                if (remainingTime <= 0) OnHit();
            }
            else
            {
                float progress = (currentTime - startTime) / moveDuration;
                transform.position = Vector3.LerpUnclamped(spawnPos, targetWorldPos, progress);
                SetTrailHeight(totalLongDuration * notesSpeed);
            }

            if (currentTime > endTime + 0.5f) OnMiss();
        }
        else
        {
            // 通常ノーツおよび中央を流れる「Bothノーツ」の移動
            float progress = (currentTime - startTime) / moveDuration;
            transform.position = Vector3.LerpUnclamped(spawnPos, targetWorldPos, progress);

            if (currentTime > myData.targetTime + 0.5f) OnMiss();
        }
    }

    public float GetTargetTime() => myData.targetTime;
    public float GetEndTime() => endTime;
    public int GetLane() => myData.lane;
    public NoteDate.NotesType GetNoteType() => myData.noteType;

    public void AddRushCount() => currentRushCount++;
    public int GetRushCount() => currentRushCount;

    public void OnHit()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;
        HideAllRenderers();
        Destroy(this.gameObject);
    }

    public void OnMiss()
    {
        //そのままのダメージを入れてみる ->　のちに変更するかも
        GameSystem1.Instance.ApplyDamageToCharacter(misstakeDamage);
        if (_isDestroyed) return;
        _isDestroyed = true;
        HideAllRenderers();
        Destroy(this.gameObject);
    }

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
        float longDuration = endTime - myData.targetTime;
        SetTrailHeight(longDuration * notesSpeed);
    }

    private void SetTrailHeight(float height)
    {
        if (trailObject == null) return;
        Vector3 localScale = trailObject.localScale;
        localScale.y = Mathf.Max(0f, height);
        trailObject.localScale = localScale;
    }

    public void SetHoldVisual(bool holding)
    {
        if (holding) hasStartedHolding = true;
        isHolding = holding;
        if (spriteRenderer != null) spriteRenderer.color = holding ? new Color(1f, 0.92f, 0.016f, 0.6f) : originalSpriteColor;
        if (trailRenderer != null)  trailRenderer.color  = holding ? new Color(1f, 0.92f, 0.016f, 0.6f) : originalTrailColor;
        if (headRend != null)       headRend.color       = holding ? new Color(1f, 0.92f, 0.016f, 0.6f) : originalHeadColor;
        if (TailRend != null)       TailRend.color       = holding ? new Color(1f, 0.92f, 0.016f, 0.6f) : originalTailColor;
    }
}