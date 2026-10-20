using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public long TotalScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public int MaxCombo { get; private set; }

    // スコアの判定に応じたスコア値を設定するための変数
    [Header("Score Values")]
    [SerializeField] private int perfectScore = 1000;
    [SerializeField] private int greatScore = 500;
    [SerializeField] private int badScore = 100;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        ResetScore();
    }

    //スコアリセット処理
    public void ResetScore()
    {
        TotalScore = 0;
        CurrentCombo = 0;
        MaxCombo = 0;
        ScoreUIController.Instance?.UpdateTotalScore(TotalScore);
        ScoreUIController.Instance?.UpdateCombo(CurrentCombo);
    }
    
    //スコア加算処理
    public void AddHitScore(int scoreAdd, Vector3 worldPos)
    {
        //スコア加算
        TotalScore += scoreAdd;
        //コンボ加算
        CurrentCombo++;
        //最大コンボ更新
        if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo;

        //スコアUIの更新表示
        ScoreUIController.Instance?.ShowHitPopup($"+{scoreAdd}", worldPos);
        ScoreUIController.Instance?.UpdateTotalScore(TotalScore);
        ScoreUIController.Instance?.UpdateCombo(CurrentCombo);
    }
    
    //ミス時の処理
    public void OnMiss()
    {
        //現コンボ数をリセット
        CurrentCombo = 0;
        ScoreUIController.Instance?.ShowComboBreak();
        ScoreUIController.Instance?.UpdateCombo(CurrentCombo);
    }

    // Helper to compute score by judgement name
    //各スコアの判定に応じたスコアを返すメソッド
    //ー＞スコア自体はUnityのインスペクター上で設定することができるのでここも変更しなくていい。
    public int ScoreForJudgement(string judgement)
    {
        switch (judgement)
        {
            case "Perfect": return perfectScore;
            case "Great": return greatScore;
            default: return badScore;
        }
    }
}
