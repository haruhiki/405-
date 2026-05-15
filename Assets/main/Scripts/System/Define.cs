using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Define", menuName = "Scriptable Objects/Define")]
public class Define : ScriptableObject
{
    [Header("操作検知用")]
    public bool isInputDetected;
    public bool isInputHold;
    public bool isInputRush;

    public Vector2 inputScreenPos { get; private set; }

    [Header("操作検知時のアクション")]
    Action TouchActionEvect; //操作時の各イベントをまとめて発行させる用

    //初期化
    public void Reset()
    {
        isInputDetected = false;
        isInputHold = false;
        isInputRush = false;
        inputScreenPos = Vector2.zero;
    }


    /// <summary> /// 入力検知用  /// </summary>
    /// <param name="pos"></param>
    /// <param name="down"></param>
    /// <param name="stay"></param>
    /// <param name="up"></param>
    public void SetInput(Vector2 pos, bool down, bool stay, bool up)
    {
        inputScreenPos = pos;
        isInputDetected = down;
        isInputHold = stay;
        isInputRush = up;
    }
}
