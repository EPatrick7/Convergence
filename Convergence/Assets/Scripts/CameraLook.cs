using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraLook : MonoBehaviour
{
    public InputManager inputManager;
    public static List<CameraLook> camLooks;
    [Range(1,4)]
    public int PlayerID;
    public PlayerPixelManager focusedPixel;
    [HideInInspector]
    public PlayerRespawner respawner;
    Camera cam;
    [HideInInspector]
    public int LastNumPixelsInView;
    [HideInInspector]
    public int NumPixelsInView;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (camLooks == null)
        {
            camLooks = new List<CameraLook>();
        }
        camLooks.Add(this);
    }
    bool inWinState;
    public IEnumerator DelayedFinalCameraSnap()
    {
       // PauseMenu.Instance.UpdateHud(PlayerID, false);

        yield return new WaitForSeconds(10f);
       // cam.depth -=100;
       // if (PlayerID == focusedPixel.PlayerID)
       // {
       //     cam.depth += 5;
        //}
        
      //  cam.rect = new Rect(Vector2.zero, Vector2.one);
        

        
    }
    private void FixedUpdate()
    {
        if (focusedPixel != null)
        {
            if(focusedPixel.playerPixel.hasWonGame&&!inWinState&&GravityManager.Instance.isMultiplayer)
            {
                inWinState = true;
                StartCoroutine(DelayedFinalCameraSnap());
            }

            transform.position = new Vector3(focusedPixel.transform.position.x, focusedPixel.transform.position.y, transform.position.z);

            cam.orthographicSize = UpdateCamSize(); //Vector2.Lerp(new Vector2(cam.orthographicSize,0),new Vector2(50 + focusedPixel.transform.localScale.x * 1.5f,0),0.1f).x;
        }
        else if (respawner != null)
        {
            if(respawner.LerpNow)
            {

                transform.position = Vector3.Lerp(transform.position, new Vector3(respawner.transform.position.x, respawner.transform.position.y, transform.position.z),0.15f);
            }
        }
    }

    private float UpdateCamSize()
    {
        var newSize = Vector2.Lerp(new Vector2(cam.orthographicSize, 0), new Vector2(50 + focusedPixel.transform.localScale.x * 1.5f, 0), 0.1f).x;
        if (camLooks.Count <= 1) //less than or equal to 1 player camera (no multiplayer cams)
		{
            var ppCam = Camera.main.GetUniversalAdditionalCameraData();
            if (ppCam.cameraStack.Count > 0)
            {
                ppCam.cameraStack[0].orthographicSize = newSize;
            }
        }
        
        return newSize;
    }

    private void OnDestroy()
    {
        camLooks.Remove(this);
    }
}
