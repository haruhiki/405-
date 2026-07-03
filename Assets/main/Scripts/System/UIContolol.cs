using UnityEngine;

public class UIContolol : MonoBehaviour
{
    private bool isUIEnabled = false;

    [Header("参照")]
    
    [SerializeField] private GameObject _uiObject;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private Define _defineSO; 
    [SerializeField] private SceneManage _sceneManage;

    [Heder("インスペクターで設定調整")]
    [System.Serializable]
    struct SliderSettings
    {
        [SereializeField] private float minValue; 
        [SereializeField] private float maxValue;
        [SereializeField] private float defaultValue;
    }
    
    /// <summary> /// ノーツスピードスライダーの設定　/// </summary>
    [SerializeField] SliderSettings _noteSpeedSliderSettings;


    [Heder("UI用ボタン")]
    [SerializeField] private Button _button;    //楽曲選曲画面に戻るボタン
    [SerializeField] private Button _button2;   //ゲーム終了するボタン
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private Slider _noteSpeedSlider;  // ノーツスピード倍率調整用スライダー


    void Start()
    {
        //ボタンにイベントを追加する
        _button.onClick.AddListener(ReturnToScene);
        _button2.onClick.AddListener(ExitGame);

        // ノーツスピードスライダーの初期設定
        SliderDefoSet(); 
        
    }

    private void OnDestroy()
    {
        //ボタンのイベントを解除する -> メモリリークを防ぐため
        _button.onClick.RemoveListener(ReturnToScene);
        _button2.onClick.RemoveListener(ExitGame);
        
        // ノーツスピードスライダーのイベントを解除
        if (_noteSpeedSlider != null)
        {
            _noteSpeedSlider.onValueChanged.RemoveListener(AdjustNoteSpeed);
        }
    }
    // Update is called once per frame
    void Update()
    {
        //UIの表示・非表示を切り替える処理
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleUI();
            if (isUIEnabled)
            {
                //UIが表示されている場合、BGMを一時停止する
                _audioManager.PauseBGM();
                Time.timeScale = 0f; // ゲームの時間を停止
            }
            else
            {
                //UIが非表示の場合、BGMを再開する
                _audioManager.ResumeBGM();
                Time.timeScale = 1f; // ゲームの時間を再開
            }
        }


    }

   //UIの表示・非表示を切り替えるメソッド
    public void ToggleUI()
    {
        if (_uiObject != null)
        {
            isUIEnabled = !isUIEnabled;
            _uiObject.SetActive(isUIEnabled);
        }
    }

    //UIの表示状態を取得するメソッド
    public bool IsUIEnabled()
    {
        return isUIEnabled;
    }

    //AudioMangarから音量を取得するメソッド
    //スライダーで調整できるようにするために使用する
    public float GetBGMVolume()
    {
        if (_audioManager != null)
        {
            return _audioManager.bgmmasterVolume;
        }
        return 1.0f; // デフォルトの音量を返す
    }

    //SE,BGMの音量を取得するために使用する
    public float GetMasterVolume()
    {
        if (_audioManager != null)
        {
            return _audioManager.masterVolume;
        }
        return 1.0f; // デフォルトの音量を返す
    }

    //タイトル画面への遷移処理
    public void ReturnToScene()
    {
        //フラグ変更
        if (_defineSO != null)
        {
            _defineSO.isEndGame = true;
            _defineSO.isInGame = false;
            Time.timeScale = 1f; // ゲームの時間を再開
        }

        //シーン遷移処理を呼び出す
        if (_sceneManage != null)
        {
            // 1はタイトルシーンのインデックス
            _sceneManage.ChangeScene(1); 
        }
    }

    //ゲーム終了処理
    public void ExitGame()
    {
        //アプリケーションを終了する
        Application.Quit();        
    }

    //ノーツ降下スピード調整
    private void AdjustNoteSpeed(float multiplier)
    {
        // シーン内のすべてのNoteConオブジェクトを取得
        NotesCon[] allNotes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        
        if (allNotes.Length == 0)
        {
            Debug.LogWarning("[UIControl] シーン内にNoteConが見つかりません");
            return;
        }
        
        // すべてのノーツに倍率を適用
        foreach (NotesCon note in allNotes)
        {
            note.SetNoteSpeedMultiplier(multiplier);
        }
        
        Debug.Log($"<color=cyan>[UIControl] ノーツスピード倍率を {multiplier:F2}x に変更（{allNotes.Length}個のノーツに適用）</color>");
    }

    // ノーツスピードスライダーの初期設定
    private  void SliderDefoSet()
    {
        //スライダー構造体の初期値を設定
        _noteSpeedSliderSettings.minValue = 0.1f; // 最小値
        _noteSpeedSliderSettings.maxValue = 3.0f; // 最大値
        _noteSpeedSliderSettings.defaultValue = 1.0f; // デフォルト値

        // ノーツスピードスライダーのイベント設定
        if (_noteSpeedSlider != null)
        {
            // スライダーの初期値を1.0（デフォルト）に設定
            _noteSpeedSlider.minValue = _noteSpeedSliderSettings.minValue;
            _noteSpeedSlider.maxValue = _noteSpeedSliderSettings.maxValue;
            _noteSpeedSlider.value = _noteSpeedSliderSettings.defaultValue;

            _noteSpeedSlider.onValueChanged.AddListener(AdjustNoteSpeed);
        }
    }
}

