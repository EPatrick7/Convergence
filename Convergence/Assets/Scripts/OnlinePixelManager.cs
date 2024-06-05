using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public class OnlinePixelManager : MonoBehaviour
{
    public static List<OnlinePixelManager> onlinePixels;
    bool isPropelling, isShielding;

    public void UpdateStats(Vector3 input,bool isPropelling,bool isShielding)
    {
        if(pixelManager!=null)
        {
            pixelManager.rigidBody.mass = input.x;
            pixelManager.Ice=input.y;
            pixelManager.Gas=input.z;

            this.isPropelling = isPropelling;
            this.isShielding = isShielding;
            UpdateVisibles();
            
        }
    }
    public void UpdateVisibles()
    {
        if (!isMine)
        {
            if (isPropelling && !pixelManager.GasJet.isPlaying)
            {
                pixelManager.GasJet.Play();
            }

            if (!isPropelling && pixelManager.GasJet.isPlaying)
            {
                pixelManager.GasJet.Stop();
            }

            if (!pixelManager.isShielding && isShielding)
                pixelManager.StartShield();
            if (pixelManager.isShielding && !isShielding)
                pixelManager.StartShield();
        }
    }
    public Vector3 FetchStats()
    {
        return new Vector3(pixelManager.rigidBody.mass, pixelManager.Ice, pixelManager.Gas);
    }

    float timeSinceLast;
    public void StatsChanged()
    {
            MultiplayerManager.Instance.SendUpdateEvent(pixelManager.PlayerID, FetchStats(),pixelManager.isPropelling,pixelManager.isShielding);
        
    }
    public static PlayerPixelManager FetchPlayer(int PlayerID)
    {
        if (onlinePixels == null)
            onlinePixels = new List<OnlinePixelManager>();
        foreach (OnlinePixelManager onlinePixel in onlinePixels)
        {
            if (onlinePixel.pixelManager.PlayerID == PlayerID)
                return onlinePixel.pixelManager;
        }
        return null;
    }
    PhotonView view;
    bool isMine;
    PlayerPixelManager pixelManager;
    private void OnDestroy()
    {

        if (onlinePixels == null)
            onlinePixels = new List<OnlinePixelManager>();
        onlinePixels.Remove(this);
    }
    private void OnEnable()
    {
        if(onlinePixels==null)
            onlinePixels=new List<OnlinePixelManager>();
        onlinePixels.Add(this);
        view = GetComponent<PhotonView>();
        isMine = view.IsMine;

        pixelManager = GetComponent<PlayerPixelManager>();
        pixelManager.PlayerID = view.CreatorActorNr;

        pixelManager.PlayerIcon.GetComponent<SpriteRenderer>().color = CameraLook.camLooks[0].inputManager.PlayerColors[pixelManager.PlayerID - 1];
        if (isMine)
        {
            foreach (CameraLook look in CameraLook.camLooks)
            {
                look.PlayerID = pixelManager.PlayerID;
                Camera cam = look.GetComponent<Camera>();

                switch (look.PlayerID)
                {
                    case 1:
                        cam.cullingMask = LayerMask.GetMask("Default", "TransparentFX", "Ignore Raycast", "Ignore Pixel", "Water", "UI", "Shield", "Black Hole", "Player", "P2", "P3", "P4");
                        break;
                    case 2:
                        cam.cullingMask = LayerMask.GetMask("Default", "TransparentFX", "Ignore Raycast", "Ignore Pixel", "Water", "UI", "Shield", "Black Hole", "Player", "P1", "P3", "P4");
                        break;
                    case 3:
                        cam.cullingMask = LayerMask.GetMask("Default", "TransparentFX", "Ignore Raycast", "Ignore Pixel", "Water", "UI", "Shield", "Black Hole", "Player", "P1", "P2", "P4");
                        break;
                    case 4:
                        cam.cullingMask = LayerMask.GetMask("Default", "TransparentFX", "Ignore Raycast", "Ignore Pixel", "Water", "UI", "Shield", "Black Hole", "Player", "P1", "P2", "P3");
                        break;
                }
            }
            foreach (PlayerHud hud in PlayerHud.huds)
            {
                hud.PlayerID = pixelManager.PlayerID;
            }
            foreach (InputManager manager in InputManager.inputManagers)
            {
                manager.PlayerId = pixelManager.PlayerID;
            }
        }
        else
        {
            GetComponent<Rigidbody2D>().isKinematic = true;
        }


        pixelManager.PlayerIcon.gameObject.layer = LayerMask.NameToLayer("P" + pixelManager.PlayerID);
        Debug.Log("Activated Player " + pixelManager.PlayerID);
        GravityManager.Instance.PreRegister(transform);
    }
    public void EjectPixel(float mass, Vector2 pos,Vector2 vel)
    {
        MultiplayerManager.Instance?.SendPlayerEjectEvent(mass, pos, vel);
    }

    private void FixedUpdate()
    {
        if (isMine&& Time.timeSinceLevelLoad > timeSinceLast)
        {
            timeSinceLast = Time.timeSinceLevelLoad + 0.5f;
            StatsChanged();
        }
        if(!isMine)
        {
            UpdateVisibles();
        }
    }
}
