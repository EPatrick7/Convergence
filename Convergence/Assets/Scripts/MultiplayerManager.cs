
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class MultiplayerManager : MonoBehaviour
{

    public GameObject PlayerObject;
 

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
              //  g.GetComponent<GravityManager>().Initialized += DoAddPlayer;
            }
            g.SetActive(true);
        }
        foreach (GameObject g in DisableOnStart)
        {
            g.SetActive(false);
        }
    }

        private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }
}
