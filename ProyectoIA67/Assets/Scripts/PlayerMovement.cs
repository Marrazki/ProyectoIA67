using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        GetInputs();
    }

    void GetInputs()
    {
        Vector2 input = Vector2.zero;

        if (Gamepad.current != null)
        {
            input = Gamepad.current.leftStick.ReadValue();
        }
        else if (Keyboard.current != null)
        {
            float x = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                x -= 1f;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            float y = 0f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                y -= 1f;
            }
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                y += 1f;
            }
            if (Keyboard.current.leftShiftKey.isPressed)
            {
                speed = 10f;
            }
            else
            {
                speed = 5f;
            }

            input = new Vector2(x, y);
            input = Vector2.ClampMagnitude(input, 1f);
        }

        Vector3 movement = new Vector3(input.x, 0f, input.y) * (speed * Time.deltaTime);
        transform.Translate(movement, Space.World);
    }
}
