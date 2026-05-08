using UnityEngine;

public class Judge : MonoBehaviour
{
    [SerializeField] public float perfectRad = 0.5f;
    [SerializeField] public float greatRad = 1.0f;
    [SerializeField] public Define _defineSO;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = FindFirstObjectByType<AudioSource>();
    }

    void Update()
    { 
        if (_defineSO == null || !_defineSO.isInputDetected) return;

        //座標変換
        float zDepth = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 screenPos = new Vector3(_defineSO.inputScreenPos.x, _defineSO.inputScreenPos.y, zDepth);
        Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
        touchWorldPos.z = 0;

        //判定対象のノーツを探す
        NotesCon targetNote = GetActiveNote();

        if (targetNote != null)
        {
            //距離と時間の差を算出
            float distance = Vector2.Distance(touchWorldPos, targetNote.GetTargetPosition());
            float timeDiff = Mathf.Abs(targetNote.GetTargetTime() - audioSource.time);

            //判定ロジック
            //時間が 0.2秒以上ズレていたら「空振り」として無視、またはMiss
            if (timeDiff > 0.2f) return;

            if (distance <= perfectRad)
            {
                Debug.Log("<color=orange>良判定！</color>");
                targetNote.OnHit();
            }
            else if (distance <= greatRad)
            {
                Debug.Log("<color=yellow>可判定！</color>");
                targetNote.OnHit(); 
            }
            else
            {
                Debug.Log("<color=red>不可（場所が違う）</color>");
                //場所が違う場合,消すかどうか
                targetNote.OnHit();
            }
        }
    }

    NotesCon GetActiveNote()
    {
        NotesCon[] allNotes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        NotesCon nearest = null;
        float minTimeDiff = float.MaxValue;
        float songTime = audioSource.time;

        foreach (var note in allNotes)
        {
            float diff = Mathf.Abs(note.GetTargetTime() - songTime);
            //判定有効幅（前後0.2秒など）
            if (diff < minTimeDiff && diff < 0.2f)
            {
                minTimeDiff = diff;
                nearest = note;
            }
        }
        return nearest;
    }

}
