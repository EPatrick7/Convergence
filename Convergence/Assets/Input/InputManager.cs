using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    public static List<InputManager> inputManagers;

    public Action<bool> PlayerSet;

    public Action<bool> UISet;
    [HideInInspector]
    public PlayerInput playerInput;
    public static bool GamePadDetected;
    private void Awake()
    {
        if (inputManagers == null)
        {
            inputManagers = new List<InputManager>();
        }
        playerInput = GetComponent<PlayerInput>();
        inputManagers.Add(this);
        SetPlayerInput(true);
    }
    private void FixedUpdate()
    {
        if (playerInput.currentControlScheme == "Gamepad")
        {
            GamePadDetected = true;
        }
     //   DeviceCount = playerInput.devices.Count;
    }
    public void SetPlayerInput(bool enabled)
    {

        if (enabled)
        {
            playerInput.actions.FindActionMap("Player").Enable();
        }
        else
        {
            playerInput.actions.FindActionMap("Player").Disable();
        }

        PlayerSet?.Invoke(enabled);
    }

    public void SetUIInput(bool enabled)
    {
        
        if (enabled)
            playerInput.actions.FindActionMap("UI").Enable();
        else
            playerInput.actions.FindActionMap("UI").Disable();

        UISet?.Invoke(enabled);
    }
    private void OnDestroy()
    {
        if(PauseMenu.Instance!=null&&!PauseMenu.Instance.hasDeregistered)
        {
            PauseMenu.Instance.DeRegisterInputs();
        }
        foreach(PlayerPixelManager p in GameObject.FindObjectsOfType<PlayerPixelManager>())
        {
            if(p!=null&&!p.hasDeregistered)
            {
                p.DeregisterInputs();
            }
        }
        inputManagers.Remove(this);
      //  GamePadDetected = false;


    }
}
