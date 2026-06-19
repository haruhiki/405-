using System;
using System.Runtime.CompilerServices;
using UnityEngine;

//Defineクラスをインクルード
using static Define;

public class GameSystem1 : MonoBehaviour
{
    Define _defineSO; //変数まとめてる-> SO =スクリプタブルオブジェクト

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

    void Start() { _defineSO = gameObject.GetComponent<Define>(); }

    //常にゲームシステムクラスを常駐させ動かす
    public SceneState SceneState { get; private set; }

    //ゲームロジックを外部で動かすためのイベントステート
    public event Action<SceneState> gameState;


    //ゲーム開始ロジック
    private void InGameStart() 
    {
        _defineSO.isInGame = true;

    }

    //ゲーム終了ロジック
    public void OutGameEnd() 
    {
       if(_defineSO.isEndGame == true) 
       {
            //全てのフラグを初期化
            _defineSO.Reset();
       }
    }

  


}
