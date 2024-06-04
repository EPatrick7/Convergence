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


        pixelManager.PlayerIcon.gameObject.layer = LayerMask.NameToLayer("P" + pixelManager.PlayerID);
        Debug.Log("Activated Player " + pixelManager.PlayerID);
        GravityManager.Instance.PreRegister(transform);
    }

}
