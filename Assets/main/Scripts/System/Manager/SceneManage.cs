using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneManage : MonoBehaviour
{

    /// <summary> /// シーン遷移を管理するクラス /// </summary>
    [SerializeField] private Define _defineSO;

    public void LoadSelectScene()
    {
        SceneManager.LoadScene((int)Define.SceneState.Select);
    }

    public void LoadResultScene()
    {
        SceneManager.LoadScene((int)Define.SceneState.Result);
    }

    /// <summary> /// シーン遷移を管理するクラス /// </summary>
    private void OnEnable()
    {
        
    }

    /// <summary> /// シーン遷移を管理するクラス /// </summary>
    private void OnDisable()
    {
        
    }
    private void Update()
    {
        if (_defineSO != null) { return; }
        

    }

    /// <summary> /// シーンを変更する /// </summary>
    /// <param name="SceneID"></param>
    public void SceneChange(int SceneID) 
    {
        // e V [      
        switch (SceneID)
        {
            case (int)Define.SceneState.Title:
                // e V [     ƂɈႤ J ڃA N V       ܂Ƃ߂ 
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

    /// <summary> /// シーン遷移時のイベントを変更する /// </summary>
    private void ChangeEvent(int SceneID) 
    {
        if(_defineSO != null) { return; }
        //TODO:ここでシーン遷移時のイベントを変更する処理を追加する
    }

    /// <summary> /// 非同期でシーンをロードする /// </summary>
    private IEnumerator LoadAsync(int SceneID)
    {
        SceneManager.LoadSceneAsync(SceneID);

        //TODO:ここで非同期ロード中の処理を追加することができる


        yield return SceneID;
    }
}
