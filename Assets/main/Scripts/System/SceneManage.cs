using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneManage : MonoBehaviour
{

    //フラグ管理SO
    Define _defineSO;
    private void Start() { _defineSO = GetComponent<Define>();  }

    //イベント登録
    private void OnEnable()
    {
        
    }

    //TODO:絶対 -> イベントの登録解除(エラーの原因になるから)
    private void OnDisable()
    {
        
    }
    private void Update()
    {
        if (_defineSO != null) { return; }
        

    }

    /// <summary> /// シーンの切り替えと各処理 -> シーンID参照 /// </summary>
    /// <param name="SceneID"></param>
    public void SceneChange(int SceneID) 
    {
        //各シーン分岐
        switch (SceneID)
        {
            case (int)Define.SceneState.Title:
                //各シーンごとに違う遷移アクションをまとめる
                ChangeEvent(SceneID);
                SceneManager.LoadScene(SceneID);
                break;
            case (int)Define.SceneState.Load:
                ChangeEvent(SceneID);
                StartCoroutine(LoadAsync(SceneID));

                break;

            case (int)Define.SceneState.Select:
                ChangeEvent(SceneID);
                SceneManager.LoadScene(SceneID);
                break;
            case (int)Define.SceneState.Game:
                ChangeEvent(SceneID);
                SceneManager.LoadScene(SceneID);

                break;

            case (int)Define.SceneState.Result:
                ChangeEvent(SceneID);
                SceneManager.LoadScene(SceneID);
                break;
        }
    }

    //各シーンの切り替え時イベント
    private void ChangeEvent(int SceneID) 
    {
        if(_defineSO != null) { return; }
        //シーン遷移アニメーション
        

    }

    //非同期処理用のシーンロード
    private IEnumerator LoadAsync(int SceneID)
    {
        
        //非同期処理用
        SceneManager.LoadSceneAsync(SceneID);

        //TODO:ロード時の何かしらの処理を書く


        yield return SceneID;
    }
}
