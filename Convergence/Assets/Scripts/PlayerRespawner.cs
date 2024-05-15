using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawner : MonoBehaviour
{
    public int PlayerID;
    public GameObject PlayerObj;
    public float WaitDelay;
    private void Start()
    {
        StartCoroutine(DelaySpawn());   
    }
    IEnumerator DelaySpawn()
    {
        yield return new WaitForSeconds(WaitDelay);

        GameObject player = Instantiate(PlayerObj, transform.position, transform.rotation,transform.parent);
        GravityManager.Instance.RegisterBody(player, Vector2.zero);
        PlayerPixelManager playerPixelManager = player.GetComponent<PlayerPixelManager>();
        playerPixelManager.PlayerID = PlayerID;
        
        foreach(PlayerHud hud in PlayerHud.huds)
        {
            if(hud.PlayerID == PlayerID)
            {
                hud.Initialize(playerPixelManager);
                break;
            }
        }

        Destroy(gameObject);
    }
}
