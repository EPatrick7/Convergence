using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;
    private GravityManager gravityManager;


    public Cutscene GameStart;
    public Cutscene OnStarDevolution;
    public Cutscene OnStarTransition;
    public Cutscene OnBlueStarTransition;
    public Cutscene OnBlackHoleTransition;
    public Cutscene OnGalacticBlackHoleConsumed;

    public Cutscene OnPlayerDeath;

    PlayerPixelManager player;
    void Awake()
    {
        Instance = this;
        gravityManager = FindObjectOfType<GravityManager>();

        if (gravityManager != null)
        {
            gravityManager.Initialized += Initialize;
        }
    }
    public void PlayerConsumed()
    {
        LoadCutscene(OnPlayerDeath);
    }
    public void IsBlueStar()
    {
        LoadCutscene(OnBlueStarTransition);
    }
    public void BlackHoleConsumed()
    {
        LoadCutscene(OnGalacticBlackHoleConsumed);
    }
    private void Initialize()
    {
        player = FindObjectOfType<PlayerPixelManager>();

        if (player != null)
        {
            player.PlanetTypeChanged += UpdatePlanetType;
        }
        LoadCutscene(GameStart);
    }
    public IEnumerator DelayLoad(Cutscene c)
    {
        yield return new WaitUntil(CinematicBars.notCinematic);
        c.gameObject.SetActive(true);
    }
    public void LoadCutscene(Cutscene c)
    {
        if (c != null)
        {
            StartCoroutine(DelayLoad(c));
        }
    }
    public IEnumerator DevolutionCheck()
    {//Ensure the player is devolving not just dead.

        yield return new WaitForSeconds(0.25f);

        if (player != null && player.planetType == PixelManager.PlanetType.Planet)
        {
            LoadCutscene(OnStarDevolution);
        }
    }
    public void UpdatePlanetType(PixelManager.PlanetType planetType,PixelManager.PlanetType lastPlanetType)
    {
        if(lastPlanetType==PixelManager.PlanetType.Planet && planetType ==PixelManager.PlanetType.Sun)
        {
            LoadCutscene(OnStarTransition);
        }
        else if (lastPlanetType == PixelManager.PlanetType.Sun && planetType == PixelManager.PlanetType.Planet)
        {
            StartCoroutine(DevolutionCheck());
        }
        else if (lastPlanetType == PixelManager.PlanetType.Sun && planetType == PixelManager.PlanetType.BlackHole)
        {
            LoadCutscene(OnBlackHoleTransition);
        }
    }
}
