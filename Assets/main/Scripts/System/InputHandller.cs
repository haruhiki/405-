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
        // 1. 毎フレームの最初に入力フラグを綺麗にリセット
        _defineSO.isInputDetected = false; // 押した瞬間フラグ
        _defineSO.isInputHold = false;     // 押しっぱなしフラグ
        _defineSO.isInputRush = false;     // 離した瞬間フラグ（変数名はそのまま流用）

        // 2. 左右のキーの状態をチェックして、フラグを「加算（OR演算）」していく
        CheckKeyInput(Keyboard.current.fKey, leftTargetCircle.position); // 左レーン
        CheckKeyInput(Keyboard.current.jKey, rightTargetCircle.position); // 右レーン
    }

    private void CheckKeyInput(KeyControl key, Vector3 circlePos)
    {
        if (key == null) return;

        // いずれかの入力が確認された場合のみ処理
        if (key.wasPressedThisFrame || key.isPressed || key.wasReleasedThisFrame)
        {
            // 判定円のスクリーン座標を計算
            Vector2 screenPos = Camera.main.WorldToScreenPoint(circlePos);

            // 【重要】現在タッチしている座標として DefineSO に渡す
            _defineSO.inputScreenPos = screenPos;

            // 押した瞬間（1フレームのみ）
            if (key.wasPressedThisFrame)
            {
                _defineSO.isInputDetected = true;
                Debug.Log($"[Input] キーが押されました！ レーン座標: {screenPos}");
            }

            // 押しっぱなし中（押している間ずっと true）
            if (key.isPressed)
            {
                _defineSO.isInputHold = true;
            }

            // 離した瞬間（1フレームのみ）
            if (key.wasReleasedThisFrame)
            {
                _defineSO.isInputRush = true; // Judge側で使っている離し判定フラグ
                Debug.Log($"[Input] キーが離されました！ レーン座標: {screenPos}");
            }
        }
    }
}
