using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public InputSystemActions playerInput;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        playerInput = new InputSystemActions();

        Instance.playerInput.Player.Enable();
    }
}
