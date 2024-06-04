using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    public static string ROOMNAME = "CONVERGE";
    public TextMeshProUGUI StatusText;
    public Image StatusImage;
    bool wasConnected;
    bool inRoom;
    bool isStalled;
    bool isStarted;
    bool isHost;
    Room room;
    void Start()
    {
        Instance = this;
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRoom(ROOMNAME);
       
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(ROOMNAME);
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isStalled = true;
    }
    public override void OnJoinedRoom()
    {
        inRoom = true;
        room = PhotonNetwork.CurrentRoom;
        isHost =PhotonNetwork.IsMasterClient;
        Debug.Log("Sucessfully joined room '" + ROOMNAME+"' with "+PhotonNetwork.CurrentRoom.PlayerCount+" Player(s)");
        StartCoroutine(RoomLoop());
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Lost Connection: " + cause);
    }
    private void OnDestroy()
    {
        PhotonNetwork.Disconnect();
    }
    public IEnumerator RoomLoop()
    {
        while (PhotonNetwork.IsConnected)
        {
            if(!isStarted&&isHost&&room.PlayerCount>0)
            {
                SetUp();
            }
            yield return new WaitForEndOfFrame();




        }
    }

    public void SetUp()
    {
        isStarted = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;

        int seed = Random.Range(100, 100000);

        MultiplayerManager.Instance.SendInitEvent(seed);

        
    }

    public void UpdateStatusText()
    {
        if (!PhotonNetwork.IsConnected)
        {
            if (wasConnected)
            {
                StatusImage.color = Color.red;
                StatusText.text =  "<color=red>Disconnected";
                StatusText.gameObject.SetActive(true);
            }
            else
            {
                StatusImage.color = Time.timeSinceLevelLoad > 3 ? Color.red:Color.white;
                StatusText.text = Time.timeSinceLevelLoad > 3 ? "<color=red>Connection Failed" : "Connecting...";
            }
        }
        else
        {
            if (inRoom)
            {
                StatusImage.color =  Color.green;
                StatusText.text = isStarted?"<color=green> Starting...": "<color=green>Waiting For Players...";
            }
            else
            {

                wasConnected = true;
                StatusImage.color = isStalled ? Color.red : Color.gray;
                StatusText.text = isStalled ? "<color=red>Server Busy" : "Logging In...";
            }
        }
    }
    void Update()
    {
        UpdateStatusText();
    }
}
