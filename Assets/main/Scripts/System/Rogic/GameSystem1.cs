using UnityEngine;

public class GameSystem1 : MonoBehaviour
{
    [SerializeField] private Define _defineSO;         // �ϐ��܂Ƃ߂Ă� -> SO = �X�N���v�^�u���I�u�W�F�N�g
    [SerializeField] private CharactorSO _charaSO;

    public CharactorSO RuntimeCharaSO { get; private set; }

    public float CurrentHPPercent => RuntimeCharaSO != null ? RuntimeCharaSO.CurrentHPNormalized : (_charaSO != null ? _charaSO.CurrentHPNormalized : 0f);

    //�V���O���g���ŃV�X�e�����̒P���ۏ�
    //�^�C�g�����烊�U���g(�G���h)�܂ŃV�[�����ɂ����Ƃ�
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
            Debug.LogWarning("[GameSystem1] Define SO �� Inspector �Őݒ肳��Ă��܂���B");
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

    // �Q�[���J�n���W�b�N
    private void InGameStart()
    {
        if (_defineSO == null) return;
        _defineSO.isInGame = true;
    }

    // �Q�[���I�����W�b�N
    public void OutGameEnd()
    {
        if (_defineSO != null && _defineSO.isEndGame)
        {
            // �S�Ẵt���O��������
            _defineSO.Reset();
        }
    }

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

    public bool IsGameOver()
    {
        if (RuntimeCharaSO != null)
        {
            return RuntimeCharaSO.chCurrentState;
        }

        return _charaSO != null && _charaSO.chCurrentState;
    }

    public float GetCurrentHPPercent()
    {
        return CurrentHPPercent;
    }

    public bool IsPaused => _isPaused || PauseManager.IsPaused;

  


}
