using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [HideInInspector]
    public PlayerPixelManager playerPixelManager;
    Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void FixedUpdate()
    {
        if (playerPixelManager != null)
        {
            transform.position = new Vector3(playerPixelManager.transform.position.x, playerPixelManager.transform.position.y, transform.position.z);

            cam.orthographicSize = Vector2.Lerp(new Vector2(cam.orthographicSize,0),new Vector2(50 + playerPixelManager.transform.localScale.x * 1.5f,0),0.1f).x;
        }
    }
}
