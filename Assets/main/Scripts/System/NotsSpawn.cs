using UnityEngine;

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
        audioSource.GetComponent<AudioSource>();
    }

    void Update()
    {
        if(notsSO == null || !audioSource.isPlaying) { return; }
        //現在の曲再生時間取得
        float currentTime = audioSource.time;
        
        //生成すべきタイミングをすぎたノーツの有無チェック
        while (spawnIndex < notsSO.notes.Count &&
            notsSO.notes[spawnIndex].targetTime - preSpawnTime <= currentTime)
        {
            Spawn(notsSO.notes[spawnIndex]);
            spawnIndex++;
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
        //NoteController controller = noteObj.GetComponent<NoteController>();
        //if (controller != null)
        //{
        //    controller.Init(noteData, notesSpeed, preSpawnTime);
        //}

    }
}
