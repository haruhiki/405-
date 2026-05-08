using UnityEngine;

public class NotesCon : MonoBehaviour
{
    private NoteDate.Notes myData;
    private float speed;
    private float spawnOffset;
    private bool isInitialized = false;

    private Vector3 spawnPos;   //生成時の位置
    private float spawnTime;    //生成されるべきだった時間

    public void Init(NoteDate.Notes data, float moveSpeed, float offset)
    {
        myData = data;
        speed = moveSpeed;
        spawnOffset = offset;

        //生成時の座標と時間を記録
        spawnPos = transform.position;
        spawnTime = myData.targetTime - spawnOffset;

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;
        float songTime = FindFirstObjectByType<AudioSource>().time;
        float progress = (songTime - spawnTime) / spawnOffset;
        transform.position = Vector3.Lerp(spawnPos, myData.targetPosition, progress);
        if (songTime > myData.targetTime + 0.5f)
        {
            OnMiss();
        }
    }

    public Vector3 GetTargetPosition() => myData.targetPosition;
    public float GetTargetTime() => myData.targetTime;

    public void OnHit()
    {
        //TODO:エフェクト生成
        Destroy(gameObject);
    }

    //miss時の処理
    private void OnMiss()
    {
        Debug.Log("Miss...");
        Destroy(gameObject);
    }


}
