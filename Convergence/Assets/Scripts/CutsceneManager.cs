using DG.Tweening;
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

    [Header("Toasts")]
    public RectTransform Toast_GameStart;
    public RectTransform Toast_FirstIce;
    public RectTransform Toast_FirstGas;

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
    private bool hasSeenIce;
    private bool hasSeenGas;
    public void ElementConsumed(PixelManager.ElementType type)
    {
        if(!hasSeenIce&&type==PixelManager.ElementType.Ice)
        {
            LoadToast(0, Toast_FirstIce);
            hasSeenIce = true;
        }
        if (!hasSeenGas && type == PixelManager.ElementType.Gas)
        {
            LoadToast(0, Toast_FirstGas);
            hasSeenGas = true;
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
        LoadToast(4f,Toast_GameStart);
    }
    public IEnumerator DelayLoad(Cutscene c)
    {
        yield return new WaitUntil(CinematicBars.notCinematic);

        if (lastToast != null)
        {
            UnloadToast(lastToast);
        }
        c.gameObject.SetActive(true);
    }
    public void LoadCutscene(Cutscene c)
    {
        if (c != null)
        {
            if(lastCutscene!=null)
                StopCoroutine(lastCutscene);
            lastCutscene=StartCoroutine(DelayLoad(c));
        }
    }
    Coroutine lastCutscene;
    public IEnumerator DevolutionCheck()
    {//Ensure the player is devolving not just dead.

        yield return new WaitForSeconds(0.75f);

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


    RectTransform lastToast;
    Coroutine loadToast;

    private Tween taostTween;
    private Tween out_taostTween;
    private float toast_unload_duration = 1;
    private float toast_duration=5;
    public void LoadToast(float launch_delay,RectTransform toast)
    {
        if(loadToast!=null)
        {
            StopCoroutine(loadToast);
        }
        if(lastToast!=null)
        {
            UnloadToast(lastToast);
            loadToast=StartCoroutine(DelayLoadToast(Mathf.Max(launch_delay,toast_unload_duration), toast));
        }
        else
            loadToast = StartCoroutine(DelayLoadToast(launch_delay, toast));
    }

    public IEnumerator DelayLoadToast(float wait, RectTransform toast)
    {
        if(wait > 0)
            yield return new WaitForSeconds(wait);

        if(CinematicBars.isCinematic)
        {
            yield return new WaitUntil(CinematicBars.notCinematic);
        }
        //Load Toast
        lastToast = toast;
        taostTween?.Kill();
        taostTween = toast.DOLocalMoveX(764, toast_unload_duration);
        taostTween.Play();

        yield return new WaitForSeconds(toast_duration);
        UnloadToast(toast);

    }
    public void UnloadToast(RectTransform toast)
    {
        if(lastToast==toast)
        {
            lastToast = null;
        }
        //Unload Toast;
        out_taostTween?.Kill();
        out_taostTween = toast.DOLocalMoveX(1132, toast_unload_duration);
        out_taostTween.Play();
    }


    private void OnDestroy()
    {
        taostTween?.Kill();
        out_taostTween?.Kill();
    }
}
