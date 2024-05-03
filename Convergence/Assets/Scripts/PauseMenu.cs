using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private ColorBlock buttonColors = new ColorBlock();

    private void Start()
    {
        InputManager.Instance.playerInput.Player.OpenMenu.performed += OpenMenu;
        InputManager.Instance.playerInput.UI.CloseMenu.performed += CloseMenu;

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
    }

    private void OpenMenu(InputAction.CallbackContext context)
    {
        InputManager.Instance.playerInput.Player.Disable();
        InputManager.Instance.playerInput.UI.Enable();

        gameObject.SetActive(true);
    }

    private void CloseMenu(InputAction.CallbackContext context)
    {
        Resume();
    }

    public void Resume()
    {
        InputManager.Instance.playerInput.Player.Enable();
        InputManager.Instance.playerInput.UI.Disable();

        gameObject.SetActive(false);
    }   
    
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        // TODO
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void OnDestroy()
    {
        InputManager.Instance.playerInput.Player.OpenMenu.performed -= OpenMenu;
        InputManager.Instance.playerInput.UI.CloseMenu.performed -= CloseMenu;
    }
}
