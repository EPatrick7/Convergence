using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerRespawner : MonoBehaviour
{
    public int PlayerID;
    public GameObject PlayerObj;
    public float WaitDelay;
    InputManager inputManager;
    private void Start()
    {
        if(InputManager.inputManagers!=null&& inputManager==null)
        {
            foreach(InputManager i in InputManager.inputManagers)
            {
                if(i.PlayerId==PlayerID)
                {
                    inputManager = i;
                    break;
                }
            }
        }    
        StartCoroutine(DelaySpawn());   
    }
    public void FixedUpdate()
    {
        if(inputManager!=null&& playRumble)
        {
            inputManager.RespawnRumble();
        }
    }
    bool playRumble;
    IEnumerator DelaySpawn()
    {
        yield return new WaitForSeconds((3*WaitDelay)/4f);
        playRumble = true;

        yield return new WaitForSeconds(WaitDelay / 4f);
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
