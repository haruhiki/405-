using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneManage : MonoBehaviour
{

    //�t���O�Ǘ�SO
    Define _defineSO;
    private void Start() { _defineSO = GetComponent<Define>();  }

    public void LoadSelectScene()
    {
        SceneManager.LoadScene((int)Define.SceneState.Select);
    }

    public void LoadResultScene()
    {
        SceneManager.LoadScene((int)Define.SceneState.Result);
    }

    //�C�x���g�o�^
    private void OnEnable()
    {
        
    }

    //TODO:��� -> �C�x���g�̓o�^����(�G���[�̌����ɂȂ邩��)
    private void OnDisable()
    {
        
    }
    private void Update()
    {
        if (_defineSO != null) { return; }
        

    }

    /// <summary> /// �V�[���̐؂�ւ��Ɗe���� -> �V�[��ID�Q�� /// </summary>
    /// <param name="SceneID"></param>
    public void SceneChange(int SceneID) 
    {
        //�e�V�[������
        switch (SceneID)
        {
            case (int)Define.SceneState.Title:
                //�e�V�[�����ƂɈႤ�J�ڃA�N�V�������܂Ƃ߂�
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

    //�e�V�[���̐؂�ւ����C�x���g
    private void ChangeEvent(int SceneID) 
    {
        if(_defineSO != null) { return; }
        //�V�[���J�ڃA�j���[�V����
        

    }

    //�񓯊������p�̃V�[�����[�h
    private IEnumerator LoadAsync(int SceneID)
    {
        
        //�񓯊������p
        SceneManager.LoadSceneAsync(SceneID);

        //TODO:���[�h���̉�������̏���������


        yield return SceneID;
    }
}
