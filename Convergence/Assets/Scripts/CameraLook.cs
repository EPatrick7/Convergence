using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLook : MonoBehaviour
{
    public InputManager inputManager;
    public static List<CameraLook> camLooks;
    [Range(1,4)]
    public int PlayerID;
    [HideInInspector]
    public PlayerPixelManager focusedPixel;
    Camera cam;

    private void Start()
    {
        inputManager=GetComponentInChildren<InputManager>();
        inputManager.PlayerId = PlayerID;
        cam = GetComponent<Camera>();
        if (camLooks == null)
        {
            camLooks = new List<CameraLook>();
        }
        camLooks.Add(this);
    }

    private void FixedUpdate()
    {
        if (focusedPixel != null)
        {
            transform.position = new Vector3(focusedPixel.transform.position.x, focusedPixel.transform.position.y, transform.position.z);

            cam.orthographicSize = Vector2.Lerp(new Vector2(cam.orthographicSize,0),new Vector2(50 + focusedPixel.transform.localScale.x * 1.5f,0),0.1f).x;
        }
    }
    private void OnDestroy()
    {
        camLooks.Remove(this);
    }
}
