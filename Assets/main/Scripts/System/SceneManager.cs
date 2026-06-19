using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SceneManager : MonoBehaviour
{

    //フラグ管理SO
    Define _defineSO;
    private void Start() { _defineSO = GetComponent<Define>();  }
    private void Update()
    {
        if (_defineSO != null) { return; }
        
        

    }

    //シーンの切り替えと各処理
    private void SceneChange(int ScneeID) 
    {
        //各シーン分岐
        switch (ScneeID)
        {
            case (int)Define.SceneState.Title:
                //各シーンごとに違う遷移アクションをまとめる
                ChangeEvent(ScneeID);
                break;
            case (int)Define.SceneState.Load:
                ChangeEvent(ScneeID);
                break;

            case (int)Define.SceneState.Select:
                ChangeEvent(ScneeID);
                break;
            case (int)Define.SceneState.Game:
                ChangeEvent(ScneeID);

                break;

            case (int)Define.SceneState.Result:
                ChangeEvent(ScneeID);
                break;
        }
    }

    //各シーンの切り替え時イベント
    private void ChangeEvent(int SceneID) 
    {
        if(_defineSO != null) { return; }
        //シーン遷移アニメーション

    }
}
