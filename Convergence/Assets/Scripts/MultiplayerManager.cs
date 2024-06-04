using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance;
    private void Start()
    {
        Instance = this;
    }
    public GameObject[] EnableOnStart;
    public GameObject[] DisableOnStart;
    public void InitializeGame()
    {
        foreach (GameObject g in EnableOnStart)
        {
            g.SetActive(true);
        }
        foreach (GameObject g in DisableOnStart)
        {
            g.SetActive(false);
        }
    }




    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            InitializeGame();
        }
    }
}
