using DG.Tweening;
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

    public bool OverideLookAt;
    public Vector2 OverideLookPos;
    public float OverideLookScale;

    private void Start()
    {
        orthoMultiplier = 1;
        cam = GetComponent<Camera>();
        if (camLooks == null)
        {
            camLooks = new List<CameraLook>();
        }
        camLooks.Add(this);
        maxViewedMass = float.MaxValue;
    }
    bool inWinState;
    bool freezeFollow;

    float maxViewedMass;

    public static bool allCamsSame()
    {
        PlayerPixelManager obj = null;
        bool setPlayer=false;
        foreach (CameraLook look in CameraLook.camLooks)
        {
            if (look != null&&look.gameObject.activeInHierarchy)
            {
                if (!setPlayer)
                {
                    obj = look.focusedPixel;
                    setPlayer = true;
                }
                else if (obj != look.focusedPixel)
                    return false;
            }
        }
        return true;
    }
    public IEnumerator DelayedFinalCameraSnap()
    {
        maxViewedMass = 1000;
        yield return new WaitUntil(allCamsSame);
        if (PlayerID == focusedPixel.PlayerID)
        {
            focusedPixel.rigidBody.mass = 20000;
            IndicatorManager.DisableAllIndicators();
            // yield return new WaitUntil(orthoUnchanged);
         //   yield return new WaitForSeconds(1.5f);
        }
       // else
            yield return new WaitForSeconds(0.5f);
        /*
        else
        {
            yield return new WaitUntil(orthoUnchanged);
            yield return new WaitForSeconds(1f);
        }*/
        float TemporthoSize = cam.orthographicSize;
        cam.orthographicSize =Mathf.Min(maxViewedMass, 2001.499f);//Max Value
        freezeFollow = true;
        PauseMenu.Instance.UpdateHud(PlayerID, false);
        inputManager.hideOverlay = true;
        cam.cullingMask |= LayerMask.GetMask("P1");
        cam.cullingMask |= LayerMask.GetMask("P2");
        cam.cullingMask |= LayerMask.GetMask("P3");
        cam.cullingMask |= LayerMask.GetMask("P4");


        Vector2 targPos = cam.transform.position;
        Vector2 RtargSize = Vector2.zero;
        Vector2 RtargPos = Vector2.one;
        bool lerpRect=false;
        switch (GravityManager.Instance.PlayerCount)
        {
            case 1:
                targPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, -10));
                lerpRect = true;
                break;
            case 2:
                if (PlayerID == 1)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 0.5f, -10));
                else if (PlayerID == 2)
                    targPos = cam.ViewportToWorldPoint(new Vector3(1, 0.5f, -10));

                break;
            case 3:
                if (PlayerID == 1)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 1, -10));
                else if (PlayerID == 2)
                {
                    Rect r = cam.rect;

                    RtargSize = new Vector2(0.5f, 0);
                    RtargPos = new Vector2(0.5f, 1);

                    cam.orthographicSize *= 2;
                    cam.rect = new Rect(RtargSize, RtargPos);

                    targPos = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, -10));

                    cam.orthographicSize /=2f;
                    cam.rect = r;
                    orthoMultiplier =2;

                    lerpRect = true;
                }
                else if (PlayerID == 3)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 0, -10));

                break;
            case 4:
                if (PlayerID == 1)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 1, -10));
                else if (PlayerID == 2)
                    targPos = cam.ViewportToWorldPoint(new Vector3(1,1, -10));
                else if (PlayerID == 3)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 0, -10));
                else if (PlayerID == 4)
                    targPos = cam.ViewportToWorldPoint(new Vector3(1, 0, -10));

                break;
        }
        cam.orthographicSize = TemporthoSize;
        for (int i =0;i<100;i++)
        {
            yield return new WaitForFixedUpdate();
            cam.transform.position = Vector2.Lerp(cam.transform.position,targPos,0.05f);
            if(lerpRect)
            {
                cam.rect = new Rect(Vector2.Lerp(cam.rect.position, RtargSize, 0.05f), Vector2.Lerp(cam.rect.size, RtargPos, 0.05f));
            }
        }

        cam.transform.position = targPos;
        if (lerpRect)
            cam.rect = new Rect(RtargSize, RtargPos);
        // if (PlayerID == focusedPixel.PlayerID)
        // {
        //     cam.depth += 5;
        //}

        //cam.rect = new Rect(Vector2.zero, Vector2.one);

        finalizedMergedCam = true;

    }
    bool finalizedMergedCam;

    public void MergeCam()
    {
        float TemporthoSize = cam.orthographicSize;
        Vector3 Temppos = transform.position;
        if(focusedPixel!=null)
            transform.position = new Vector3(focusedPixel.transform.position.x, focusedPixel.transform.position.y, transform.position.z);

        cam.orthographicSize = Mathf.Min(maxViewedMass,2001.499f);//Max Value
        Vector2 targPos = cam.transform.position;
        Vector2 RtargSize = Vector2.zero;
        Vector2 RtargPos = Vector2.one;
        bool lerpRect = false;
        switch (GravityManager.Instance.PlayerCount)
        {
            case 1:
                targPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, -10));
                lerpRect = true;
                break;
            case 2:
                if (PlayerID == 1)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 0.5f, -10));
                else if (PlayerID == 2)
                    targPos = cam.ViewportToWorldPoint(new Vector3(1, 0.5f, -10));

                break;
            case 3:
                if (PlayerID == 1)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 1, -10));
                else if (PlayerID == 2)
                {
                    Rect r = cam.rect;

                    RtargSize = new Vector2(0.5f, 0);
                    RtargPos = new Vector2(0.5f, 1);

                    cam.orthographicSize *= 2;
                    cam.rect = new Rect(RtargSize, RtargPos);

                    targPos = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, -10));

                    cam.orthographicSize /= 2f;
                    cam.rect = r;
                    orthoMultiplier = 2;

                    lerpRect = true;
                }
                else if (PlayerID == 3)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 0, -10));

                break;
            case 4:
                if (PlayerID == 1)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 1, -10));
                else if (PlayerID == 2)
                    targPos = cam.ViewportToWorldPoint(new Vector3(1, 1, -10));
                else if (PlayerID == 3)
                    targPos = cam.ViewportToWorldPoint(new Vector3(0, 0, -10));
                else if (PlayerID == 4)
                    targPos = cam.ViewportToWorldPoint(new Vector3(1, 0, -10));

                break;
        }


        transform.position = Temppos;
        cam.orthographicSize = TemporthoSize;

        cam.transform.position = targPos;
        if (lerpRect)
            cam.rect = new Rect(RtargSize, RtargPos);
    }
    bool orthoUnchanged()
    {
        return Time.timeSinceLevelLoad > lastOrthoChange;
    }
    float orthoMultiplier=1;
    float lastOrthoSize;
    float lastOrthoChange;
    private void FixedUpdate()
    {
        if (OverideLookAt)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(OverideLookPos.x, OverideLookPos.y, transform.position.z), 0.15f);
            var newSize = Vector2.Lerp(new Vector2(cam.orthographicSize, 0),new Vector2(OverideLookScale, 0), 0.1f).x;
            if (camLooks.Count <= 1) //less than or equal to 1 player camera (no multiplayer cams)
            {
                var ppCam = Camera.main.GetUniversalAdditionalCameraData();
                if (ppCam.cameraStack.Count > 0)
                {
                    ppCam.cameraStack[0].orthographicSize = newSize;
                }
            }
            cam.orthographicSize = newSize;


        }
        else if (focusedPixel != null)
        {
            if(focusedPixel.playerPixel!=null&&focusedPixel.playerPixel.hasWonGame&&!inWinState&&GravityManager.Instance.isMultiplayer)
            {
                AudioManager.Instance?.PlayerWinSucceedSFX();
                inWinState = true;
                StartCoroutine(DelayedFinalCameraSnap());
            }
            if (!freezeFollow)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(focusedPixel.transform.position.x, focusedPixel.transform.position.y, transform.position.z),0.5f);
            }
            if(finalizedMergedCam)
            {
                MergeCam();
            }
            cam.orthographicSize =  UpdateCamSize(); //Vector2.Lerp(new Vector2(cam.orthographicSize,0),new Vector2(50 + focusedPixel.transform.localScale.x * 1.5f,0),0.1f).x;
            if(lastOrthoSize!=cam.orthographicSize)
            {
                lastOrthoChange = Time.timeSinceLevelLoad + 0.1f;
                lastOrthoSize = cam.orthographicSize;
            }
        
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
        float minRadius= 1;
        if (GravityManager.Instance.isMultiplayer)
            minRadius=75;
        var newSize = Vector2.Lerp(new Vector2(cam.orthographicSize, 0), orthoMultiplier * new Vector2(Mathf.Min(maxViewedMass, Mathf.Max(minRadius,(50 + focusedPixel.transform.localScale.x * 1.5f))), 0), 0.1f).x;
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
