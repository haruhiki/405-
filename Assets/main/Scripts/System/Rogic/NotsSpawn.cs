using UnityEngine;
using UnityEngine.InputSystem;

public class NotsSpawn : MonoBehaviour
{
    public AudioDataSO audioDataSO;

    [Header("設定")]
    [SerializeField] public float notesSpeed = 5.0f;   
    [SerializeField] public float preSpawnTime = 2.0f; 

    [Header("プレハブアセット")]
    public GameObject shortNotes;  
    public GameObject LongNotes;   
    public GameObject RushNotes;   
    [SerializeField] private GameObject bothNotes; // 【追加】同時押し（Both）用のプレハブ

    [Header("生成ポイント")]
    public Transform[] spawnPoints;
    public Transform bothSpawnPoint; // 【追加】同時押し（Both）用の生成ポイント
    public Transform bothTargetCircle; // 【追加】同時押し（Both）用の判定円の中央ポイント

    [Header("判定円の参照")]
    public Transform leftTargetCircle;
    public Transform rightTargetCircle;

    [Header("エディタ同期設定")]
    [Tooltip("オンにすると再生中に自動的にノーツを流します。")]
    [SerializeField] private bool autoSpawnEnabled = true;

    private NotesobjSO notsSO;
    private int spawnIndex = 0;
    private NotesCon[] activeNoteCon = new NotesCon[2];

    void Start()
    {
        RefreshChartData();
    }

    public void RefreshChartData()
    {
        spawnIndex = 0; 

        if (audioDataSO != null && audioDataSO.notesobjSO != null)
        {
            notsSO = audioDataSO.notesobjSO;
            Debug.Log($"<color=lime>【NotsSpawn同期】</color> 譜面データを最新に更新したぜ！ 合計: {notsSO.notes.Count} 件");
        }
        else
        {
            Debug.LogWarning("NotsSpawn: audioDataSO、またはその中の notsSO がセットされていません！");
        }
    }

    void Update()
    {
        if (!autoSpawnEnabled || notsSO == null || AudioManager.Instance == null || !AudioManager.Instance.IsBGMPlaying()) return;

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

            if (spawnAt > time) break;
            spawnIndex++;
        }

        NotesCon[] activeInScene = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        foreach (var note in activeInScene)
        {
            if (note.enabled) Destroy(note.gameObject);
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

    // プレハブ決定
    GameObject prefab = (noteDate.noteType == NoteDate.NotesType.Long_Start) ? LongNotes :
                        (noteDate.noteType == NoteDate.NotesType.Rush) ? RushNotes : 
                        (noteDate.noteType == NoteDate.NotesType.Both) ? bothNotes : // 追記が必要
                        shortNotes;

    // 座標決定ロジック：BothとRushは中央ポイントを使う
    Vector3 spawnPos;
    Vector3 targetPos;

    if (noteDate.noteType == NoteDate.NotesType.Both || noteDate.noteType == NoteDate.NotesType.Rush)
    {
        spawnPos = (bothSpawnPoint != null) ? bothSpawnPoint.position : (spawnPoints[0].position + spawnPoints[1].position) * 0.5f;
        targetPos = (bothTargetCircle != null) ? bothTargetCircle.position : (leftTargetCircle.position + rightTargetCircle.position) * 0.5f;
    }
    else
    {
        spawnPos = spawnPoints[noteDate.lane].position;
        targetPos = (noteDate.lane == 0) ? leftTargetCircle.position : rightTargetCircle.position;
    }

    GameObject noteObj = Instantiate(prefab, spawnPos, Quaternion.identity);
    NotesCon controller = noteObj.GetComponent<NotesCon>();
    
    if (controller != null)
    {
        controller.Init(noteDate, preSpawnTime, targetPos);

        if (noteDate.noteType == NoteDate.NotesType.Long_Start)
        {
            activeNoteCon[noteDate.lane] = controller;
        }
    }
}
}