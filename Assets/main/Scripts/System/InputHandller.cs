using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
        _defineSO.isInputHold = false;
        _defineSO.isInputRush = false;

        ////スマホタッチ
        //if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        //{
        //    var touch = Touchscreen.current.touches[0];
        //    if (touch.press.wasPressedThisFrame)
        //    {
        //        _defineSO.SetInput(touch.position.ReadValue());
        //    }
        //}

        HoldInput(Keyboard.current.jKey, rightTargetCircle.position);
        HoldInput(Keyboard.current.fKey, leftTargetCircle.position);


    }

    private void HoldInput(KeyControl Key,Vector3 circlePos) 
    {
        if (Key.wasPressedThisFrame || Key.isPressed || Key.wasReleasedThisFrame)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(circlePos);
            _defineSO.SetInput(screenPos, Key.wasPressedThisFrame, Key.isPressed, Key.wasReleasedThisFrame);
        }
    }
}
