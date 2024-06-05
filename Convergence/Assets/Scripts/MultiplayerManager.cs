
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void SendUpdateEvent(int playerID,Vector3 data,bool isPropelling,bool isShielding)
    {
        object[] content = new object[] {playerID, data,isPropelling,isShielding};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(PlayerUpdate, content, raiseEventOptions, SendOptions.SendReliable);

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

                PlayerPixelManager player = OnlinePixelManager.FetchPlayer((int)data[0]);
                if(player!=null)
                {
                    player.GetComponent<OnlinePixelManager>().UpdateStats((Vector3)data[1], (bool)data[2], (bool)data[3]);
                }
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
