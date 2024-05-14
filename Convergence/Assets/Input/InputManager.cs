using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    public static List<InputManager> inputManagers;

    public Action<bool> PlayerSet;

    public Action<bool> UISet;
    [HideInInspector]
    public PlayerInput playerInput;

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


    }
}
