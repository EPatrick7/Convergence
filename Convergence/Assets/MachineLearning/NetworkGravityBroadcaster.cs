using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

#region NetworkDataStructure
[System.Serializable]
public struct NetworkedPlayerInputs
{
    //Normalized float2 of the direction we are aiming
    public float2 aimDirection;
    public bool justEjected;
    public bool isPropelling;
    public bool isShielding;

    public NetworkedPlayerInputs(Vector2 mouseDirection,bool justEjected,bool isPropelling,bool isShielding)
    {
        this.aimDirection = new float2(mouseDirection.normalized.x, mouseDirection.normalized.y);
        this.justEjected = justEjected;
        this.isPropelling = isPropelling;
        this.isShielding = isShielding;
    }
}

[System.Serializable]
public struct NetworkedPlayerGravityBody
{
    public float2 pos;
    public float2 vel;
    public float2 acc;

    public float mass;
    public float radius;

    public float gas;
    public float ice;

    public NetworkedPlayerInputs lastPlayerInputs;

    public NetworkedPlayerGravityBody(GravityBody body, PlayerPixelManager pixel)
    {
        pos = new float2(body.x, body.y);
        if (pixel.GetComponent<Rigidbody2D>() == null)
        {
            vel = new float2(0, 0);
        }
        else
        {
            vel = new float2(pixel.GetComponent<Rigidbody2D>().velocity.x, pixel.GetComponent<Rigidbody2D>().velocity.y);
        }
        acc = new float2(body.acceleration().x, body.acceleration().y);
        mass = body.mass;
        radius = body.radius;

        gas = body.elements[1];
        ice = body.elements[0];

        lastPlayerInputs = new NetworkedPlayerInputs(pixel.MouseDirection(), false, pixel.isPropelling&& pixel.Gas>0, pixel.isShielding && pixel.Ice>0);
    }
}


[System.Serializable]
public struct NetworkedGravityBody
{
    public float2 pos;
    public float2 vel;
    public float2 acc;

    public float mass;
    public float radius;

    public float gas;
    public float ice;

    public float distance;
    public NetworkedGravityBody(GravityBody body,GameObject pixel,float localDistance=float.MaxValue)
    {
        pos=new float2(body.x,body.y);
        vel = new float2(pixel.GetComponent<Rigidbody2D>().velocity.x, pixel.GetComponent<Rigidbody2D>().velocity.y);
        acc = new float2(body.acceleration().x, body.acceleration().y);
        mass = body.mass;
        radius = body.radius;

        gas = body.elements[1];
        ice = body.elements[0];
        distance = localDistance;
    }
}


[System.Serializable]
public class NetworkedGravityUniverse
{
    public List<NetworkedGravityBody> networkedGravityBodies;
    public NetworkedPlayerGravityBody localPlayer;
    public NetworkedGravityUniverse(GravityManager.GravUniverse gravUniverse)
    {
        networkedGravityBodies = new List<NetworkedGravityBody>();
        localPlayer = new NetworkedPlayerGravityBody();
        if (gravUniverse != null)
        {
            for (int i = 0; i < gravUniverse.bodies.Count; i++)
            {
                GravityBody body = gravUniverse.bodies[i];
                if (i < gravUniverse.pixels.Count)
                {
                    GameObject pixel = gravUniverse.pixels[i];

                    if (pixel != null&&pixel.GetComponent<Rigidbody2D>()!=null)
                    {
                        networkedGravityBodies.Add(new NetworkedGravityBody(body, pixel));
                    }
                }
            }
        }
    }
    public void registerBody(GravityBody body,PixelManager pixel,float distance)
    {
        if (pixel != null && pixel.GetComponent<Rigidbody2D>() != null)
        {
            networkedGravityBodies.Add(new NetworkedGravityBody(body, pixel.gameObject, distance));
        }
    }
    public void registerPlayerBody(GravityBody body, PlayerPixelManager pixel)
    {
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
        PlayerPixelManager player = PlayerPixelManager.MostRecentInstance;
        if (player != null)
        {
            if(currentInputs.justEjected)
            {
                player.Eject(currentInputs.aimDirection);
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
