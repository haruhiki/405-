using UnityEngine;

[CreateAssetMenu(fileName = "CharactorSO", menuName = "Scriptable Objects/CharactorSO")]
public class CharactorSO : ScriptableObject
{
    //後に拡張要素を組み込むためにSOで管理
    [Header("キャラクター体力")]
    [SerializeField] public float constantCharaHP = 200.0f;
    public float chHPpoint = 200f;      //キャラの体力

    //真: キャラ終わり,偽: キャラまだ生きてる
    public bool chCurrentState = false; //キャラの現在の状態
    
    //TODO：複数キャラ選択可能にしておきたいが一旦なし。
    public enum charaSound 
    {
       None,
       ch1,
       ch2,
    }

    //キャラ情報を初期化
    public void ResetStatus()
    {
        chHPpoint = constantCharaHP;
        chCurrentState = false;
    }

    //TODO:HP変動処理-> キャラクタークラスで行うべきか？
    public void HPfluctuation(float misstakeDamage)  
    {
        chHPpoint -= misstakeDamage;
        //TODO:スライダーゲージ処理

        //HPチェックを行う
        HpPointLost();

    }

    //現在のHPがなくなったら
    private bool HpPointLost() 
    {
        if(chHPpoint <= 0) 
        {
            chCurrentState = true;
            return true;
        }
        return false;
    }

}
