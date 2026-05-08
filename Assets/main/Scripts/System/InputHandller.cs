using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandller : MonoBehaviour
{
    public Define _defineSO;
    private void Update()
    {
        //毎フレームリセット
        _defineSO.isInputDetected = false;

        //スマホタッチ
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            if (touch.press.wasPressedThisFrame)
            {
                _defineSO.SetInput(touch.position.ReadValue());
            }
        }
        //キーボード
        else if (Keyboard.current != null)
        {
            // 【F】キーで左（赤丸）の判定テスト
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                // 判定クラスに「画面左側を叩いた」座標を送る（とりあえず固定値）
                _defineSO.SetInput(new Vector2(Screen.width * 0.25f, Screen.height / 2f));
            }
            // 【J】キーで右（青丸）の判定テスト
            else if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                // 判定クラスに「画面右側を叩いた」座標を送る（とりあえず固定値）
                _defineSO.SetInput(new Vector2(Screen.width * 0.75f, Screen.height / 2f));
            }
        }
    }
}
