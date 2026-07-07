using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class MusicSelectButton : MonoBehaviour
{
    private Button _button;
    private StageSelectionButton _stageSelection; // 既存の開始処理

    void Awake() {
        _button = GetComponent<Button>();
        _stageSelection = GetComponent<StageSelectionButton>();
    }

    public void Setup(StageDataSO data, bool isCenter)
    {
        // 既存のStageSelectionButtonにデータをセット（これでクリック開始処理が機能する）
        var field = typeof(StageSelectionButton).GetField("stage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(_stageSelection, data);

        // 見た目とクリック可否の制御
        _button.interactable = isCenter;
        
        transform.DOScale(isCenter ? 1.1f : 0.9f, 0.2f);
        GetComponent<Image>().DOColor(isCenter ? Color.yellow : Color.white, 0.2f);
        
        // ボタンの中のTextコンポーネントを取得して書き換える
        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = data.DisplayName; 

        // CanvasGroupをアタッチしてあれば、透過させる（よりリッチになる）
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.DOFade(isCenter ? 1.0f : 0.5f, 0.2f);
    }
}