using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.Windows;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    public static List<InputManager> inputManagers;
    [HideInInspector]
    public PlayerKillNotifier killNotifier;

    public Action<bool> PlayerSet;

    public Action<bool> UISet;
    [HideInInspector]
    public PlayerInput playerInput;
    public static bool GamePadDetected;
    [Range(1,4)]
    public int PlayerId;
    public bool ShouldColorPlayer;
    public Color[] PlayerColors;
    private void Awake()
    {
        if (inputManagers == null)
        {
            inputManagers = new List<InputManager>();
        }
        playerInput = GetComponent<PlayerInput>();
        inputManagers.Add(this);
        SetPlayerInput(true);

        if (GamePadDetected)
        {
            Gamepad.current.ResetHaptics();
        }
    }

    public static InputManager GetManager(int id)
    {
        if (inputManagers == null || inputManagers.Count <= 0) return null;

        foreach (InputManager manager in inputManagers)
            if (manager.PlayerId == id) return manager;

        return null;
    }
    public void SetRumble(float min,float max, Color col)
    {
        if(playerInput.devices.Count<=0)
        {
            return;
        }

        //var dev = playerInput.devices[0];
        foreach (InputDevice dev in playerInput.devices)
        { 
            if (dev.GetType().ToString().Contains("Gamepad"))
            {
                Gamepad devpad = (Gamepad)dev;

                if (dev.GetType().ToString().Contains("DualShock4"))
                {
                    DualShock4GamepadHID devshock = (DualShock4GamepadHID)dev;

                    devshock.SetMotorSpeedsAndLightBarColor(min, max, col);
                }
                else
                    devpad.SetMotorSpeeds(min, max);
            }
        }
    }
    private void OnApplicationQuit()
    {
        SetRumble(0, 0, Color.clear);
    }

    public void SetRumble(float min,float max)
    {

        if (ShouldColorPlayer)
        {
            if (PlayerId <= 0)
            {
                Debug.LogError("Player ID unset, cannot rumble!");
            }
            SetRumble(min, max, PlayerColors[PlayerId-1]);
        }
        else
            SetRumble(min, max, Color.clear);
    }
    public void BonkRumble(bool isLarger,bool isMicroscopic,bool isSlightlySmaller)
    {
        //isLarger = true if player just hit something bigger than them.
        //isMicroscopic=true if player just hit something waay smaller than them.
        if (isLarger)
        {
            RumbleAmount += LargeCollisionRumble;
        }
        else if (isSlightlySmaller)
        {
            LilRumbleContribution += MedCollisionRumble;
        }
        else if(!isMicroscopic)
        {
            LilRumbleContribution += SmallCollisionRumble;
        }
        else
            LilRumbleContribution += MicroCollisionRumble;

    }
    public void EjectRumble()
    {
        RumbleAmount += ejectRumble;
    }
    public void AmbientRumble(PixelManager.PlanetType planetType)
    {
        if(planetType==PixelManager.PlanetType.Sun)
        {
            LilRumbleContribution2 += SunRumble;
        }
        else if (planetType == PixelManager.PlanetType.BlackHole)
        {
            LilRumbleContribution2 += BlackHoleRumble;
        }
    }
    float LilRumbleContribution;
    float LilRumbleContribution2;

    public void RespawnRumble()
    {
        RumbleAmount += respawnRumble;
    }
    public void PropelRumble()
    {

        RumbleAmount += propelRumble;
    }
    public void AddRumble(float amount)
    {
        RumbleAmount += amount;
    }
    [Header("Rumble (Controller)")]
    public float RumbleAmount = 0;
    public float MaxCollisionRumble = 0.2f;
    public float MedCollisionRumble = 0.1f;
    public float SmallCollisionRumble = 0.05f;
    public float LargeCollisionRumble = 1f;
    public float MicroCollisionRumble = 0;

    public float MaxAmbientRumble = 0.2f;
    public float SunRumble = 0.02f;
    public float BlackHoleRumble = 0.01f;

    public float ejectRumble = 0.2f;
    public float propelRumble = 0.02f;

    public float respawnRumble = 0.05f;

    public float MaxRumble = 10;
    public float RumbleScalar=1;
    public Vector2 PitchScalar=Vector2.one;


    private void FixedUpdate()
    {
        RumbleAmount += Mathf.Min(LilRumbleContribution, MaxCollisionRumble);
        RumbleAmount += Mathf.Min(LilRumbleContribution2, MaxAmbientRumble);
        LilRumbleContribution2 = 0;
        LilRumbleContribution = 0;
        RumbleAmount = Mathf.Min(RumbleAmount, MaxRumble);
        SetRumble(RumbleAmount* PitchScalar.x*RumbleScalar, RumbleAmount*RumbleScalar*PitchScalar.y);
        RumbleAmount /= 3f;

        if (playerInput.currentControlScheme == "Gamepad")
        {
            GamePadDetected = true;
        }


        if(killNotifier==null&&GravityManager.Instance.PlayerCount>1)
        {
            killNotifier = PlayerKillNotifier.GetNotifier(PlayerId);
        }
        if(killNotifier!=null)
        {
            killNotifier.overlay.gameObject.SetActive(playerInput.devices.Count<=0);
        }

     //   DeviceCount = playerInput.devices.Count;
    }
    public void SetPlayerInput(bool enabled)
    {

        if (enabled)
        {
            playerInput.actions.FindActionMap("Player").Enable();
        }
        else
        {
            playerInput.actions.FindActionMap("Player").Disable();
        }

        PlayerSet?.Invoke(enabled);
    }

    public void SetUIInput(bool enabled)
    {
        
        if (enabled)
            playerInput.actions.FindActionMap("UI").Enable();
        else
            playerInput.actions.FindActionMap("UI").Disable();

        UISet?.Invoke(enabled);
    }
    private void OnDestroy()
    {
        //SetRumble(0, 0, Color.clear);
        if (PauseMenu.Instance!=null&&!PauseMenu.Instance.hasDeregistered)
        {
            PauseMenu.Instance.DeRegisterInputs();
        }
        foreach(PlayerPixelManager p in GameObject.FindObjectsOfType<PlayerPixelManager>())
        {
            if(p!=null&&!p.hasDeregistered)
            {
                p.DeregisterInputs();
            }
        }
        inputManagers.Remove(this);
      //  GamePadDetected = false;


    }
}
