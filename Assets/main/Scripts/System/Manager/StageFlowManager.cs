using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StageFlowManager : MonoBehaviour
{
    private static StageFlowManager _instance;
    public static StageFlowManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("StageFlowManager");
                _instance = go.AddComponent<StageFlowManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Scene Names")]
    //各シーンの名前をInspectorで設定できるようにする
    [SerializeField] private string selectSceneName = "MusicSelect";
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string resultSceneName = "Result";

    [Header("Stage Flow")]
    //ステージクリア後の遷移までの時間をInspectorで設定できるようにする
    [SerializeField] private float clearDelaySeconds = 0.2f;

    [Header("Result Overlay")]
    //ステージクリア・ゲームオーバー時のオーバーレイ表示の設定
    [SerializeField] private string resultOverlayName = "StageResultOverlay";
    [SerializeField] private Color clearColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color gameOverColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private int resultFontSize = 80;

    //ステージの選択と結果の管理
    public StageDataSO SelectedStage { get; private set; }
    public StageResultData LastResult { get; private set; }

    //ステージの進行状況の管理
    private bool _stageStarted;                 //ステージが開始されたかどうかのフラグ
    private bool _resultTransitionTriggered;    //ステージ終了後の遷移がトリガーされたかどうかのフラグ
    private float _transitionTimer;             //ステージ終了後の遷移までの時間を計測するタイマー

    //オーバーレイ表示の管理
    private GameObject _resultOverlay;
    //オーバーレイ表示のテキストコンポーネント
    private TextMeshProUGUI _resultOverlayTMP;

    //ステージがアクティブかどうかを判定するプロパティ
    public bool IsStageActive => _stageStarted && !_resultTransitionTriggered;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        //sceneLoadedイベントにOnSceneLoadedメソッドを登録
        //シーン切り替え検出を行うために必要
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        //sceneLoadedイベントからOnSceneLoadedメソッドを解除
        //後々メモリゴミになるため削除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        //ステージがアクティブでない場合は処理を行わない
        if (!_stageStarted || _resultTransitionTriggered) return;

        if (GameSystem1.Instance != null && GameSystem1.Instance.IsGameOver())
        {
            // ゲームオーバー条件を満たした場合、ステージを終了する
            FinishStage(false, "HP_ZERO");
            return;
        }

        if (AudioManager.Instance == null) return;

        float totalDuration = AudioManager.Instance.GetTotalDuration();
        if (totalDuration > 0.1f)
        {
            float currentTime = AudioManager.Instance.GetCurrentTime();
            if (currentTime >= totalDuration - 0.15f)
            {
                FinishStage(true, "SONG_FINISHED");
            }
        }
    }

    //ステージの選択
    public void SelectStage(StageDataSO stage)
    {
        SelectedStage = stage;
        LastResult = null;
        _stageStarted = false;
        _resultTransitionTriggered = false;
        _transitionTimer = 0f;

        Debug.Log($"[StageFlow] Stage selected: {stage?.DisplayName ?? "(none)"}");
    }

    public void StartSelectedStage()
    {
        if (SelectedStage == null)
        {
            Debug.LogWarning("[StageFlow] No stage is selected.");
            return;
        }

        LastResult = null;
        _stageStarted = false;
        _resultTransitionTriggered = false;
        _transitionTimer = 0f;

        SceneManager.LoadScene(gameSceneName);
    }

    //ステージ終了後にセレクトシーンに戻る
    public void ReturnToSelectScene()
    {
        //各状態をリセット
        _stageStarted = false;
        _resultTransitionTriggered = false;
        _transitionTimer = 0f;
        //セレクトシーンに戻る
        SceneManager.LoadScene(selectSceneName);
    }

    //シーンがロードされたときに呼ばれるメソッド
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameSceneName) return;
        if (SelectedStage == null) return;
        if (_stageStarted) return;

        FindOrCreateResultOverlay();
        HideResultOverlay();
        BeginStage();
    }

    //ステージ開始処理
    private void BeginStage()
    {
        //ステージ開始フラグを立てる
        _stageStarted = true;
        _resultTransitionTriggered = false;
        _transitionTimer = 0f;

        //ステージ開始時に必要なシステムを初期化
        EnsureRuntimeSystems();

        if (GameSystem1.Instance != null)
        {
            GameSystem1.Instance.ResetRuntimeCharacter();
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        if (AudioManager.Instance != null && SelectedStage != null && SelectedStage.musicData != null)
        {
            AudioManager.Instance.LoadMusicData(SelectedStage.musicData);
            AudioManager.Instance.PlayActiveMusic();
        }

        Debug.Log($"[StageFlow] Stage started: {SelectedStage?.DisplayName}");
    }

    //ステージ開始時に必要なシステムを初期化するメソッド
    private void EnsureRuntimeSystems()
    {
        if (GameSystem1.Instance == null)
        {
            new GameObject("GameSystem1").AddComponent<GameSystem1>();
        }

        if (AudioManager.Instance == null)
        {
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();
        }
    }

    //ステージ終了処理
    public void FinishStage(bool isClear, string reason)
    {
        //ステージ終了後の遷移がすでにトリガーされている場合は処理を行わない
        if (_resultTransitionTriggered) return;

        _resultTransitionTriggered = true;
        _transitionTimer = clearDelaySeconds;

        //ステージ終了時に必要なシステムを初期化
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.BGMStop();
        }

        float hpRatio = 0f;
        // GameSystem1のインスタンスが存在する場合、現在のHP割合を取得
        if (GameSystem1.Instance != null)
        {
            hpRatio = GameSystem1.Instance.GetCurrentHPPercent();
        }

        LastResult = new StageResultData(SelectedStage, isClear, reason, hpRatio);
        Debug.Log($"[StageFlow] Stage finished: {(isClear ? "Clear" : "GameOver")} - {reason}");
        ShowResultOverlay(isClear);
    }

    //ステージ終了後の遷移処理
    private void FindOrCreateResultOverlay()
    {
        if (_resultOverlay != null) return;

        _resultOverlay = GameObject.Find(resultOverlayName);
        if (_resultOverlay != null)
        {
            _resultOverlayTMP = _resultOverlay.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("StageResultCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        _resultOverlay = new GameObject(resultOverlayName);
        _resultOverlay.transform.SetParent(canvas.transform, false);
        var overlayRect = _resultOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("ResultText");
        textObject.transform.SetParent(_resultOverlay.transform, false);
        _resultOverlayTMP = textObject.AddComponent<TextMeshProUGUI>();
        _resultOverlayTMP.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _resultOverlayTMP.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _resultOverlayTMP.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _resultOverlayTMP.rectTransform.anchoredPosition = Vector2.zero;
        _resultOverlayTMP.rectTransform.sizeDelta = new Vector2(1200f, 200f);
        _resultOverlayTMP.alignment = TextAlignmentOptions.Center;
        _resultOverlayTMP.fontSize = resultFontSize;
        _resultOverlayTMP.enableWordWrapping = false;
        _resultOverlayTMP.raycastTarget = false;

        _resultOverlay.SetActive(false);
    }

    //ステージ終了後のオーバーレイ表示を行うメソッド
    private void ShowResultOverlay(bool isClear)
    {
        if (_resultOverlay == null) FindOrCreateResultOverlay();
        if (_resultOverlay == null) return;

        _resultOverlay.SetActive(true);
        if (_resultOverlayTMP != null)
        {
            _resultOverlayTMP.text = isClear ? "GAME CLEAR" : "GAME OVER";
            _resultOverlayTMP.color = isClear ? clearColor : gameOverColor;
        }
    }

    //ステージ終了後のオーバーレイ表示を非表示にするメソッド
    private void HideResultOverlay()
    {
        if (_resultOverlay != null)
        {
            _resultOverlay.SetActive(false);
            if (_resultOverlayTMP != null) _resultOverlayTMP.text = string.Empty;
        }
    }

    //ステージ終了後の遷移処理を行うメソッド
    private void LateUpdate()
    {
        if (!_resultTransitionTriggered) return;

        if (_transitionTimer > 0f)
        {
            _transitionTimer -= Time.deltaTime;
            if (_transitionTimer <= 0f)
            {
                SceneManager.LoadScene(resultSceneName);
            }
        }
    }
}

//ステージ結果データを保持するクラス
[Serializable]
public class StageResultData
{
    public StageDataSO stage;
    public bool isClear;
    public string reason;
    public float hpRatio;

    public StageResultData(StageDataSO stageData, bool clear, string resultReason, float hpPercent)
    {
        stage = stageData;
        isClear = clear;
        reason = resultReason;
        hpRatio = hpPercent;
    }
}

