using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "Define", menuName = "Scriptable Objects/Define")]
public class Define : ScriptableObject
{
    //各シーンを適当に洗い出しておく
    //各シーンのステート管理
    public enum SceneState
    {
        Title,
        Load,
        Select,
        Game,
        Result,
    }


    [Header("ゲームロジック管理用")]
    public bool isInGame;
    public bool isSlideAnim;
    public bool isEndGame;


    [Header("操作検知用")]
    public bool isInputDetected;
    public bool isInputHold;
    public bool isInputRush;

    public bool isRightKey;
    public bool isLeftKey;

    public Vector2 inputScreenPos;

    [Header("操作検知時のアクション")]
    public Action TouchActionEvect; //操作時の各イベントをまとめて発行させる用

    //キャラクター処理時のキー判別用検知イベント
    public event Action RightKeyEvent;
    public event Action LeftKeyEvent;

    //ゲームロジックを外部で動かすためのイベントステート
    public event Action<SceneState> gameState;

    //ゲームステートイベント内に格納されたイベントを講読する。
    public void CallGameStateEvent(SceneState state) { gameState?.Invoke(state); }

    //画面タッチ時のイベント処理を講読する。
    public void CallTouchEvent() { TouchActionEvect?.Invoke(); }


    //初期化
    public void Reset()
    {
        isInputDetected = false;
        isInputHold = false;
        isInputRush = false;
        isEndGame = false;
        isInGame = false;
        isSlideAnim = false;
        isRightKey = false;
        isLeftKey = false;
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

        CallTouchEvent();
    }

    public void SetInputKey(bool right,bool left) 
    {
        isRightKey = right;
        isLeftKey = left;

        if (left)  { LeftKeyEvent?.Invoke(); }
        if (right) { RightKeyEvent?.Invoke(); }
    }
}
