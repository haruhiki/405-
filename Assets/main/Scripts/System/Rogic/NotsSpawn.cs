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

        // 【修正】プレハブの決定に Both を追加
        GameObject prefab = shortNotes;
        if (noteDate.noteType == NoteDate.NotesType.Long_Start) prefab = LongNotes;
        else if (noteDate.noteType == NoteDate.NotesType.Rush) prefab = RushNotes;
        else if (noteDate.noteType == NoteDate.NotesType.Both) prefab = bothNotes != null ? bothNotes : shortNotes; // 未設定ならショートで代用

        // 【修正】生成初期位置の割り出し。BothとRushは中央を基準にする
        Vector3 spawnPosition = spawnPoints[noteDate.lane].position;
        Vector3 finalDestination = (noteDate.lane == 0) ? leftTargetCircle.position : rightTargetCircle.position;

        if (noteDate.noteType == NoteDate.NotesType.Both || noteDate.noteType == NoteDate.NotesType.Rush)
        {
            Vector3 centerTarget = (leftTargetCircle.position + rightTargetCircle.position) * 0.5f;
            finalDestination = centerTarget;
            spawnPosition = (spawnPoints[0].position + spawnPoints[1].position) * 0.5f;
        }

        GameObject noteObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        NotesCon controller = noteObj.GetComponent<NotesCon>();
        if (controller != null)
        {
            // NotesCon内部でも中央に軌道補正するロジックと重複しても安全なように同期
            controller.Init(noteDate, preSpawnTime, finalDestination);

            if (noteDate.noteType == NoteDate.NotesType.Long_Start)
            {
                activeNoteCon[noteDate.lane] = controller;
            }
        }
    }
}