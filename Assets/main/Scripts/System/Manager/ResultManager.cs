using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button selectButton;

    [Header("References")]
    [SerializeField] private StageFlowManager stageFlowManager;

    private void Start()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryStage);
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(BackToSelect);
        }

        RefreshResult();
    }

    private void OnDestroy()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryStage);
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(BackToSelect);
        }
    }

    private void RefreshResult()
    {
        StageFlowManager flow = stageFlowManager != null ? stageFlowManager : StageFlowManager.Instance;
        if (flow == null || flow.LastResult == null)
        {
            if (titleText != null) titleText.text = "RESULT";
            if (resultText != null) resultText.text = "No result data.";
            if (hpText != null) hpText.text = "HP: 0%";
            return;
        }

        StageResultData result = flow.LastResult;
        if (titleText != null) titleText.text = result.isClear ? "CLEAR" : "GAME OVER";
        if (resultText != null) resultText.text = result.stage != null ? result.stage.DisplayMusicTitle : "Stage";
        if (hpText != null) hpText.text = $"HP: {(int)(result.hpRatio * 100f)}%";
    }

    private void RetryStage()
    {
        StageFlowManager flow = stageFlowManager != null ? stageFlowManager : StageFlowManager.Instance;
        if (flow != null)
        {
            flow.StartSelectedStage();
        }
    }

    private void BackToSelect()
    {
        StageFlowManager flow = stageFlowManager != null ? stageFlowManager : StageFlowManager.Instance;
        if (flow != null)
        {
            flow.ReturnToSelectScene();
        }
    }
}
