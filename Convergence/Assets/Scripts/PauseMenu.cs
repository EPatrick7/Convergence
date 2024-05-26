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
using UnityEngine.Rendering.Universal;
using UnityEngine.Windows;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using DG.Tweening;

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

    private Tween tween;

    [SerializeField]
    private bool multiplayer;

    bool hasRegistered;
    public void RegisterInputs()
    {
        if (hasRegistered)
        {
            Debug.LogWarning("Cannot double register a pause menu!");
            return;
        }
        hasRegistered = true;
        foreach (InputManager inputManager in InputManager.inputManagers)
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
        if(menuFrozen!=null&&UnityEngine.Input.GetMouseButton(0))
        {
            if ((targDelayTime - loadDelay) + 0.1f < Time.timeSinceLevelLoad)
            {
                StopCoroutine(menuFrozen);
                menuFrozen = null;
                LoadScene(delayedLoadedID);
            }

        }

        if(isPauseMenu&&isPaused)
        {
            CutsceneManager.Instance?.PlayerPaused();
        }
        if(EventSystem.current != null&& SelectIfGamepad!=null) {
            if(EventSystem.current.currentSelectedGameObject==null)
            {
                if(InputManager.GamePadDetected)
                    EventSystem.current.SetSelectedGameObject(SelectIfGamepad.gameObject);
                else if (UnityEngine.Input.GetKey(KeyCode.UpArrow)|| UnityEngine.Input.GetKey(KeyCode.Return) || UnityEngine.Input.GetKey(KeyCode.DownArrow) || UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                {
                    EventSystem.current.SetSelectedGameObject(SelectIfGamepad.gameObject);
                }
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
            if(!GravityManager.Instance.isMultiplayer)
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

    public void FadeInPPVol()
	{
        foreach (CameraLook l in CameraLook.camLooks)
        {
            l.GetComponent<Volume>().enabled = true;
            l.GetComponent<Volume>().weight = 0;

            tween?.Kill();

            tween = DOTween.To(()=> l.GetComponent<Volume>().weight, x=> l.GetComponent<Volume>().weight = x, 1, .15f);
            tween.Play();
            break;
        }
    }

    public void FadeOutPPVol()
	{
        foreach (CameraLook l in CameraLook.camLooks)
        {
            l.GetComponent<Volume>().weight = 1;
            tween?.Kill();
            tween = DOTween.To(() => l.GetComponent<Volume>().weight, x => l.GetComponent<Volume>().weight = x, 0, .15f);
            tween.OnComplete(()=>SetPPVol(false));
            break;
        }
    }

    public void UpdateHud(int playerId,bool state)
    {
        foreach (PlayerHud hud in playerHUD)
        {
            if (hud.PlayerID == playerId)
            {
                hud.gameObject.SetActive(state);
                return;
            }
        }
    }
    private void UpdateHuds(bool state)
    {
        foreach(PlayerHud hud in playerHUD)
        {
            hud.gameObject.SetActive(state);
        }
    }
    float openedMenu;
    private void OpenMenu(InputAction.CallbackContext context)
    {
        if (isPauseMenu&&!isPaused)
        {
            Pause();
        }
    }
    public void Pause()
    {

        openedMenu = Time.timeSinceLevelLoad;
        isPaused = true;
        foreach (InputManager inputManager in InputManager.inputManagers)
        {
            inputManager.SetPlayerInput(false);
            inputManager.SetUIInput(true);
        }
        //SetPPVol(true);
        if (!multiplayer)
		{
            var ppCam = Camera.main.GetUniversalAdditionalCameraData();
            if (ppCam.cameraStack.Count > 0)
            {
                ppCam.cameraStack[0].GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
        }
        
        FadeInPPVol();
        indicatorManager.DisableIndicators();
        UpdateHuds(false);
        //cutsceneManager.gameObject.SetActive(false);

        //EventSystem.current.SetSelectedGameObject(ResumeButton);

        gameObject.SetActive(true);
    }

    private void CloseMenu(InputAction.CallbackContext context)
    {
        if (isPauseMenu&&Time.timeSinceLevelLoad-0.2f > openedMenu&& isPaused)
        {
            Resume();
        }
    }

    public void Resume()
    {
        UnPause();
    }

    public void UnPause()
    {

        isPaused = false;
        foreach (InputManager inputManager in InputManager.inputManagers)
        {
            inputManager.SetPlayerInput(true);
            inputManager.SetUIInput(false);
        }
        //SetPPVol(false);
        FadeOutPPVol();
        if (!multiplayer) 
        { 
            var ppCam = Camera.main.GetUniversalAdditionalCameraData();
            if (ppCam.cameraStack.Count > 0)
            {
                ppCam.cameraStack[0].GetUniversalAdditionalCameraData().renderPostProcessing = false;
            }
        }
        
        indicatorManager.EnableIndicators();
        UpdateHuds(true);
        //cutsceneManager.gameObject.SetActive(true);

        gameObject.SetActive(false);
    }
    public void Restart()
    {
        if (!alreadyLoadedScene)
        {
            alreadyLoadedScene = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
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
    Coroutine menuFrozen;
    public float loadDelay;
    public float tutloadDelay;
    float targDelayTime;
    int delayedLoadedID;
    bool alreadyLoadedScene;
    public void LoadSceneDelayed(int id)
    {
        if (menuFrozen==null)
        {
            delayedLoadedID = id;
            menuFrozen=StartCoroutine(DelayedLoadScene(id));
        }
        else
        {
            float delay = loadDelay;
            if (id > 2) delay = tutloadDelay;
            if ((targDelayTime - (delay)) + 0.2f < Time.timeSinceLevelLoad)
            {
                StopCoroutine(menuFrozen);
                menuFrozen = null;
                LoadScene(delayedLoadedID);
            }
        }
    }
    public IEnumerator DelayedLoadScene(int id)
    {
        float delay = loadDelay;
        if (id > 2)
		{
            delay = tutloadDelay;
		}
        targDelayTime = Time.timeSinceLevelLoad + delay;
        yield return new WaitForSeconds(delay);
        if (!alreadyLoadedScene)
        {
            alreadyLoadedScene = true;
            SceneManager.LoadSceneAsync(id);
        }
    }
    public void LoadScene(int id)
    {
        //REMOVE DEBUG::
        if (menuFrozen==null&&!alreadyLoadedScene)
        {
            alreadyLoadedScene = true;
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
