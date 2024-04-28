using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private void Start()
    {
        InputManager.Instance.playerInput.Player.OpenMenu.performed += OpenMenu;
        InputManager.Instance.playerInput.UI.CloseMenu.performed += CloseMenu;

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
