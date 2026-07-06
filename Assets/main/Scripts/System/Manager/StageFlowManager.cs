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
    [SerializeField] private string selectSceneName = "MusicSelect";
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string resultSceneName = "Result";

    [Header("Stage Flow")]
    [SerializeField] private float clearDelaySeconds = 0.2f;

    [Header("Result Overlay")]
    [SerializeField] private string resultOverlayName = "StageResultOverlay";
    [SerializeField] private Color clearColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color gameOverColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private int resultFontSize = 80;

    public StageDataSO SelectedStage { get; private set; }
    public StageResultData LastResult { get; private set; }

    private bool _stageStarted;
    private bool _resultTransitionTriggered;
    private float _transitionTimer;

    private GameObject _resultOverlay;
    private TextMeshProUGUI _resultOverlayTMP;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!_stageStarted || _resultTransitionTriggered) return;

        if (GameSystem1.Instance != null && GameSystem1.Instance.IsGameOver())
        {
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

    public void ReturnToSelectScene()
    {
        _stageStarted = false;
        _resultTransitionTriggered = false;
        _transitionTimer = 0f;
        SceneManager.LoadScene(selectSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameSceneName) return;
        if (SelectedStage == null) return;
        if (_stageStarted) return;

        FindOrCreateResultOverlay();
        HideResultOverlay();
        BeginStage();
    }

    private void BeginStage()
    {
        _stageStarted = true;
        _resultTransitionTriggered = false;
        _transitionTimer = 0f;

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

    public void FinishStage(bool isClear, string reason)
    {
        if (_resultTransitionTriggered) return;

        _resultTransitionTriggered = true;
        _transitionTimer = clearDelaySeconds;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.BGMStop();
        }

        float hpRatio = 0f;
        if (GameSystem1.Instance != null)
        {
            hpRatio = GameSystem1.Instance.GetCurrentHPPercent();
        }

        LastResult = new StageResultData(SelectedStage, isClear, reason, hpRatio);
        Debug.Log($"[StageFlow] Stage finished: {(isClear ? "Clear" : "GameOver")} - {reason}");
        ShowResultOverlay(isClear);
    }

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

    private void HideResultOverlay()
    {
        if (_resultOverlay != null)
        {
            _resultOverlay.SetActive(false);
            if (_resultOverlayTMP != null) _resultOverlayTMP.text = string.Empty;
        }
    }

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

