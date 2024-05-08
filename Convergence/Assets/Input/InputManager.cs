using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public InputSystemActions playerInput;

    public Action<bool> PlayerSet;

    public Action<bool> UISet;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        playerInput = new InputSystemActions();

        InputManager.SetPlayerInput(true);
    }

    public static void SetPlayerInput(bool enabled)
    {
        if (Instance == null) return;

        if (enabled)
            Instance.playerInput.Player.Enable();
        else
            Instance.playerInput.Player.Disable();

        Instance.PlayerSet?.Invoke(enabled);
    }

    public static void SetUIInput(bool enabled)
    {
        if (Instance == null) return;

        if (enabled)
            Instance.playerInput.UI.Enable();
        else
            Instance.playerInput.UI.Disable();

        Instance.UISet?.Invoke(enabled);
    }
}
