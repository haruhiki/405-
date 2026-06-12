using UnityEngine;
using UnityEngine.InputSystem;

public class NotsSpawn : MonoBehaviour
{
    public AudioDataSO audioDataSO;

    [Header("設定")]
    [SerializeField] public float notesSpeed = 5.0f;   //ノーツ速度
    [SerializeField] public float preSpawnTime = 2.0f; //ノーツ生成時間

    [Header("プレハブアセット（Projectビューの物を割り当てる）")]
    public GameObject shortNotes;  //短押し用
    public GameObject LongNotes;   //長押し用
    public GameObject RushNotes;   //連打用

    [Header("生成ポイント")]
    public Transform[] spawnPoints;

    [Header("判定円の参照")]
    public Transform leftTargetCircle;
    public Transform rightTargetCircle;

    [Header("エディタ同期設定")]
    [Tooltip("オンにすると再生中に自動的にノーツを流します。作成中などで自動生成してほしくない時はオフにしてください。")]
    [SerializeField] private bool autoSpawnEnabled = true;

    // 内部で安全に使うための隠し参照
    private NotesobjSO notsSO;
    private int spawnIndex = 0;
    private NotesCon[] activeNoteCon = new NotesCon[2];

    void Start()
    {
        spawnIndex = 0; // 開始時にリセット
        if (audioDataSO != null && audioDataSO.notesobjSO != null)
        {
            notsSO = audioDataSO.notesobjSO;
            Debug.Log($"譜面データ読み込み完了: {notsSO.notes.Count} 件のノーツがあります");
        }
        else
        {
            Debug.LogWarning("audioDataSO、またはその中の notsSO がセットされていません！");
        }
    }

    void Update()
    {
        if (!autoSpawnEnabled) return;
        if (notsSO == null) return; // 安全対策

        if (AudioManager.Instance == null) return;
        if (!AudioManager.Instance.IsBGMPlaying()) return;

        float currentTime = AudioManager.Instance.GetCurrentTime();

        while (spawnIndex < notsSO.notes.Count)
        {
            float target = notsSO.notes[spawnIndex].targetTime;
            float spawnAt = target - preSpawnTime;

            if (spawnAt <= currentTime)
            {
                Spawn(notsSO.notes[spawnIndex]);
                spawnIndex++;
            }
            else
            {
                break;
            }
        }
    }

    public void ResetSpawnIndexToTime(float time)
    {
        if (notsSO == null) return;

        spawnIndex = 0;
        while (spawnIndex < notsSO.notes.Count)
        {
            float target = notsSO.notes[spawnIndex].targetTime;
            float spawnAt = target - preSpawnTime;

            if (spawnAt > time)
            {
                break;
            }
            spawnIndex++;
        }

        NotesCon[] activeInScene = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        foreach (var note in activeInScene)
        {
            if (note.enabled)
            {
                Destroy(note.gameObject);
            }
        }

        System.Array.Clear(activeNoteCon, 0, activeNoteCon.Length);
        Debug.Log($"【生成インデックス同期】変更時間: {time:F3}s | 再開インデックス: {spawnIndex}");
    }

    void Spawn(NoteDate.Notes noteDate)
    {
        if (noteDate.noteType == NoteDate.NotesType.Long_End)
        {
            if (activeNoteCon[noteDate.lane] != null)
            {
                activeNoteCon[noteDate.lane].SetEndTime(noteDate.targetTime);
                activeNoteCon[noteDate.lane] = null;
            }
            return;
        }

        GameObject prefab = (noteDate.noteType == NoteDate.NotesType.Long_Start) ? LongNotes :
                         (noteDate.noteType == NoteDate.NotesType.Rush) ? RushNotes : shortNotes;

        GameObject noteObj = Instantiate(prefab, spawnPoints[noteDate.lane].position, Quaternion.identity);
        NotesCon controller = noteObj.GetComponent<NotesCon>();
        if (controller != null)
        {
            Vector3 finalDestination = (noteDate.lane == 0) ? leftTargetCircle.position : rightTargetCircle.position;
            controller.Init(noteDate, preSpawnTime, finalDestination);

            if (noteDate.noteType == NoteDate.NotesType.Long_Start)
            {
                activeNoteCon[noteDate.lane] = controller;
            }
        }
    }
}
