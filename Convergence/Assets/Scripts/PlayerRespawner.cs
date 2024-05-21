using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerRespawner : MonoBehaviour
{
    public static List<PlayerRespawner> playerRespawners;
    public int PlayerID;
    public GameObject PlayerObj;
    public float WaitDelay;
    public float SetMass;
    InputManager inputManager;
    [HideInInspector]
    public bool LerpNow;
    public bool RestartsScene;

    private void Start()
    {    
        if(playerRespawners== null)
            playerRespawners=new List<PlayerRespawner>();
        playerRespawners.Add(this);
        inputManager = InputManager.GetManager(PlayerID);

        StartCoroutine(DelaySpawn());   
    }
    private void OnDestroy()
    {
        playerRespawners.Remove(this);
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
        LerpNow = true;
        yield return new WaitForSeconds(WaitDelay / 4f);
        if (RestartsScene)
        {
            PauseMenu.Instance.Restart();
        }
        else
        {
            GameObject player = Instantiate(PlayerObj, transform.position, transform.rotation, transform.parent);
            GravityManager.Instance.RegisterBody(player, Vector2.zero);
            PlayerPixelManager playerPixelManager = player.GetComponent<PlayerPixelManager>();
            playerPixelManager.PlayerID = PlayerID;

            playerPixelManager.GetComponent<Rigidbody2D>().mass = Mathf.Max(SetMass, playerPixelManager.GetComponent<Rigidbody2D>().mass);


            playerPixelManager.Initialize();



            foreach (PlayerHud hud in PlayerHud.huds)
            {
                if (hud.PlayerID == PlayerID)
                {
                    hud.Initialize(playerPixelManager);
                    break;
                }
            }
        }
        Destroy(gameObject);
    }
}
