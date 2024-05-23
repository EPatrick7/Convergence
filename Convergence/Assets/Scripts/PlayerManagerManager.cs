using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class PlayerManagerManager : MonoBehaviour
{
    [Range(1,4)]
    public int PlayerId;
    public CameraLook PCamera;
    public InputManager InputManager;
    public PlayerHud PlayerHud;
    public Image IslandBG;
    public PlayerKillNotifier PlayerKillNotifier;

    public GameObject EnableIf3P;

    public GameObject NextEnable;
    
    public void UpdateAll()
    {
        if(Application.isPlaying&& PlayerId>GravityManager.Instance.PlayerCount)
        {
            gameObject.SetActive(false);
            return;
        }

        Color targ = InputManager.PlayerColors[PlayerId - 1];
        Camera cam =PCamera.GetComponent<Camera>();

        switch (PlayerId)
        {
            case 1:
                cam.cullingMask = LayerMask.GetMask("Default", "TransparentFX", "Ignore Raycast", "Ignore Pixel", "Water", "UI", "Shield", "Black Hole", "Player","P2", "P3", "P4");
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
        Vector2 pos=Vector2.zero;
        Vector2 size = Vector2.one;
        switch(GravityManager.Instance.PlayerCount)
        {
            case 1:
                size.x = 0.5f;
                if(EnableIf3P!=null)
                {
                    EnableIf3P.GetComponent<Camera>().rect = new Rect(new Vector2(0.5f,0f),new Vector2(0.5f,1f));
                }
                break;
            case 2:
                if (PlayerId > 1)
                {
                    pos.x = 0.5f;
                }
                size.x = 0.5f;
                break;
            case 3:
                if (PlayerId % 2 == 0)
                {
                    pos.x = 0.5f;
                }

                if (PlayerId <= 2)
                {
                    pos.y = 0.5f;
                }
                size.x = 0.5f;
                size.y = 0.5f;
                
                break;
            case 4:
                if (PlayerId % 2 == 0)
                {
                    pos.x = 0.5f;
                }

                if (PlayerId <= 2)
                {
                    pos.y = 0.5f;
                }
                size.x = 0.5f;
                size.y = 0.5f;

                break;
        }

        cam.rect = new Rect(pos,size);
        IslandBG.color=new Color(targ.r,targ.g,targ.b,IslandBG.color.a);

        PCamera.PlayerID = PlayerId;/*
        if (PCamera.focusedPixel != null)
        {
            PCamera.focusedPixel.camLook = PCamera;
            PCamera.focusedPixel.pInput = InputManager.GetComponent<PlayerInput>();
            PCamera.focusedPixel.RegisterInputs();
        }*/
        InputManager.PlayerId=PlayerId;
        PlayerHud.PlayerID=PlayerId;
        PlayerKillNotifier.PlayerID = PlayerId;

        InputManager.gameObject.SetActive(true);

    }
    public IEnumerator DelayPauseSetup()
    {
        yield return new WaitForSeconds(0.1f);
        if(GravityManager.Instance.isMultiplayer)
            PauseMenu.Instance.RegisterInputs();
    }
    private void Start()
    {
        UpdateAll();
        if(PlayerId < GravityManager.Instance.PlayerCount)
        {
            if (NextEnable != null) 
                NextEnable.SetActive(true);
        }
        if (EnableIf3P != null)
        {
            StartCoroutine(DelayPauseSetup());
            EnableIf3P.SetActive(GravityManager.Instance.PlayerCount == 3|| GravityManager.Instance.PlayerCount == 1);
        }
    }
}
