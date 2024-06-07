
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class MultiplayerManager : MonoBehaviour
{

    public GameObject PlayerObject;
    #region Event IDs
    public const byte InitializeEvent = 0;

    public void SendInitEvent(int seed)
    {
        object[] content = new object[] {seed};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(InitializeEvent, content, raiseEventOptions, SendOptions.SendReliable);

    }

    public const byte PlayerEject = 1;

    public void SendPlayerEjectEvent(float mass, Vector2 pos,Vector2 velocity)
    {
        object[] content = new object[] { mass,pos,velocity};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others};
        PhotonNetwork.RaiseEvent(PlayerEject, content, raiseEventOptions, SendOptions.SendReliable);

    }
    public const byte PlayerUpdate= 2;
    private static readonly DateTime referencePoint = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void SendPlayerUpdateEvent(int playerID,Vector3 data,bool isPropelling,bool isShielding, float lastJetRot)
    {
        object[] content = new object[] {playerID, data,isPropelling,isShielding, lastJetRot, (DateTime.UtcNow- referencePoint).TotalSeconds };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(PlayerUpdate, content, raiseEventOptions, SendOptions.SendReliable);

    }
    public const byte PlayerDeath = 3;

    public void SendPlayerDeathEvent(int playerID)
    {
        object[] content = new object[] { playerID};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(PlayerDeath, content, raiseEventOptions, SendOptions.SendReliable);

    }
    public const byte BodyUpdate= 4;

    public void SendBodyUpdateEvent(OnlineBodyUpdate onlineBodyUpdate)
    {

        object[] content = new object[] { onlineBodyUpdate.id, onlineBodyUpdate.pos, onlineBodyUpdate.vel, onlineBodyUpdate.acc, onlineBodyUpdate.mass, onlineBodyUpdate.elements,(System.DateTime.Now- referencePoint).TotalSeconds};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(BodyUpdate, content, raiseEventOptions, SendOptions.SendUnreliable);

    }

    public const byte KillBody = 5;

    public void SendKillBodyEvent(int id)
    {

        object[] content = new object[] {id};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(KillBody, content, raiseEventOptions, SendOptions.SendReliable);

    }

    public const byte SpawnBody = 6;

    public void SendSpawnBodyEvent(Vector2 pos, Vector2 velocity,Vector2 elements)
    {
        object[] content = new object[] { pos, velocity,elements };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SpawnBody, content, raiseEventOptions, SendOptions.SendReliable);

    }
    #endregion

    public static MultiplayerManager Instance;
    private void Start()
    {
        Instance = this;
    }
    public GameObject[] EnableOnStart;
    public GameObject[] DisableOnStart;
    private void InitializeGame(int seed)
    {
        foreach (GameObject g in EnableOnStart)
        {
            if (g.GetComponent<GravityManager>()!=null)
            {
                g.GetComponent<GravityManager>().isOnline = true;
                g.GetComponent<GravityManager>().RandomSeed = seed;
                g.GetComponent<GravityManager>().Initialized += DoAddPlayer;
            }
            g.SetActive(true);
        }
        foreach (GameObject g in DisableOnStart)
        {
            g.SetActive(false);
        }
    }
    public void DoAddPlayer()
    {
        Debug.Log("Instantiating Player...");

        int id =PhotonNetwork.LocalPlayer.ActorNumber;

        GameObject g= PhotonNetwork.Instantiate("OnlinePixel", GravityManager.Instance.DesiredPlayerPos+ new Vector2(id*50,id*50), Quaternion.identity);
        
    }

    public void OnEvent(EventData photonEvent)
    {
        
        byte eventCode = photonEvent.Code;
       // Debug.Log(eventCode);
        object[] data;
        PlayerPixelManager player;
        switch (eventCode)
        {
            case InitializeEvent:

                data = (object[])photonEvent.CustomData;
                InitializeGame((int)data[0]);
                Debug.Log("Initializing Game {Seed=" + (int)data[0]+"}...");
                break;
            case PlayerEject:
                data = (object[])photonEvent.CustomData;
                Vector3 pos = (Vector2)data[1];
                GameObject pixel = Instantiate(GravityManager.Instance.Pixel, pos, GravityManager.Instance.Pixel.transform.rotation, transform.parent);
                pixel.GetComponent<Rigidbody2D>().mass = (float)data[0];
                pixel.transform.localScale = Vector3.one * pixel.GetComponent<PixelManager>().radius(pixel.GetComponent<Rigidbody2D>().mass);
                pixel.GetComponent<PixelManager>().Initialize();
                GravityManager.Instance.RegisterBody(pixel, (Vector2)data[2]);

                break;
            case PlayerUpdate:

                data = (object[])photonEvent.CustomData;

                player = OnlinePixelManager.FetchPlayer((int)data[0]);
                if(player!=null)
                {
                    player.GetComponent<OnlinePixelManager>().UpdateStats((Vector3)data[1], (bool)data[2], (bool)data[3], (float)data[4],(double)data[5]);
                }
                break;
            case PlayerDeath:

                data = (object[])photonEvent.CustomData;

                Debug.Log("Player " + (int)data[0]+" has died!");

                player = OnlinePixelManager.FetchPlayer((int)data[0]);
                if(player!=null)
                {
                    player.RunDeath();
                    Destroy(player.gameObject);
                }
                break;
            case BodyUpdate:

                data = (object[])photonEvent.CustomData;

                OnlineBodyUpdate inputBody=new OnlineBodyUpdate();
                inputBody.id = (int)data[0];
                inputBody.pos = (Vector2)data[1];
                inputBody.vel = (Vector2)data[2];
                inputBody.acc = (Vector2)data[3];
                inputBody.mass = (float) data[4];
                inputBody.elements = (Vector2) data[5];
                inputBody.time= (double)data[6];


                GravityManager.Instance?.AddUpdateToQueue(inputBody);

                break;
            case KillBody:

                data = (object[])photonEvent.CustomData;

                GravityManager.Instance.KillBody((int)data[0]);
                break;
            case SpawnBody:
                data = (object[])photonEvent.CustomData;

                GravityManager.Instance.RecieveSpawnEvent((Vector2)data[0], (Vector2)data[1], (Vector2)data[2]);

                break;

        }
    }

        private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }
}
