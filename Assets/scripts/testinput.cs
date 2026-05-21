using UnityEngine;
using UnityEngine.InputSystem;

public class TestInput : MonoBehaviour
{
    private gracz_ruch graczRuch;

    void Start()
    {
        graczRuch = GetComponent<gracz_ruch>();
        Debug.Log("TestInput rozpoczêty");
    }

    void Update()
    {
        // SprawdŸ klawiaturê
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                Debug.Log("Wciœniêto W");
            if (Keyboard.current.aKey.isPressed)
                Debug.Log("Wciœniêto A");
            if (Keyboard.current.sKey.isPressed)
                Debug.Log("Wciœniêto S");
            if (Keyboard.current.dKey.isPressed)
                Debug.Log("Wciœniêto D");
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Debug.Log("Wciœniêto Spacjê");
        }

        // SprawdŸ myszkê
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.magnitude > 0)
                Debug.Log("Ruch myszki: " + delta);
        }
    }
}