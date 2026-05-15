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

    public void Init(NoteDate.Notes data, float duration, Vector3 realTarget)
    {
        myData = data;
        moveDuration = duration;
        targetWorldPos = realTarget; //本物の円の座標を保存
        audioSource = FindFirstObjectByType<AudioSource>();
        spawnPos = transform.position;
        startTime = myData.targetTime - moveDuration;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || audioSource == null) return;

        float progress = (audioSource.time - startTime) / moveDuration;

        //targetWorldPos（本物の円）に向かうので、見た目と判定が一致する
        transform.position = Vector3.LerpUnclamped(spawnPos, targetWorldPos, progress);

        //判定ラインを過ぎて 0.5秒後に消える（突き抜け演出）
        if (audioSource.time > myData.targetTime + 0.5f) { Destroy(gameObject); }
    }

    public float GetTargetTime() => myData.targetTime;
    public int GetLane() => myData.lane;
    public void OnHit() => Destroy(gameObject);
    public void OnMiss() { Debug.Log("Miss!"); Destroy(gameObject); }

    public NoteDate.NotesType GetNoteType() => myData.noteType;


}