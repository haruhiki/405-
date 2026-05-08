using UnityEngine;
using UnityEngine.InputSystem;

public class NotsSpawn : MonoBehaviour
{
    [Header("データ・設定")]
    public NotesobjSO notsSO;
    public AudioSource audioSource;
    [SerializeField] public float notesSpeed = 5.0f;   //ノーツ速度
    [SerializeField] public float preSpawnTime = 2.0f; //ノーツ生成

    [Header("prehub")]
    public GameObject shortNotes;  //短押し
    public GameObject LongNotes;   //長押し
    public GameObject RushNotes;   //連打

    [Header("生成ポイント")]
    public Transform[] spawnPoints;

    private int spawnIndex = 0;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        spawnIndex = 0; // 開始時にリセット

        if (notsSO != null)
        {
            Debug.Log($"譜面データ読み込み完了: {notsSO.notes.Count} 件のノーツがあります");
        }

        if (notsSO == null)
        {
            Debug.LogError("notsSO がインスペクターでセットされていません！");
        }
        else
        {
            Debug.Log($"現在のノーツ数: {notsSO.notes.Count} 件");
        }
    }

    void Update()
    {
        //テスト用
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (audioSource.clip != null)
            {
                spawnIndex = 0; // インデックスをリセット
                audioSource.Play();
                Debug.Log($"再生開始: {audioSource.clip.name} / 全長: {audioSource.clip.length}秒");
            }
            else
            {
                Debug.LogError("AudioSourceにClip（曲）がセットされていません！");
            }
        }

        //再背中のみノーツ生成を行う-> それ以外はretrun 
        if (!audioSource.isPlaying) return;

        float currentTime = audioSource.time;

        while (spawnIndex < notsSO.notes.Count)
        {
            float target = notsSO.notes[spawnIndex].targetTime;
            float spawnAt = target - preSpawnTime;

            Debug.Log($"[Index:{spawnIndex}] 左辺(SpawnAt):{spawnAt:F3} <= 右辺(Current):{currentTime:F3}");

            if (spawnAt <= currentTime)
            {
                Debug.Log("<color=green>条件クリア！生成します</color>");
                Spawn(notsSO.notes[spawnIndex]);
                spawnIndex++;
            }
            else
            { 
                break;
            }
        }
    }
    //ノーツ本体生成
    void Spawn(NoteDate.Notes noteDate) 
    {

        GameObject prefab = shortNotes;
        if (noteDate.noteType == NoteDate.NotesType.Long) prefab = LongNotes;
        if (noteDate.noteType == NoteDate.NotesType.Rush) prefab = RushNotes;

        //生成
        GameObject noteObj = Instantiate(prefab, spawnPoints[noteDate.lane].position, Quaternion.identity);

        //プレハブ選択
        NotesCon controller = noteObj.GetComponent<NotesCon>();
        if (controller != null)
        {
            controller.Init(noteDate, notesSpeed, preSpawnTime);
        }

    }
}
