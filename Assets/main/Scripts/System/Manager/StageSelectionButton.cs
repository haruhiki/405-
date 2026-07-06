using UnityEngine;

public class StageSelectionButton : MonoBehaviour
{
    [Header("Selected Stage")]
    [SerializeField] private StageDataSO stage;

    public void SelectAndStart()
    {
        if (stage == null)
        {
            Debug.LogWarning("[StageSelectionButton] No stage is assigned.");
            return;
        }

        StageFlowManager flow = StageFlowManager.Instance;
        flow.SelectStage(stage);
        flow.StartSelectedStage();
    }

    public void SelectOnly()
    {
        if (stage == null)
        {
            Debug.LogWarning("[StageSelectionButton] No stage is assigned.");
            return;
        }

        StageFlowManager.Instance.SelectStage(stage);
    }
}
