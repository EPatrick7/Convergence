using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class PlayerPixelManager : PixelManager
{
    [Tooltip("The prefab of the ejected pixel")]
    public GameObject Pixel;

    [Header("Ejection")]
    [Min(0), Tooltip("The proportional size the ejected pixel will be")]
    public float SplitScale = 0.1f;
    [Min(0), Tooltip("The proportional elements the ejected pixel will steal")]
    public float SplitElements = 0.05f;

    [Min(0), Tooltip("The scale the pixel will be propelled based on the mass of the ejected pixel")]
    public float EjectionForceScale = 50.0f;

    [Min(0), Tooltip("The cooldown in seconds for ejecting pixels")]
    public float EjectionRate = 1.0f;

    [Header("Propulsion")]
    [Min(0), Tooltip("The scale the pixel will be propelled based on the gas expended")]
    public float PropulsionForceScale = 10.0f;

    [Min(0), Tooltip("The cooldown in seconds for propelling")]
    public float PropulsionRate = 0.1f;

    [Min(0), Tooltip("The proportion of gas expended when propelling")]
    public float PropulsionCost = 0.01f;

    [Header("Shield")]
    public Shield Shield;

    [Header("Propeller")]
    public ParticleSystem GasJet;

    private bool canEject = true;

    private bool isPropelling = false;


    private GravityManager gravityManager;

    private Camera cam;

    private void Start()
    {
        gravityManager = GetComponentInParent<GravityManager>();

        InputManager.Instance.playerInput.Player.Eject.performed += Eject;
        InputManager.Instance.playerInput.Player.Propel.started += StartPropel;
        InputManager.Instance.playerInput.Player.Propel.canceled += CancelPropel;
        InputManager.Instance.playerInput.Player.Shield.started += StartShield;
        InputManager.Instance.playerInput.Player.Shield.canceled += CancelShield;
        gameObject.layer = 8;
        cam = Camera.main;
        cam.GetComponent<CameraLook>().playerPixelManager = this;
    }

    private Vector2 MouseDirection()
    {
        Vector2 mousePos = InputManager.Instance.playerInput.Player.MousePosition.ReadValue<Vector2>();
        return (cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0)) - transform.position).normalized;
    }

    #region Eject
    private void Eject(InputAction.CallbackContext context)
    {
        if (!canEject) return;

        if (cam == null) return;

        if (mass() <= 1.0f) return;

        Vector2 ejectDirection = MouseDirection();

        // Create ejected pixel
        GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(ejectDirection.x, ejectDirection.y, 0) * (transform.localScale.x * 0.525f), Pixel.transform.rotation, transform.parent);

        // Handle mass of player and ejected pixel
        float ejectedMass = Mathf.Round(mass() * SplitScale * 64) / 64f;

        ejectedMass /= Mathf.Min(6, Mathf.Max(1, (mass() / 500f)));
        

        GetComponent<Rigidbody2D>().mass -= ejectedMass;

        pixel.GetComponent<Rigidbody2D>().mass = ejectedMass;
        pixel.transform.localScale = Vector3.one * pixel.GetComponent<PixelManager>().radius(ejectedMass);

        // Handle ejection push
        float force = (ejectedMass + Mathf.Clamp(ejectedMass / Mathf.Log(ejectedMass), 0f, Mathf.Pow(ejectedMass, 2f))) * EjectionForceScale;


        GetComponent<Rigidbody2D>().velocity += (ejectDirection * force) / mass() * -1 * Mathf.Max(1, (mass() / 600f));

        gravityManager.RegisterBody(pixel, (ejectDirection * force) / pixel.GetComponent<PixelManager>().mass());

        InvokeMassChanged();

        StartCoroutine(EjectReset(EjectionRate));
    }

    private IEnumerator EjectReset(float duration)
    {
        canEject = false;

        yield return new WaitForSeconds(duration);

        canEject = true;
    }
    #endregion

    #region Propel

    public void StartParticles()
    {

        GasJet.transform.localScale = new Vector3(Mathf.Max(1, transform.localScale.x / 15f), Mathf.Max(1, transform.localScale.y / 15f), Mathf.Max(1, transform.localScale.z /15f));
        var em = GasJet.emission;
        em.enabled = true;
        if (!GasJet.isPlaying)
            GasJet.Play();
    }
    public void StopParticles()
    {


        var em = GasJet.emission;
        em.enabled = false;
        if (GasJet.isPlaying)
            GasJet.Stop();
    }
    private void StartPropel(InputAction.CallbackContext context)
    {
        if (isPropelling) return;


        isPropelling = true;

        StartCoroutine(Propel(PropulsionRate));
    }

    private void CancelPropel(InputAction.CallbackContext context)
    {
        if (!isPropelling) return;

        StopParticles();

        isPropelling = false;
    }
    private void FixedUpdate()
    {
        Vector2 diff = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        GasJet.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);
    }
    private IEnumerator Propel(float interval)
    {
        int streak=0;
        while (isPropelling)
        {
            if (Gas > 0f)
            {
                float expendedGas = Mathf.Max(1f, mass() + Gas) * PropulsionCost * interval;

                Gas -= expendedGas*0.75f;

                Vector2 propelDirection = MouseDirection();
                float propulsionForce = expendedGas * PropulsionForceScale;


                GetComponent<Rigidbody2D>().velocity += (propelDirection * propulsionForce) / mass() * -1 * Mathf.Min(5.5f, Mathf.Max(1, (mass() / 50f)));


                yield return new WaitForSeconds(interval);

                if (streak > 1)
                {
                    StartParticles();
                }
                streak++;
            }
            else
            {
                yield return new WaitForSeconds(interval);
                StopParticles();
            }
        }
        StopParticles();

    }
    #endregion

    #region Shield
    private void StartShield(InputAction.CallbackContext context)
    {
        if (Shield == null) return;

        if (isShielding) return;

        isShielding = true;

        Shield.ShieldUp();
    }

    private void CancelShield(InputAction.CallbackContext context)
    {
        if (Shield == null) return;

        if (!isShielding) return;

        isShielding = false;

        Shield.ShieldDown();
    }
    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();

        InputManager.Instance.playerInput.Player.Eject.performed -= Eject;
        InputManager.Instance.playerInput.Player.Propel.started -= StartPropel;
        InputManager.Instance.playerInput.Player.Propel.canceled -= CancelPropel;
        InputManager.Instance.playerInput.Player.Shield.started -= StartShield;
        InputManager.Instance.playerInput.Player.Shield.canceled -= CancelShield;
    }
}
