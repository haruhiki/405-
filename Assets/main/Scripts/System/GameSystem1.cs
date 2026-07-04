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
            return RuntimeCharaSO.chHPpoint;
        }

        if (_charaSO != null)
        {
            _charaSO.HPfluctuation(damage);
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

  


}
