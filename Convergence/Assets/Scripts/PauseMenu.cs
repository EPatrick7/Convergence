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

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private ColorBlock buttonColors = new ColorBlock();

    private Volume ppVol;
    public bool isPauseMenu = true;
    private void Start()
    {
        if (isPauseMenu)
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

            ppVol = Camera.main.gameObject.GetComponent<Volume>();
            ppVol.enabled = false;
        }

    }

    private void OpenMenu(InputAction.CallbackContext context)
    {
        InputManager.SetPlayerInput(false);
        InputManager.SetUIInput(true);
        ppVol.enabled = true;

        gameObject.SetActive(true);
    }

    private void CloseMenu(InputAction.CallbackContext context)
    {
        Resume();
    }

    public void Resume()
    {
        InputManager.SetPlayerInput(true);
        InputManager.SetUIInput(false);
        ppVol.enabled = false;

        gameObject.SetActive(false);
    }   
    
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadScene(int id)
    {
        SceneManager.LoadSceneAsync(id);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void OnDestroy()
    {
        if (isPauseMenu)
        {
            InputManager.Instance.playerInput.Player.OpenMenu.performed -= OpenMenu;
            InputManager.Instance.playerInput.UI.CloseMenu.performed -= CloseMenu;
        }
    }
}
