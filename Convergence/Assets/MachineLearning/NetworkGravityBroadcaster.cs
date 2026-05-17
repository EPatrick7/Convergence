using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

#region NetworkDataStructure

[System.Serializable]
public struct SerializableVector2
{
    public float x;
    public float y;
    public SerializableVector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public SerializableVector2(float2 v)
    {
        x = v.x;
        y = v.y;
    }
}

[System.Serializable]
public struct NetworkedPlayerInputs
{
    //Normalized float2 of the direction we are aiming
    public float aimAngle;
    public bool justEjected;
    public bool isPropelling;
    public bool isShielding;

    public bool wantsToRestartGame;

    public NetworkedPlayerInputs(float aimAngle, bool justEjected,bool isPropelling,bool isShielding,bool wantsToRestartGame)
    {
        this.aimAngle = aimAngle;
        this.justEjected = justEjected;
        this.isPropelling = isPropelling;
        this.isShielding = isShielding;
        this.wantsToRestartGame = wantsToRestartGame;
    }
}

[System.Serializable]
public struct NetworkedPlayerGravityBody
{
    public SerializableVector2 pos;
    public SerializableVector2 vel;
    public SerializableVector2 acc;

    public float mass;
    public float radius;

    public float gas;
    public float ice;
    public bool inDanger;

    public NetworkedPlayerInputs lastPlayerInputs;


    public NetworkedPlayerGravityBody(GravityBody body, PlayerPixelManager pixel)
    {
        pos = new SerializableVector2(body.x, body.y);
        if (pixel.GetComponent<Rigidbody2D>() == null)
        {
            vel = new SerializableVector2(0, 0);
        }
        else
        {
            vel = new SerializableVector2(pixel.GetComponent<Rigidbody2D>().velocity.x, pixel.GetComponent<Rigidbody2D>().velocity.y);
        }
        acc = new SerializableVector2(body.acceleration().x, body.acceleration().y);
        mass = body.mass;
        radius = pixel.transform.localScale.x;

        gas = body.elements[1];
        ice = body.elements[0];

        lastPlayerInputs = new NetworkedPlayerInputs(pixel.aimAngle, false, pixel.isPropelling&& pixel.Gas>0, pixel.isShielding && pixel.Ice>0,false);

        inDanger = pixel.inDanger;
    }
}


[System.Serializable]
public struct NetworkedGravityBody
{
    public SerializableVector2 pos;
    public SerializableVector2 vel;
    public SerializableVector2 acc;

    public float mass;
    public float radius;

    public float gas;
    public float ice;

    public float distance;
    public NetworkedGravityBody(GravityBody body,GameObject pixel,float localDistance=float.MaxValue)
    {
        pos=new SerializableVector2(body.x,body.y);
        vel = new SerializableVector2(pixel.GetComponent<Rigidbody2D>().velocity.x, pixel.GetComponent<Rigidbody2D>().velocity.y);
        acc = new SerializableVector2(body.acceleration().x, body.acceleration().y);
        mass = body.mass;
        radius = pixel.transform.localScale.x;

        gas = body.elements[1];
        ice = body.elements[0];
        distance = localDistance;
    }
}


[System.Serializable]
public class NetworkedGravityUniverse
{
    public NetworkedGravityBody[] networkedGravityBodies;
    public NetworkedPlayerGravityBody localPlayer;
    public NetworkedGravityUniverse(GravityManager.GravUniverse gravUniverse)
    {
        networkedGravityBodies = new NetworkedGravityBody[gravUniverse.bodies.Count];
        localPlayer = new NetworkedPlayerGravityBody();
    }
    public void registerPlayerBody(GravityBody body, PlayerPixelManager pixel)
    {
        pixel.DeregisterInputs();
        pixel.isAIPuppet = true;
        localPlayer = new NetworkedPlayerGravityBody(body, pixel);
    }
}
#endregion

[RequireComponent(typeof(GravityManager))]
public class NetworkGravityBroadcaster : MonoBehaviour
{
    private GravityManager m_GravityManager;
    public NetworkedGravityUniverse gravityUniverse;

    public enum SimulationMode {Realtime,Stepped}
    public SimulationMode simulationMode;



    [Header("Stepped Simulation Data")]
    public bool awaitingSimulation;
    public NetworkedPlayerInputs currentInputs;

    private void Start()
    {
        awaitingSimulation = true;
        m_GravityManager = GetComponent<GravityManager>();
    }
    public bool StepReady()
    {
        if (simulationMode == SimulationMode.Realtime)
            return true;

        return awaitingSimulation;
    }
    public void CompleteStep()
    {
        if (!awaitingSimulation)
            return;
        PlayerPixelManager player = PlayerPixelManager.MostRecentInstance;
        if (player != null)
        {
            player.aimAngle = currentInputs.aimAngle;
            if(currentInputs.justEjected)
            {
                player.Eject(player.MouseDirection());
            }
            if(currentInputs.isShielding)
            {
                player.StartShield();
            }
            else
            {
                player.CancelShield();
            }

            if (currentInputs.isPropelling)
            {
                player.StartPropel();
            }
            else
            {
                player.CancelPropel();
            }
        }
        

        awaitingSimulation = false;
        currentInputs = new NetworkedPlayerInputs();
    }

    public void UpdateData(NetworkedGravityUniverse newUniverse)
    {
        gravityUniverse = newUniverse;
    }
}
