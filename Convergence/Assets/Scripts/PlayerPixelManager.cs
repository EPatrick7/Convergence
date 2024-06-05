using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;
using DG.Tweening;

public class PlayerPixelManager : PixelManager
{
    [Tooltip("The prefab of the ejected pixel")]
    public GameObject Pixel;

    [Header("Ejection")]
    [Min(0), Tooltip("The proportional size the ejected pixel will be")]
    public float SplitScale = 0.1f;
    [Min(1),Tooltip("The multiplier to split scale applied post sun")]
    public float postSunSplitScale_Multiplier = 3f;

    [Tooltip("The multiplier to the gas element absorption based on mass (1->10000)")]
    public AnimationCurve GasPickupMultiplier;
    public float GasMultiplier()
    {
        return GasPickupMultiplier.Evaluate(mass() / 10000f);
    }

    [ Tooltip("The multiplier to split scale applied based on radius (1->1000)")]
    public AnimationCurve RadiusSplitScale_Multiplier;

    [Tooltip("The scale the pixel will be propelled based on the mass of the ejected pixel")]
    public float EjectionForceScale = 2.0f;

    [Tooltip("The multiplier applied to the ejected body speed")]
    public float EjectedSpeedMult = 50;

    [Tooltip("On a scale of 0->750 In Stage in mass, is a number multiplied to the EFS")]
    public AnimationCurve PlanetForceDampenScale;
    [Tooltip("On a scale of 750->7500 In Stage in mass, is a number multiplied to the EFS")]
    public AnimationCurve SunForceDampenScale;
    [Tooltip("On a scale of 7500->10000 In Stage in mass, is a number multiplied to the EFS")]
    public AnimationCurve BHForceDampenScale;
    [Tooltip("On a scale of 1->1000 In Radius units, is a number multiplied to the EFS")]
    public AnimationCurve RadiusForceDampenScale;

    [Tooltip("On a scale of 1->((Radius())^2*VelMagnOffset) In Velocity sqr magnitude units, is a number multiplied to the EFS")]
    public AnimationCurve VelocityMagnitudeForceDampenScale;
    [Tooltip("The above curve is on a scale of 1->((Radius())^2*VelMagnOffset) In Velocity sqr magnitude units, and is a number multiplied to the EFS")]
    public float VelMagnOffset =50;

    [Min(0), Tooltip("The cooldown in seconds for ejecting pixels")]
    public float EjectionRate = 1.0f;

    [Header("Propulsion")]
    [Tooltip("The multiplier to split scale applied based on radius (1->1000)")]
    public AnimationCurve RadiusPropCostScale_Multiplier;
    [Min(0), Tooltip("The scale the pixel will be propelled based on the gas expended")]
    public float PropulsionForceScale = 10.0f;
    [Tooltip("On a scale of 1->((Radius())^2*VelMagnOffset) In Velocity sqr magnitude units, is a number multiplied to the PFS")]
    public AnimationCurve VelocityMagnitudePropForceDampenScale;
    [Tooltip("On a scale of 1->1000 In Radius units, is a number multiplied to the PFS")]
    public AnimationCurve RadiusPropForceDampenScale;
    [Tooltip("The above curve is on a scale of 1->((Radius())^2*VelMagnOffset) In Velocity sqr magnitude units, and is a number multiplied to the PFS")]
    public float PropVelMagnOffset = 50;

    [Min(0), Tooltip("The cooldown in seconds for propelling")]
    public float PropulsionRate = 0.1f;

    [Min(0), Tooltip("The proportion of gas expended when propelling")]
    public float PropulsionCost = 0.01f;

    [Header("Shield")]
    public Shield Shield;

    [HideInInspector]
    [Tooltip("isShielding is true when the player input for it is active")]
    public bool isShielding { get; private set; } = false;
    [HideInInspector]
    public bool shieldActivated;

    [Header("Propeller")]
    public ParticleSystem GasJet;
    [Header("Effects")]
    public ParticleSystem StarvationFX;


    public GameObject RespawnFX;
    public GameObject PlayerIcon;
    [Range(1,4)]
    public int PlayerID;

    [HideInInspector]
    public bool hasDeregistered;


    private GravityManager gravityManager;
    private Camera cam;
    public CameraLook camLook;
    public PlayerInput pInput;

    private bool canEject = true;
    private bool isPropelling = false;


    [HideInInspector]
    public bool hasRegistered;
    [HideInInspector]
    public bool hasWonGame;
    float dangerUntil;
    public bool inDanger = false;
    Coroutine dangerAwait; [HideInInspector]
    Vector2 lastMousePos = new Vector2(0, -1);

    private void Start()
    {

        gravityManager =GravityManager.Instance;

        gameObject.layer = 8;
        gameObject.name = "Player (" + PlayerID + ")";
        foreach (CameraLook l in CameraLook.camLooks)
        {
            if(l.PlayerID==PlayerID)
            {
                cam = l.GetComponent<Camera>();

                camLook = cam.GetComponent<CameraLook>();
                pInput = camLook.inputManager.GetComponent<PlayerInput>();
                l.focusedPixel = this;


                PlayerIcon.GetComponent<SpriteRenderer>().color = camLook.inputManager.PlayerColors[PlayerID - 1];
                PlayerIcon.gameObject.layer = LayerMask.NameToLayer("P" + PlayerID);
            }

        }
        StartCoroutine(DelayedRegister());

    }
    //Delays registering the inputs until a system is located.

    #region Inputs
    public IEnumerator DelayedRegister()
    {
        while (pInput == null)
        {

            if (camLook == null)
            {
                foreach (CameraLook l in CameraLook.camLooks)
                {
                    if (l.PlayerID == PlayerID)
                    {
                        cam = l.GetComponent<Camera>();

                        camLook = cam.GetComponent<CameraLook>();
                        pInput = camLook.inputManager.GetComponent<PlayerInput>();
                        l.focusedPixel = this;


                        PlayerIcon.GetComponent<SpriteRenderer>().color = camLook.inputManager.PlayerColors[PlayerID - 1];
                        PlayerIcon.gameObject.layer = LayerMask.NameToLayer("P" + PlayerID);
                    }

                }
            }
            else
            {
                pInput = camLook.inputManager.GetComponent<PlayerInput>();
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForEndOfFrame();
        }
        RegisterInputs();
    }
    public Vector2 MouseDirection()
    {
        if (!hasRegistered)
        {
            if(GetComponent<OnlinePixelManager>() == null) //Expected Behavior For Online Pixels
                Debug.LogWarning("Tried to get MouseDirection, Input not Ready!");
            return Vector2.zero;
        }
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
    public void DeregisterInputs()
    {
        if (hasRegistered)
        {
            hasRegistered = false;
            hasDeregistered = true;
            pInput.actions.FindActionMap("Player").FindAction("Eject").performed -= Eject;
            pInput.actions.FindActionMap("Player").FindAction("Propel").started -= StartPropel;
            pInput.actions.FindActionMap("Player").FindAction("Propel").canceled -= CancelPropel;
            pInput.actions.FindActionMap("Player").FindAction("Shield").started -= StartShield;
            pInput.actions.FindActionMap("Player").FindAction("Shield").canceled -= CancelShield;
        }
    }
    public Vector2 MousePos()
    {
        if (!hasRegistered)
        {
            Debug.LogWarning("Tried to get MouseDirection, Input not Ready!");
            return Vector2.zero;
        }
        return pInput.actions.FindActionMap("Player").FindAction("MousePosition").ReadValue<Vector2>();
    }
    public void RegisterInputs()
    {
        if (hasRegistered)
        {
            Debug.LogWarning("Cannot double register a player!");
        }
        if (!hasRegistered)
        {
            hasRegistered = true;
            pInput.actions.FindActionMap("Player").FindAction("Eject").performed += Eject;
            pInput.actions.FindActionMap("Player").FindAction("Propel").started += StartPropel;
            pInput.actions.FindActionMap("Player").FindAction("Propel").canceled += CancelPropel;
            pInput.actions.FindActionMap("Player").FindAction("Shield").started += StartShield;
            pInput.actions.FindActionMap("Player").FindAction("Shield").canceled += CancelShield;
        }
    }

    #endregion

    #region Events

    public void WinGame(PixelManager other)
    {
        //AudioManager.Instance?.PlayerWinSucceedSFX();
        //If we just consumed the central black hole...
        other.ConstantMass = false;
        ConstantMass = true;
        other.rigidBody.mass+= other.MassOverride/50f;
        other.MassOverride = 0;
        other.rigidBody.constraints = RigidbodyConstraints2D.None;
        rigidBody.constraints = RigidbodyConstraints2D.FreezePosition;
        rigidBody.mass += 10000;
        GravityManager.Instance.offset_drift = transform.position;
        GravityManager.Instance.drift_power = 300;
        GravityManager.Instance.DoParticleRespawn = false;
        GravityManager.Instance.respawn_players = false;
        GravityManager.GameWinner = playerPixel;
        hasWonGame = true;
        AudioManager.Instance?.PlayerAbsorbBigSFX();
        BlackHoleState.RadiusScalar *= 1.3f;
        CutsceneManager.Instance?.BlackHoleConsumed();

        CreditsMenu.Instance?.DelayRollCredits();

    }
    public void RunDeath()
    {
        AudioManager.Instance?.StopPlayerJet();
        AudioManager.Instance?.NormalSpeed();
        if (RespawnFX != null && GravityManager.Instance.respawn_players)
        {
            PlayerRespawner r = Instantiate(RespawnFX, transform.position, RespawnFX.transform.rotation, transform.parent).GetComponent<PlayerRespawner>();
            //g.transform.DOScale(.1f, 9);
            r.PlayerID = PlayerID;
            camLook.respawner = r;
            if (GravityManager.Instance.random_respawn_players)
            {
                r.transform.position = GravityManager.Instance.RespawnPos();
            }
            else if (r.RestartsScene)
            {
                r.transform.position = startPos;
            }
            else
            {
                r.transform.position = transform.position;
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
                r.SetMass = Mathf.Min(725, 0.33f * avgMass / numLooks);

            //g.transform.DOScale(.1f, 9);
            r.PlayerID = PlayerID;

            camLook.respawner = r;

        }
        else if (GravityManager.GameWinner != null)
        {
            camLook.focusedPixel = GravityManager.GameWinner;
        }
    }
    public void Ambient()
    {
        camLook?.inputManager?.AmbientRumble(planetType);
    }
    #endregion

    #region Danger
    private bool notInDanger()
    {
        return Time.timeSinceLevelLoad > dangerUntil;
    }
    public IEnumerator DangerWaitUnitl()
    {
        //Activate Warning HUD
        //Debug.Log("Player " + PlayerID + " is in danger!");
        yield return new WaitUntil(notInDanger);
        inDanger = false;
        AudioManager.Instance?.NormalSpeed();
        //Deactivate Warning HUD
        //Debug.Log("Player " + PlayerID + " is no longer in danger!");
        dangerAwait = null;
    }
    public void WarnDanger()
    {
        AudioManager.Instance?.InDangerSpeed();
        camLook.inputManager.DangerRumble();
        inDanger = true;
        dangerUntil = Time.timeSinceLevelLoad + 1.5f;
        if (dangerAwait == null)
        {
            dangerAwait = StartCoroutine(DangerWaitUnitl());
        }
    }
    #endregion

    #region Eject
    private void Eject(InputAction.CallbackContext context)
    {
        if (Time.timeSinceLevelLoad < 0.1f)
            return;
        if (!canEject) return;
        if (hasWonGame)
            return;
        if (GravityManager.Instance.SimulationFrozen())
            return;

        if (cam == null) return;


        CutsceneManager.Instance?.PlayerEjected();

        camLook.inputManager.EjectRumble();
        Vector2 ejectDirection = MouseDirection();

        if (mass() < 750)
		{
            AudioManager.Instance?.PlayerEject();
        } else
		{
            AudioManager.Instance?.PlayerEjectBig();
		}
        

        // Create ejected pixel
        GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(ejectDirection.x, ejectDirection.y, 0) * (transform.localScale.x * 0.63f), Pixel.transform.rotation, transform.parent);

        // Handle mass of player and ejected pixel
        /* Legacy Mass
        float ejectedMass = Mathf.Round(mass() * SplitScale * 64) / 64f;

        ejectedMass /= Mathf.Min(6, Mathf.Max(1, (mass() / 500f)));
        */
        float splitMultiplier = 1;
        if(mass()>SunTransition_MassReq*1.25f)
        {
            splitMultiplier *=postSunSplitScale_Multiplier;
        }
        splitMultiplier *= RadiusSplitScale_Multiplier.Evaluate(radius() / 1000f);
        float ejectedMass = Mathf.Round(radius() * splitMultiplier * SplitScale * 64) / 64f;

        rigidBody.mass -= ejectedMass;

        pixel.GetComponent<PixelManager>().Initialize();
        pixel.GetComponent<Rigidbody2D>().mass = ejectedMass;
        pixel.transform.localScale = Vector3.one * pixel.GetComponent<PixelManager>().radius(ejectedMass);

        // Handle ejection push
        /*Legacy Force
        float force = (ejectedMass + Mathf.Clamp(ejectedMass / Mathf.Log(ejectedMass), 0f, Mathf.Pow(ejectedMass, 2f))) * EjectionForceScale;


        if (mass() <= 5.0f)
        {
            force = 0;
        }
        rigidBody.velocity += (ejectDirection * force) / mass() * -1 * Mathf.Max(1, (mass() / 600f));

        gravityManager.RegisterBody(pixel, (ejectDirection * force) / pixel.GetComponent<PixelManager>().mass());
        */
        float dampener = PlanetForceDampenScale.Evaluate(mass() / SunTransition_MassReq);
        if(mass()>=SunTransition_MassReq)
        {
            dampener*=SunForceDampenScale.Evaluate((mass() - SunTransition_MassReq) / (BlackHoleTransition_MassReq - SunTransition_MassReq));
        }
        if (mass() >= BlackHoleTransition_MassReq)
        {
            dampener*= BHForceDampenScale.Evaluate((mass() - BlackHoleTransition_MassReq) / (10000f - BlackHoleTransition_MassReq));
        }
        dampener *= RadiusForceDampenScale.Evaluate(radius() / 1000f);
        dampener *= VelocityMagnitudeForceDampenScale.Evaluate(rigidBody.velocity.sqrMagnitude / (VelMagnOffset*radius() * radius()));
        float force = EjectionForceScale*dampener;
        rigidBody.velocity += ejectDirection * -force * radius();
        float ejectMulti=1+((mass()/10000f)*15);

        gravityManager.RegisterBody(pixel, rigidBody.velocity+ ejectDirection.normalized* EjectedSpeedMult* ejectMulti /*+(ejectDirection* ejectMulti * EjectedSpeedMult* Mathf.Max(1,radius(ejectedMass)) * Mathf.Max(0.25f, force))*/);

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
        if(hasRegistered)
            CutsceneManager.Instance?.PlayerConsumed();
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
        {
            AudioManager.Instance?.StartPlayerJet();
        }
        GasJet.Play();
    }
    public void StopParticles()
    {
        var em = GasJet.emission;
        em.enabled = false;
        if (GasJet.isPlaying)
        {
            AudioManager.Instance?.StopPlayerJet();
        }
        GasJet.Stop();
    }
    private void StartPropel(InputAction.CallbackContext context)
    {
        if (isPropelling) return;

        if (hasWonGame)
            return;
        if (GravityManager.Instance.SimulationFrozen())
            return;


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
        /*if (pInput!=null&&pInput.GetComponent<InputManager>().PlayerId != PlayerID)
        {
            DeregisterInputs();
            pInput = null;
            hasDeregistered = false;
            StartCoroutine(DelayedRegister());
        }*/
        Vector2 diff = MouseDirection();

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        GasJet.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);

    }
    private IEnumerator Propel(float interval)
    {
        int streak=0;
        while (isPropelling)
        {
            if (Gas > 0f&&!GravityManager.Instance.SimulationFrozen())
            {

                yield return new WaitForSeconds(interval);

                if (streak > 1)
                {

                    float expendedGas = Mathf.Max(1f, radius() + Gas) * PropulsionCost * interval * RadiusPropCostScale_Multiplier.Evaluate(radius() / 1000f);

                    Gas -= Mathf.Max(5,expendedGas * 0.75f*(1/ VelocityMagnitudePropForceDampenScale.Evaluate(rigidBody.velocity.sqrMagnitude / (PropVelMagnOffset * radius() * radius()))));

                    Vector2 propelDirection = MouseDirection();
                    /*Legacy Force
                    float propulsionForce = expendedGas * PropulsionForceScale;


                    rigidBody.velocity += (propelDirection * propulsionForce) / mass() * -1 * Mathf.Min(5.5f, Mathf.Max(1, (mass() / 50f)));
                    */

                    float dampener = 1;
                    dampener *= VelocityMagnitudePropForceDampenScale.Evaluate(rigidBody.velocity.sqrMagnitude / (PropVelMagnOffset * radius() * radius()));
                    dampener *= RadiusPropForceDampenScale.Evaluate(radius() / 1000f);
                    float propulsionForce = radius() * -PropulsionForceScale*dampener;
                    rigidBody.velocity += propelDirection * propulsionForce;

                    if (Gas > 0f)
                    {
                        CutsceneManager.Instance?.PlayerPropelled();
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
    public void Bonk(bool isLarger, bool isMicroscopic, bool isSlightlySmaller)
    {
        camLook?.inputManager?.BonkRumble(isLarger, isMicroscopic, isSlightlySmaller);
    }
    #region Shield
    private void StartShield(InputAction.CallbackContext context)
    {
        
        if (Shield == null) return;
        if (hasWonGame)
            return;
        if (GravityManager.Instance.SimulationFrozen())
            return;


        if (isShielding) return;


        if (Ice > 0f)
        {
            CutsceneManager.Instance?.PlayerShielded();
            AudioManager.Instance?.PlayerShieldUp();
        }

        isShielding = true;
        Shield.ShieldUp();
    }
    public float ShieldRadius()
    {
        return Shield.transform.lossyScale.x * Shield.GetComponent<CircleCollider2D>().radius*1.025f;
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

        shieldActivated = false;
        Shield.ShieldDown();
    }

    public override bool ShieldIsActive()
    {
        if (Shield == null) return false;

        return Shield.IsActive();
    }
    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if(!hasDeregistered)
            DeregisterInputs();
    }
}
