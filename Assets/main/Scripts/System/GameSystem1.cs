using UnityEngine;

public class GameSystem1 : MonoBehaviour
{
    [SerializeField] private Define _defineSO;         // 変数まとめてる -> SO = スクリプタブルオブジェクト
    [SerializeField] private CharactorSO _charaSO;

    public CharactorSO RuntimeCharaSO { get; private set; }

    //シングルトンでシステム内の単一を保証
    //タイトルからリザルト(エンド)までシーン内においとく
    #region singleton
    public static GameSystem1 Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
            Debug.LogWarning("[GameSystem1] Define SO が Inspector で設定されていません。");
        }

        if (_charaSO != null)
        {
            RuntimeCharaSO = Instantiate(_charaSO);
            RuntimeCharaSO.ResetStatus();
        }
    }

    // ゲーム開始ロジック
    private void InGameStart()
    {
        if (_defineSO == null) return;
        _defineSO.isInGame = true;
    }

    // ゲーム終了ロジック
    public void OutGameEnd()
    {
        if (_defineSO != null && _defineSO.isEndGame)
        {
            // 全てのフラグを初期化
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

  


}
