using UnityEngine;

public class NotesCon : MonoBehaviour
{
    private NoteDate.Notes myData;
    private float speed;
    private float spawnOffset;
    private bool isInitialized = false;
    public void Init(NoteDate.Notes data, float moveSpeed, float offset)
    {
        myData = data;
        speed = moveSpeed;
        spawnOffset = offset;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;
        transform.Translate(Vector3.back * speed * Time.deltaTime);
        //テスト用
        if (Time.time > myData.targetTime + 5.0f) { Destroy(gameObject); }
    }

}
