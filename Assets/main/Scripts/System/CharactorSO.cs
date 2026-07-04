using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharactorSO", menuName = "Scriptable Objects/CharactorSO")]
public class CharactorSO : ScriptableObject
{
    // 後に拡張要素を組み込むためにSOで管理
    [Header("キャラクター体力")]
    public float constantCharaHP = 200.0f;

    [HideInInspector] public float chHPpoint = 200f;      // キャラの体力
    [HideInInspector] public bool chCurrentState = false; // キャラの現在の状態

    public event Action<float> OnHPChanged;

    public float CurrentHP => chHPpoint;
    public float CurrentHPNormalized => constantCharaHP > 0f ? chHPpoint / constantCharaHP : 0f;

    //TODO：複数キャラ選択可能にしておきたいが一旦なし。
    public enum charaSound
    {
        None,
        ch1,
        ch2,
    }

    // キャラ情報を初期化
    public void ResetStatus()
    {
        chHPpoint = constantCharaHP;
        chCurrentState = false;
        NotifyHPChanged();
    }

    public void HPfluctuation(float misstakeDamage)
    {
        chHPpoint = Mathf.Max(0f, chHPpoint - misstakeDamage);
        if (chHPpoint <= 0f)
        {
            chCurrentState = true;
        }
        NotifyHPChanged();
    }

    public void Heal(float amount)
    {
        chHPpoint = Mathf.Clamp(chHPpoint + amount, 0f, constantCharaHP);
        if (chHPpoint > 0f)
        {
            chCurrentState = false;
        }
        NotifyHPChanged();
    }

    private void NotifyHPChanged()
    {
        OnHPChanged?.Invoke(CurrentHPNormalized);
    }
}
