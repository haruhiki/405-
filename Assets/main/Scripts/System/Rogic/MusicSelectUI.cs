using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class MusicSelectUI : MonoBehaviour
{
    [SerializeField] private List<StageDataSO> stageList;
    [SerializeField] private List<MusicSelectButton> buttonList; // 3つだけ登録（上・中・下）
    [SerializeField] private Image jacketImage;
    [SerializeField] private TextMeshProUGUI titleText;

    /// 現在選択されている曲のインデックス
    private int _currentIndex = 0;
    
    /// <summary> /// 初期化処理
    private void Start() => RefreshUI();

    /// <summary> /// 上ボタンが押されたときの処理
    public void OnClickUp()
    {
        _currentIndex = (_currentIndex - 1 + stageList.Count) % stageList.Count;
        RefreshUI();
    }

    /// <summary> /// 下ボタンが押されたときの処理
    public void OnClickDown() 
    {
        _currentIndex = (_currentIndex + 1) % stageList.Count;
        RefreshUI();
    }

    private void RefreshUI()
    {
        // 3つのボタンそれぞれに対してデータを割り当て
        for (int i = 0; i < buttonList.Count; i++)
        {
            // i=0(上), i=1(中), i=2(下) となるようにインデックスを計算
            int offset = i - 1;
            int targetIndex = (_currentIndex + offset + stageList.Count) % stageList.Count;
            
            // ボタンにデータを更新（StageSelectionButtonも更新）
            buttonList[i].Setup(stageList[targetIndex], (i == 1));
        }

        // 中央(i=1)の曲情報を反映
        StageDataSO currentData = stageList[_currentIndex];
        StageFlowManager.Instance.SelectStage(currentData);
        
        // ジャケット画像を更新（フェードイン・フェードアウト）
        jacketImage.DOFade(0f, 0.1f).OnComplete(() => {
            jacketImage.sprite = currentData.backgroundSprite;
            jacketImage.DOFade(1f, 0.1f);
        });
        
        // 曲名を更新
        if (titleText != null) titleText.text = currentData.DisplayName;
    }
}