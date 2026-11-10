using UnityEngine;

public class GameSystem1 : MonoBehaviour
{
    [SerializeField] private Define _defineSO;         //ゲーム全体の定義を保持するScriptableObject
    [SerializeField] private CharactorSO _charaSO;

    public CharactorSO RuntimeCharaSO { get; private set; }

    public float CurrentHPPercent => RuntimeCharaSO != null ? RuntimeCharaSO.CurrentHPNormalized : (_charaSO != null ? _charaSO.CurrentHPNormalized : 0f);

    // ゲームが一時停止中かどうかを示すプロパティ
    #region singleton
    private static GameSystem1 _instance;
    public static GameSystem1 Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameSystem1");
                _instance = go.AddComponent<GameSystem1>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            ResetRuntimeCharacter();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    private void Start()
    {
        if (_defineSO == null)
        {
            // Define SO がインスペクターで設定されていない場合の警告
            Debug.LogWarning("[GameSystem1] Define SO is not set in the inspector.");
        }

        ResetRuntimeCharacter();
        // Subscribe to pause/resume events if PauseManager exists
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.OnPaused += HandlePaused;
            PauseManager.Instance.OnResumed += HandleResumed;
        }
        _isPaused = PauseManager.IsPaused;
    }

    private bool _isPaused = false;

    private void HandlePaused()
    {
        _isPaused = true;
        Debug.Log("[GameSystem1] Paused");
    }

    private void HandleResumed()
    {
        _isPaused = false;
        Debug.Log("[GameSystem1] Resumed");
    }

    public void ResetRuntimeCharacter()
    {
        if (_charaSO != null)
        {
            RuntimeCharaSO = Instantiate(_charaSO);
            RuntimeCharaSO.ResetStatus();
        }
        else
        {
            RuntimeCharaSO = null;
        }
    }

    // ゲーム開始時に呼び出されるメソッド
    private void InGameStart()
    {
        if (_defineSO == null) return;
        _defineSO.isInGame = true;
    }

    // ゲーム終了時に呼び出されるメソッド
    public void OutGameEnd()
    {
        if (_defineSO != null && _defineSO.isEndGame)
        {
            //_define.SOで定義されているフラグの初期化を行う
            _defineSO.Reset();
        }
    }
    
    // キャラクターにダメージを与えるメソッド
    public float ApplyDamageToCharacter(float damage)
    {
        if (RuntimeCharaSO != null)
        {
            RuntimeCharaSO.HPfluctuation(damage);
            Debug.Log($"[GameSystem1] Applied damage {damage} to runtime chara. HP now {RuntimeCharaSO.chHPpoint}");
            return RuntimeCharaSO.chHPpoint;
        }

        if (_charaSO != null)
        {
            _charaSO.HPfluctuation(damage);
            Debug.Log($"[GameSystem1] Applied damage {damage} to inspector chara. HP now {_charaSO.chHPpoint}");
            return _charaSO.chHPpoint;
        }

        return 0f;
    }

    // ゲームオーバーかどうかを判定するメソッド
    public bool IsGameOver()
    {
        if (RuntimeCharaSO != null)
        {
            return RuntimeCharaSO.chCurrentState;
        }

        return _charaSO != null && _charaSO.chCurrentState;
    }

    // 現在のHPの割合を取得するメソッド
    public float GetCurrentHPPercent()
    {
        return CurrentHPPercent;
    }

    public bool IsPaused => _isPaused || PauseManager.IsPaused;

  


}
