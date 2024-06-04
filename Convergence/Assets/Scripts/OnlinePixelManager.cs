using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OnlinePixelManager : MonoBehaviour
{
    PhotonView view;
    bool isMine;
    PlayerPixelManager pixelManager;
    private void OnEnable()
    {
        
        view = GetComponent<PhotonView>();
        isMine = view.IsMine;

        pixelManager = GetComponent<PlayerPixelManager>();
        pixelManager.PlayerID = view.CreatorActorNr;
        if (isMine)
        {
            foreach (CameraLook look in CameraLook.camLooks)
            {
                look.PlayerID = pixelManager.PlayerID;
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


        Debug.Log("Activated Player " + pixelManager.PlayerID);
        GravityManager.Instance.PreRegister(transform);
    }

}
