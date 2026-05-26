using UnityEngine;

public class NotesCon : MonoBehaviour
{
    private NoteDate.Notes myData;
    private float moveDuration;
    private float startTime;
    private Vector3 spawnPos;
    private bool isInitialized = false;

    private AudioSource audioSource;
    private Vector3 targetWorldPos;

    private float endTime;
    private bool isEndTimeSet = false;
    [SerializeField] private Transform trailObject;

    public void Init(NoteDate.Notes data, float duration, Vector3 realTarget)
    {
        myData = data;
        moveDuration = duration;
        targetWorldPos = realTarget; //本物の円の座標を保存
        audioSource = FindFirstObjectByType<AudioSource>();
        spawnPos = transform.position;
        startTime = myData.targetTime - moveDuration;
        isInitialized = true;

        if(myData.noteType != NoteDate.NotesType.Long_Start) 
        {
            endTime = myData.targetTime;
            isEndTimeSet = true;
        }
    }

    void Update()
    {
        if (!isInitialized || audioSource == null) return;

        float progress = (audioSource.time - startTime) / moveDuration;

        //targetWorldPos（本物の円）に向かうので、見た目と判定が一致する
        transform.position = Vector3.LerpUnclamped(spawnPos, targetWorldPos, progress);

        if (isEndTimeSet)
        {
            //判定ラインを過ぎて 0.5秒後に消える（突き抜け演出）
            if (audioSource.time > myData.targetTime + 0.5f) { OnMiss(); }
        }
       
    }

    public float GetTargetTime() => myData.targetTime;
    public float GetEndTime() => endTime;
    public int GetLane() => myData.lane;

    /// <summary> /// ノーツヒット時 /// </summary>
    public void OnHit() => Destroy(gameObject);

    //ノーツヒットミス時
    public void OnMiss()
    { 
        Debug.Log("Miss!"); 
        Destroy(gameObject); 
    }

    public  void SetEndTime(float time) 
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

    }


    /// <summary> /// ノーツタイプを取得　/// </summary>
    /// <returns></returns>
    public NoteDate.NotesType GetNoteType() => myData.noteType;


}