using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputHandller : MonoBehaviour
{
   public Define _defineSO;
    public Transform leftTargetCircle;
    public Transform rightTargetCircle;

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
    }

    private void CheckKeyInput(KeyControl key, Vector3 circlePos)
    {
        if (key == null) return;

        if (key.wasPressedThisFrame || key.isPressed || key.wasReleasedThisFrame)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(circlePos);
            _defineSO.inputScreenPos = screenPos;

            if (key.wasPressedThisFrame)
            {
                _defineSO.isInputDetected = true;
            }

            if (key.isPressed)
            {
                _defineSO.isInputHold = true;
            }

            if (key.wasReleasedThisFrame)
            {
                _defineSO.isInputRush = true; 
            }
        }
    }
}
