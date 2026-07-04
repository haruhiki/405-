using UnityEngine;
using UnityEngine.UI;

public class UIContolol : MonoBehaviour
{
private bool isUIEnabled = false;

    [Header("参照")]
    [SerializeField] private GameObject _uiObject;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private Define _defineSO; 
    [SerializeField] private SceneManage _sceneManage;
    [SerializeField] private StageFlowManager _stageFlowManager;

    /// <summary> /// ノーツスピードスライダーの設定 /// </summary>
    [System.Serializable]
    public struct SliderSettings 
    {
        [SerializeField] public float minValue; 
        [SerializeField] public float maxValue;
        [SerializeField] public float defaultValue;
    }
    
    /// <summary> /// ノーツスピードスライダーの設定 /// </summary>
    [SerializeField] private SliderSettings _noteSpeedSliderSettings;

    [Header("UI用ボタン/スライダー")]
    [SerializeField] private Button _button;         // 楽曲選曲画面に戻るボタン
    [SerializeField] private Button _button2;        // ゲーム終了するボタン
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private Slider _noteSpeedSlider; // ノーツスピード倍率調整用スライダー

    // 🚀【追加】現在のスピード倍率を保存する変数（新しく生成されるノーツ用）
    private float _currentNoteSpeedMultiplier = 1.0f;
    public float CurrentNoteSpeedMultiplier => _currentNoteSpeedMultiplier;

    void Start()
    {
        // ボタンにイベントを追加する
        if (_button != null) _button.onClick.AddListener(ReturnToScene);
        if (_button2 != null) _button2.onClick.AddListener(ExitGame);

        // ノーツスピードスライダーの初期設定
        SliderDefoSet(); 
    }

    private void OnDestroy()
    {
        // ボタンのイベントを解除する -> メモリリークを防ぐため
        if (_button != null) _button.onClick.RemoveListener(ReturnToScene);
        if (_button2 != null) _button2.onClick.RemoveListener(ExitGame);
        
        // ノーツスピードスライダーのイベントを解除
        if (_noteSpeedSlider != null)
        {
            _noteSpeedSlider.onValueChanged.RemoveListener(AdjustNoteSpeed);
        }
    }

    void Update()
    {
       // UIの表示・非表示を切り替える処理
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleUI();
            if (_audioManager == null) return;

            if (isUIEnabled)
            {
                _audioManager.BGMPause();
                Time.timeScale = 0f;  //ゲームの時間を停止
            }
            else
            {
                _audioManager.BGMauPause();
                Time.timeScale = 1f;  //ゲームの時間を再開
            }
        }
    }

    // UIの表示・非表示を切り替えるメソッド
    public void ToggleUI()
    {
        if (_uiObject != null)
        {
            isUIEnabled = !isUIEnabled;
            _uiObject.SetActive(isUIEnabled);
        }
    }

    // UIの表示状態を取得するメソッド
    public bool IsUIEnabled() => isUIEnabled;

    // AudioManagerから音量を取得するメソッド
    public float GetBGMVolume()
    {
        return _audioManager != null ? _audioManager.bgmmasterVolume : 1.0f;
    }

    public float GetMasterVolume()
    {
        return _audioManager != null ? _audioManager.masterVolume : 1.0f;
    }

    // タイトル画面への遷移処理
    public void ReturnToScene()
    {
        if (_defineSO != null)
        {
            _defineSO.isEndGame = true;
            _defineSO.isInGame = false;
            Time.timeScale = 1f; 
        }

        if (_stageFlowManager != null)
        {
            _stageFlowManager.ReturnToSelectScene();
        }
        else if (_sceneManage != null)
        {
            _sceneManage.SceneChange(1); // 1はタイトルシーンのインデックス
        }
    }

    // ゲーム終了処理
    public void ExitGame()
    {
        Application.Quit();        
    }

    // ノーツ降下スピード調整
    private void AdjustNoteSpeed(float multiplier)
    {
        _currentNoteSpeedMultiplier = multiplier;

        // シーン内のすべてのNoteConオブジェクトを取得
        NotesCon[] allNotes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        
        // すべてのノーツに倍率を即座に適用（もしあれば）
        foreach (NotesCon note in allNotes)
        {
            // 💡 NoteCon側にこのメソッドを実装して、スピードにかける設計にすると完璧だぜ！
            // note.SetNoteSpeedMultiplier(multiplier); 
        }
        
        Debug.Log($"<color=cyan>[UIControl] ノーツスピード倍率を {multiplier:F2}x に変更</color>");
    }

    // ノーツスピードスライダーの初期設定
    private void SliderDefoSet()
    {
        // ノーツスピードスライダーのイベント設定
        if (_noteSpeedSlider != null)
        {
            // インスペクターで設定した構造体の値をスライダーに同期
            _noteSpeedSlider.minValue = _noteSpeedSliderSettings.minValue;
            _noteSpeedSlider.maxValue = _noteSpeedSliderSettings.maxValue;
            _noteSpeedSlider.value = _noteSpeedSliderSettings.defaultValue;

            // 初期値を反映
            _currentNoteSpeedMultiplier = _noteSpeedSliderSettings.defaultValue;

            // リスナー登録
            _noteSpeedSlider.onValueChanged.AddListener(AdjustNoteSpeed);
        }
    }
}

