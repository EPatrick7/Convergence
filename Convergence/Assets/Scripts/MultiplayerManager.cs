
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

    public const byte AddPlayer = 1;

    public void SendBirthEvent(int playerId)
    {
        object[] content = new object[] { playerId };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All};
        PhotonNetwork.RaiseEvent(AddPlayer, content, raiseEventOptions, SendOptions.SendReliable);

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

        GameObject g= PhotonNetwork.Instantiate("OnlinePixel", GravityManager.Instance.DesiredPlayerPos+ new Vector2(id*10,id*10), Quaternion.identity);
        
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
            case AddPlayer:

               data = (object[])photonEvent.CustomData;
                //Dirty replace later:
               foreach(PlayerPixelManager p in FindObjectsOfType<PlayerPixelManager>())
                {
                    if(!p.isInitialized)
                    {
                        p.PlayerID = (int)data[0];
                        GravityManager.Instance.PreRegister(p.transform);
                        return;
                    }
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
