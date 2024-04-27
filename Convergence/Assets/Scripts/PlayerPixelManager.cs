using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows.WebCam;

public class PlayerPixelManager : PixelManager
{
    [Tooltip("The prefab of the ejected pixel")]
    public GameObject Pixel;

    [Min(0), Tooltip("The proportional size the ejected pixel will be")]
    public float SplitScale = 0.1f;

    [Min(0), Tooltip("The scale the pixel will be propelled based on the mass of the ejected pixel")]
    public float ForceScale = 50.0f;

    [Min(0), Tooltip("The cooldown in seconds for ejecting pixels")]
    public float EjectRate = 1.0f;

    private bool canEject = true;

    private bool ejectIsHeld = false;

    private GravityManager gravityManager;

    private Camera cam;

    private void Start()
    {
        gravityManager = GetComponentInParent<GravityManager>();

        InputManager.Instance.playerInput.Player.Eject.started += EjectStart;
        InputManager.Instance.playerInput.Player.Eject.canceled += EjectCancel;

        cam = Camera.main;
    }

    private void FixedUpdate()
    {
        if (ejectIsHeld)
        {
            Eject();
        }

        if (cam != null)
        {
            cam.transform.position = new Vector3(transform.position.x, transform.position.y, cam.transform.position.z);
        }
    }

    #region Eject
    private void EjectStart(InputAction.CallbackContext context)
    {
        ejectIsHeld = true;
    }

    private void EjectCancel(InputAction.CallbackContext context)
    {
        ejectIsHeld = false;
    }
    private void Eject()
    {
        if (!canEject) return;

        if (cam == null) return;

        if (mass() < 1.0f) return;

        Vector2 mousePos = InputManager.Instance.playerInput.Player.MousePosition.ReadValue<Vector2>();
        Vector2 ejectDirection = (cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0)) - transform.position).normalized;

        GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(ejectDirection.x, ejectDirection.y, 0) * (transform.localScale.x * 0.5f), Pixel.transform.rotation, transform.parent);

        float ejectedMass = Mathf.Round(mass() * SplitScale * 64) / 64f;

        GetComponent<Rigidbody2D>().mass -= ejectedMass;
        
        pixel.GetComponent<Rigidbody2D>().mass = ejectedMass;
        pixel.transform.localScale = Vector3.one * ejectedMass / pixel.GetComponent<PixelManager>().density();

        float force = ejectedMass * ForceScale;

        gravityManager.RegisterBody(pixel, (ejectDirection * force) / pixel.GetComponent<PixelManager>().mass());


        GetComponent<Rigidbody2D>().velocity += (ejectDirection * force) / mass() * -1;

        StartCoroutine(EjectReset(EjectRate));
    }

    private IEnumerator EjectReset(float duration)
    {
        canEject = false;

        yield return new WaitForSeconds(duration);

        canEject = true;
    }

    #endregion


    #region Pause Menu
    private void OpenMenu(InputAction.CallbackContext context)
    {
        InputManager.Instance.playerInput.Player.Disable();
        InputManager.Instance.playerInput.UI.Enable();
    }

    private void CloseMenu(InputAction.CallbackContext context)
    {
        InputManager.Instance.playerInput.Player.Enable();
        InputManager.Instance.playerInput.UI.Disable();
    }

    #endregion

    private void OnDestroy()
    {
        InputManager.Instance.playerInput.Player.Eject.started -= EjectStart;
        InputManager.Instance.playerInput.Player.Eject.canceled -= EjectCancel;

        InputManager.Instance.playerInput.Player.OpenMenu.performed -= OpenMenu;
        InputManager.Instance.playerInput.UI.CloseMenu.performed -= CloseMenu;
    }
}
