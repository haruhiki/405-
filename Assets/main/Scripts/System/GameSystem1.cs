using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GameSystem1 : MonoBehaviour
{
    [SerializeField] Define _defineSO;         //変数まとめてる-> SO =スクリプタブルオブジェクト
    [SerializeField] CharactorSO _charaSO;

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

    private bool isGameOver() 
    {
        //キャラクター情報管理SOもしくは変数等の管理SOがない場合はじく
        if(!_charaSO || !_defineSO) { return false; }

        if(_charaSO.chCurrentState == true) 
        {
            return true;
        }
        

        return true;
    }

  


}
