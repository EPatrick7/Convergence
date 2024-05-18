using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPointer : MonoBehaviour
{
    PlayerPixelManager playerPixelManager;
    private void Start()
    {
        playerPixelManager=transform.parent.GetComponent<PlayerPixelManager>();
    }
    void Update()
    {
        if (playerPixelManager != null&& playerPixelManager.pInput!=null && playerPixelManager.pInput.devices.Count > 0)
        {
            Vector2 diff = playerPixelManager.MouseDirection();

            float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);
        }
    }
}
