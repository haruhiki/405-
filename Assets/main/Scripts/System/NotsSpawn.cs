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

    [Header("判定円の参照")]
    public Transform leftTargetCircle;  
    public Transform rightTargetCircle; 

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
        GameObject prefab = (noteDate.noteType == NoteDate.NotesType.Long) ? LongNotes :
                         (noteDate.noteType == NoteDate.NotesType.Rush) ? RushNotes : shortNotes;

        GameObject noteObj = Instantiate(prefab, spawnPoints[noteDate.lane].position, Quaternion.identity);
        NotesCon controller = noteObj.GetComponent<NotesCon>();
        if (controller != null)
        {
            // CSV上の座標(noteDate.targetPosition)ではなく、
            // シーン上の実際の円(leftTargetCircleなど)の座標を目的地として渡す
            Vector3 finalDestination = (noteDate.lane == 0) ? leftTargetCircle.position : rightTargetCircle.position;

            // NotesConのInitに目的地を引数として追加するか、内部で代入する
            controller.Init(noteDate, preSpawnTime, finalDestination);
        }
    }
}
