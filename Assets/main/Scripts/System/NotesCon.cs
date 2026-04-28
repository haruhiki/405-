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

        // 単純な移動（下方向に流れる例）
        // 実際には判定ラインの位置に合わせて方向ベクトルを調整します
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        // 判定ラインを通り過ぎて一定時間（例：1秒）経ったら自動削除
        // 本来は判定ロジックで消しますが、テスト用に。
        // if (Time.time > myData.targetTime + 1.0f) { Destroy(gameObject); }
    }

}
