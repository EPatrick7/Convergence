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
using UnityEngine.InputSystem.UI;

public class PauseMenu : MonoBehaviour
{
    public Selectable SelectIfGamepad;

    public static PauseMenu Instance;

    public static bool isPaused;
    [SerializeField]
    private ColorBlock buttonColors = new ColorBlock();

    public bool isPauseMenu = true;

    [SerializeField]
    private IndicatorManager indicatorManager;
    [SerializeField]
    private GameObject ResumeButton;

    [SerializeField]
    private List<PlayerHud> playerHUD;

    [SerializeField]
    private CutsceneManager cutsceneManager;

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
    private void FixedUpdate()
    {
        if(isPauseMenu&&isPaused)
        {
            CutsceneManager.Instance?.PlayerPaused();
        }
        if(EventSystem.current != null&& SelectIfGamepad!=null) {
            if(InputManager.GamePadDetected && EventSystem.current.currentSelectedGameObject==null)
            {
                EventSystem.current.SetSelectedGameObject(SelectIfGamepad.gameObject);
            }
        }
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
    InputActionAsset asset;
    private void Start()
    {
        isPaused = false;
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
        else //if (!isPauseMenu)
        {
            asset = EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset;
            asset.FindActionMap("Player").Enable();
            EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset.FindActionMap("Player").FindAction("Home").started += YesPresHome;
            EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset.FindActionMap("Player").FindAction("Home").canceled += NoPresHome;

            EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset.FindActionMap("Player").FindAction("OpenMenu").started += YesPresPause;
            EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset.FindActionMap("Player").FindAction("OpenMenu").canceled += NoPresPause;

        }

    }
    public void SetPPVol(bool state)
    {
        foreach(CameraLook l in CameraLook.camLooks)
        {
            l.GetComponent<Volume>().enabled = state;
        }
    }
    private void UpdateHuds(bool state)
    {
        foreach(PlayerHud hud in playerHUD)
        {
            hud.gameObject.SetActive(state);
        }
    }
    private void OpenMenu(InputAction.CallbackContext context)
    {
        if (isPauseMenu)
        {
            isPaused = true;
            foreach (InputManager inputManager in InputManager.inputManagers)
            {
                inputManager.SetPlayerInput(false);
                inputManager.SetUIInput(true);
            }
            SetPPVol(true);
            indicatorManager.DisableIndicators();
            UpdateHuds(false);
            //cutsceneManager.gameObject.SetActive(false);

            //EventSystem.current.SetSelectedGameObject(ResumeButton);

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
        isPaused = false;
        foreach (InputManager inputManager in InputManager.inputManagers)
        {
            inputManager.SetPlayerInput(true);
            inputManager.SetUIInput(false);
        }
        SetPPVol(false);
        indicatorManager.EnableIndicators();
        UpdateHuds(true);
        //cutsceneManager.gameObject.SetActive(true);

        gameObject.SetActive(false);
    }   
    

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void YesPresHome(InputAction.CallbackContext context)
    {
        isPressingHome = true;
    }
    public void NoPresHome(InputAction.CallbackContext context)
    {
        isPressingHome = false;
    }
    public bool isPressingHome;


    public void YesPresPause(InputAction.CallbackContext context)
    {
        isPressingSelect = true;
    }
    public void NoPresPause(InputAction.CallbackContext context)
    {
        isPressingSelect = false;
    }
    private bool isPressingSelect;
    public void LoadScene(int id)
    {
        //REMOVE DEBUG::

        if (id == 1 && ((UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKey(KeyCode.LeftShift)) || isPressingHome))
        {
            SceneManager.LoadSceneAsync(2);
        }
        else if (id == 1 && ((UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKey(KeyCode.LeftAlt)) || isPressingSelect))
        {
            SceneManager.LoadSceneAsync(3);
        }
        else//
        {
            SceneManager.LoadSceneAsync(id);
        }
    }
    public void Quit()
    {
        Application.Quit();
    }
    [HideInInspector]
    public bool hasDeregistered;
    public void OnDestroy()
    {
        isPaused = false;
        if (isPauseMenu&&!hasDeregistered)
        {
            DeRegisterInputs();
        }
        if (!isPauseMenu)
        {
            asset.FindActionMap("Player").FindAction("Home").started -= YesPresHome;
            asset.FindActionMap("Player").FindAction("Home").canceled -= NoPresHome;
        }
    }
}
