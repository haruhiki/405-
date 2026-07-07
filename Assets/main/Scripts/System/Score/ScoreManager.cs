using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public long TotalScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public int MaxCombo { get; private set; }

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

    public void ResetScore()
    {
        TotalScore = 0;
        CurrentCombo = 0;
        MaxCombo = 0;
        ScoreUIController.Instance?.UpdateTotalScore(TotalScore);
        ScoreUIController.Instance?.UpdateCombo(CurrentCombo);
    }

    public void AddHitScore(int scoreAdd, Vector3 worldPos)
    {
        TotalScore += scoreAdd;
        CurrentCombo++;
        if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo;

        ScoreUIController.Instance?.ShowHitPopup($"+{scoreAdd}", worldPos);
        ScoreUIController.Instance?.UpdateTotalScore(TotalScore);
        ScoreUIController.Instance?.UpdateCombo(CurrentCombo);
    }

    public void OnMiss()
    {
        CurrentCombo = 0;
        ScoreUIController.Instance?.ShowComboBreak();
        ScoreUIController.Instance?.UpdateCombo(CurrentCombo);
    }

    // Helper to compute score by judgement name
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
