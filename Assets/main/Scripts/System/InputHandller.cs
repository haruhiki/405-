using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandller : MonoBehaviour
{
    public Define _defineSO;
    public Transform leftTargetCircle;
    public Transform rightTargetCircle;
    public NoteDate noteDate;


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
        // キーボード入力部分
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            //座標を送るのではなく、「左レーンが叩かれた」という事実を優先するために
            //判定円の「スクリーン座標」を逆算して送る
            Vector2 screenPos = Camera.main.WorldToScreenPoint(leftTargetCircle.position);
            _defineSO.SetInput(screenPos);

        }
        else if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(rightTargetCircle.position);
            _defineSO.SetInput(screenPos);
        }


    }


    //ショートノーツ時のプッシュ判定
    //ノーツデータ内のノーツ種類と押しているかで判定を取る
    private void SinglePush(bool isShortPush, NoteDate noteDate)
    {
       // if(Keyboard.current.fKey.)
    }

    private void LongPush(bool isLongPush, NoteDate noteDate) 
    {

    }
}
