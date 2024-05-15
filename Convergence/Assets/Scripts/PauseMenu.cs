using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Windows;
using UnityEngine.InputSystem.Controls;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [SerializeField]
    private ColorBlock buttonColors = new ColorBlock();

    public bool isPauseMenu = true;

    [SerializeField]
    private IndicatorManager indicatorManager;
    [SerializeField]
    private GameObject ResumeButton;

    public void RegisterInputs()
    {
        foreach(InputManager inputManager in InputManager.inputManagers)
        {
            inputManager.playerInput.actions.FindActionMap("Player").FindAction("OpenMenu").performed += OpenMenu;
            inputManager.playerInput.actions.FindActionMap("UI").FindAction("CloseMenu").performed += CloseMenu;

        }
      /* InputManager.Instance.playerInput.Player.OpenMenu.performed += OpenMenu;
        InputManager.Instance.playerInput.UI.CloseMenu.performed += CloseMenu;
      */
    }
    public void DeRegisterInputs()
    {
        hasDeregistered = true;

        foreach (InputManager inputManager in InputManager.inputManagers)
        {

            inputManager.playerInput.actions.FindActionMap("Player").FindAction("OpenMenu").performed -= OpenMenu;
            inputManager.playerInput.actions.FindActionMap("UI").FindAction("CloseMenu").performed -= CloseMenu;

        }
        //
        //InputManager.Instance.playerInput.Player.OpenMenu.performed -= OpenMenu;
        //InputManager.Instance.playerInput.UI.CloseMenu.performed -= CloseMenu;

    }
    private void Start()
    {
        Instance = this;
        if (isPauseMenu)
        {
            
            foreach (Button button in GetComponentsInChildren<Button>())
            {
                ColorBlock colors = button.colors;
                colors.normalColor = buttonColors.normalColor;
                colors.highlightedColor = buttonColors.highlightedColor;
                colors.pressedColor = buttonColors.pressedColor;
                colors.selectedColor = buttonColors.selectedColor;
                colors.disabledColor = buttonColors.disabledColor;
                button.colors = colors;
            }

            gameObject.SetActive(false);
            SetPPVol(false);

            RegisterInputs();
        }

    }
    public void SetPPVol(bool state)
    {
        foreach(CameraLook l in CameraLook.camLooks)
        {
            l.GetComponent<Volume>().enabled = state;
        }
    }
    private void OpenMenu(InputAction.CallbackContext context)
    {
        if (isPauseMenu)
        {


            foreach (InputManager inputManager in InputManager.inputManagers)
            {
                inputManager.SetPlayerInput(false);
                inputManager.SetUIInput(true);
            }
            SetPPVol(true);
            indicatorManager.DisableIndicators();

            EventSystem.current.SetSelectedGameObject(ResumeButton);

            gameObject.SetActive(true);
        }
    }

    private void CloseMenu(InputAction.CallbackContext context)
    {
        if (isPauseMenu)
        {
            Resume();
        }
    }

    public void Resume()
    {
        foreach (InputManager inputManager in InputManager.inputManagers)
        {
            inputManager.SetPlayerInput(true);
            inputManager.SetUIInput(false);
        }
        SetPPVol(false);
        indicatorManager.EnableIndicators();

        gameObject.SetActive(false);
    }   
    
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private void FixedUpdate()
    {
        ;
    }
    public void LoadScene(int id)
    {
        //REMOVE DEBUG::
        bool isPressingHome = UnityEngine.Input.GetButton("Home")||UnityEngine.Input.GetKey(KeyCode.JoystickButton12);
        if (id==1&&((UnityEngine.Input.GetKey(KeyCode.LeftControl)&& UnityEngine.Input.GetKey(KeyCode.LeftShift))|| isPressingHome))
        {
            SceneManager.LoadSceneAsync(2);
        }
        else//
            SceneManager.LoadSceneAsync(id);
    }

    public void Quit()
    {
        Application.Quit();
    }
    [HideInInspector]
    public bool hasDeregistered;
    public void OnDestroy()
    {
        if (isPauseMenu&&!hasDeregistered)
        {
            DeRegisterInputs();
        }
    }
}
