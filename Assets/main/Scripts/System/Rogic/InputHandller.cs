using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputHandller : MonoBehaviour
{
   public Define _defineSO;
    public Transform leftTargetCircle;
    public Transform rightTargetCircle;
    
    // Update is called once per frame
    private void Update()
    {
        if (_defineSO == null || Keyboard.current == null) return;

        // 1. 毎フレームの最初に入力フラグをリセット
        _defineSO.isInputDetected = false; 
        _defineSO.isInputHold = false;     
        _defineSO.isInputRush = false;     

        // 【追加】左右キーの生の押し状態をDefineSOへ毎フレーム確実に同期する
        _defineSO.isLeftKey = Keyboard.current.fKey.isPressed;
        _defineSO.isRightKey = Keyboard.current.jKey.isPressed;

        // 2. 左右のキーの状態をチェックしてフラグを加算
        CheckKeyInput(Keyboard.current.fKey, leftTargetCircle.position); // 左レーン
        CheckKeyInput(Keyboard.current.jKey, rightTargetCircle.position); // 右レーン

        //test用に矢印キーの入力を検知するためのメソッドを呼び出す
        //矢印キーの入力を検知するためのメソッドを呼び出す
        CheckArrowKeyInput();
    }

    // キー入力をチェックしてDefineSOのフラグを更新するメソッド
    private void CheckKeyInput(KeyControl key, Vector3 circlePos)
    {
        // キーがnullの場合は処理をスキップ
        if (key == null) return;
        
        // キーが押された、押されている、または離された場合にフラグを更新
        if (key.wasPressedThisFrame || key.isPressed || key.wasReleasedThisFrame)
        {
            // キーが押された瞬間、押されている間、または離された瞬間にスクリーン座標を更新
            Vector2 screenPos = Camera.main.WorldToScreenPoint(circlePos);
            _defineSO.inputScreenPos = screenPos;

            // キーの状態に応じてDefineSOのフラグを更新
            if (key.wasPressedThisFrame)
            {
                _defineSO.isInputDetected = true;
            }
            
            // キーが押されている間にフラグを更新
            if (key.isPressed)
            {
                _defineSO.isInputHold = true;
            }
            
            // キーが離された瞬間にフラグを更新
            if (key.wasReleasedThisFrame)
            {
                _defineSO.isInputRush = true; 
            }
        }
    }

    //別で矢印キーの入力を検知するためのメソッド
    public void CheckArrowKeyInput()
    {
        if (_defineSO == null || Keyboard.current == null) return;

        // 上矢印キーの状態を更新
        _defineSO.isUpArrowKey = Keyboard.current.upArrowKey.isPressed;

        // 下矢印キーの状態を更新
        _defineSO.isDownArrowKey = Keyboard.current.downArrowKey.isPressed;

    }
}
