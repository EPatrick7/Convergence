using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using static UnityEngine.GraphicsBuffer;
using DG.Tweening;

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
    [Header("Effects")]
    public ParticleSystem StarvationFX;


    public GameObject RespawnFX;

    [HideInInspector]
    public int PlayerID;
    private bool canEject = true;

    private bool isPropelling = false;


    private GravityManager gravityManager;

    private Camera cam;
    
    public void RegisterInputs()
    {
        pInput.actions.FindActionMap("Player").FindAction("Eject").performed+=Eject;
        pInput.actions.FindActionMap("Player").FindAction("Propel").started += StartPropel;
        pInput.actions.FindActionMap("Player").FindAction("Propel").canceled += CancelPropel;
        pInput.actions.FindActionMap("Player").FindAction("Shield").started += StartShield;
        pInput.actions.FindActionMap("Player").FindAction("Shield").canceled += CancelShield;
    }
    [HideInInspector]
    public bool hasDeregistered;
    public void DeregisterInputs()
    {
        hasDeregistered = true;
        pInput.actions.FindActionMap("Player").FindAction("Eject").performed -= Eject;
        pInput.actions.FindActionMap("Player").FindAction("Propel").started -= StartPropel;
        pInput.actions.FindActionMap("Player").FindAction("Propel").canceled -= CancelPropel;
        pInput.actions.FindActionMap("Player").FindAction("Shield").started -= StartShield;
        pInput.actions.FindActionMap("Player").FindAction("Shield").canceled -= CancelShield;
    }
    public Vector2 MousePos()
    {
        return pInput.actions.FindActionMap("Player").FindAction("MousePosition").ReadValue<Vector2>(); 
    }
    CameraLook camLook;
    [HideInInspector]
    public PlayerInput pInput;
    private void Start()
    {

        gravityManager =GravityManager.Instance;

        gameObject.layer = 8;
        foreach(CameraLook l in CameraLook.camLooks)
        {
            if(l.PlayerID==PlayerID)
            {
                cam = l.GetComponent<Camera>();

                camLook = cam.GetComponent<CameraLook>();
                pInput = camLook.inputManager.GetComponent<PlayerInput>();
                l.focusedPixel = this;
            }
        }
        RegisterInputs();
    }
    Vector2 lastMousePos=new Vector2(0,-1);
    public Vector2 MouseDirection()
    {
        Vector2 mousePos = MousePos();
        if (pInput.currentControlScheme == "Gamepad")
        {
           if(mousePos==Vector2.zero)
            {
                return lastMousePos;
            }
            mousePos = mousePos.normalized;
            lastMousePos = mousePos;
            return mousePos;
        }
        else
        {
            return (cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0)) - transform.position).normalized;
        }
    }
    public void RunDeath(PlayerPixelManager eater = null)
    {
        if (GravityManager.Instance.respawn_players)
        {
            if (RespawnFX != null)
            {
                PlayerRespawner r = Instantiate(RespawnFX, transform.position, RespawnFX.transform.rotation, transform.parent).GetComponent<PlayerRespawner>();
                if (GravityManager.Instance.random_respawn_players)
                {
                    r.transform.position = GravityManager.Instance.RespawnPos();
                }

                float avgMass = 0;
                float numLooks = 0;
                foreach (CameraLook clook in CameraLook.camLooks)
                {
                    if (clook != null && clook.focusedPixel != null)
                    {
                        avgMass += clook.focusedPixel.GetComponent<Rigidbody2D>().mass;
                        numLooks++;
                    }
                }
                if (numLooks > 0)
                    r.SetMass = Mathf.Min(500, 0.1f * avgMass / numLooks);

                //g.transform.DOScale(.1f, 9);
                r.PlayerID = PlayerID;

                if (eater != null)
                {
                    //Play cutscene here.
                }
                camLook.respawner = r;

            }
        }
        else if(GravityManager.GameWinner!=null)
        {
            camLook.focusedPixel = GravityManager.GameWinner;
        }
    }
    #region Eject
    private void Eject(InputAction.CallbackContext context)
    {
        if (!canEject) return;

        if (cam == null) return;


        CutsceneManager.Instance.PlayerEjected();

        camLook.inputManager.EjectRumble();
        Vector2 ejectDirection = MouseDirection();

        // Create ejected pixel
        GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(ejectDirection.x, ejectDirection.y, 0) * (transform.localScale.x * 0.525f), Pixel.transform.rotation, transform.parent);

        // Handle mass of player and ejected pixel
        float ejectedMass = Mathf.Round(mass() * SplitScale * 64) / 64f;

        ejectedMass /= Mathf.Min(6, Mathf.Max(1, (mass() / 500f)));
        

        rigidBody.mass -= ejectedMass;

        pixel.GetComponent<PixelManager>().Initialize();
        pixel.GetComponent<Rigidbody2D>().mass = ejectedMass;
        pixel.transform.localScale = Vector3.one * pixel.GetComponent<PixelManager>().radius(ejectedMass);

        // Handle ejection push
        float force = (ejectedMass + Mathf.Clamp(ejectedMass / Mathf.Log(ejectedMass), 0f, Mathf.Pow(ejectedMass, 2f))) * EjectionForceScale;


        if (mass() <= 5.0f)
        {
            force = 0;
        }
        rigidBody.velocity += (ejectDirection * force) / mass() * -1 * Mathf.Max(1, (mass() / 600f));

        gravityManager.RegisterBody(pixel, (ejectDirection * force) / pixel.GetComponent<PixelManager>().mass());

        InvokeMassChanged();

        StartCoroutine(EjectReset(EjectionRate));
        if (mass() <= 5.0f)
        {
            Starve();
        }
    }
    
    public void Starve()
    {
        if(StarvationFX!=null)
        {
            StarvationFX.transform.parent = null;
            StarvationFX.Play();
        }
        RunDeath();
        CutsceneManager.Instance.PlayerConsumed();
        Destroy(gameObject);
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
        Vector2 diff = MouseDirection();

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

                yield return new WaitForSeconds(interval);

                if (streak > 1)
                {

                    float expendedGas = Mathf.Max(1f, mass() + Gas) * PropulsionCost * interval;

                    Gas -= expendedGas * 0.75f;

                    Vector2 propelDirection = MouseDirection();
                    float propulsionForce = expendedGas * PropulsionForceScale;


                    rigidBody.velocity += (propelDirection * propulsionForce) / mass() * -1 * Mathf.Min(5.5f, Mathf.Max(1, (mass() / 50f)));

                    if (Gas > 0f)
                    {
                        CutsceneManager.Instance.PlayerPropelled();
                    }
                    camLook.inputManager.PropelRumble();
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

    public void Ambient()
    {
        camLook?.inputManager?.AmbientRumble(planetType);
    }
    public void Bonk(bool isLarger,bool isMicroscopic,bool isSlightlySmaller)
    {
        camLook.inputManager.BonkRumble(isLarger, isMicroscopic, isSlightlySmaller);
    }
    #region Shield
    private void StartShield(InputAction.CallbackContext context)
    {
        if (Shield == null) return;

        if (isShielding) return;


        if (Ice > 0f)
        {
            CutsceneManager.Instance.PlayerShielded();
        }
        isShielding = true;

        Shield.ShieldUp();
    }
    public float ShieldRadius()
    {
        return Shield.transform.lossyScale.x * Shield.GetComponent<CircleCollider2D>().radius;
    }
    /*
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, ShieldRadius());
    }
    */
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
        if(!hasDeregistered)
            DeregisterInputs();
    }
}
