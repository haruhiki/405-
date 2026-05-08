using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Define", menuName = "Scriptable Objects/Define")]
public class Define : ScriptableObject
{
    [Header("操作検知用")]
    public bool isInputDetected;
    public Vector2 inputScreenPos { get; private set; }


    [Header("操作検知時のアクション")]
    Action TouchActionEvect; //操作時の各イベントをまとめて発行させる用

    //初期化
    public void Reset()
    {
        isInputDetected = false;
        inputScreenPos = Vector2.zero;
    }

    //入力監視役
    public void SetInput(Vector2 pos)
    {
        isInputDetected = true;
        inputScreenPos = pos;
    }
}
