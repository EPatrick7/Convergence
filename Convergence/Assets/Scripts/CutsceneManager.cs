using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public enum LimitStates{Normal,OnlyToasts,None};
    public LimitStates mode;
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
    public RectTransform Toast_Death;
    public RectTransform Toast_GameStartDelayed;
    public RectTransform Toast_GalaxyLost;

    [Tooltip("The localX where the toast moves when loaded in.")]
    public float Load_LocalMoveX = 764;
    [Tooltip("The localX where the toast moves when unloaded out.")]
    public float Unload_LocalMoveX = 1132;

    [Tooltip("How long a toast takes to move in/out of the screen.")]
    public float toast_unload_duration = 1;
    [Tooltip("How long a toast takes before loading out.")]
    public float toast_duration = 6;
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
    #region Triggers
    float lastDist=0;
    bool hasSeenLostSpace;
    public void DistToBlackHole(float dist)
    {
        if(dist< lastDist)
        {//Returning to central black hole
            if (lastToast != null && lastToast.gameObject.name == Toast_GalaxyLost.gameObject.name && (out_taostTween == null || !out_taostTween.IsActive()))
            {
                UnloadToast(lastToast);
            }
        }
        else if(dist>GravityManager.Instance.SpawnRadius&&!hasSeenLostSpace)
        {
            LoadToast(0, Toast_GalaxyLost);
            hasSeenLostSpace = true;
        }
        lastDist = dist;
    }
    public void PlayerPaused()
    {
        if (lastToast != null && lastToast.gameObject.name == Toast_Death.gameObject.name && (out_taostTween == null || !out_taostTween.IsActive()))
        {
            UnloadToast(lastToast);
        }
    }
    public void PlayerEjected()
    {
        if(lastToast!=null&&lastToast.gameObject.name== Toast_GameStart.gameObject.name&&!CinematicBars.isCinematic && (out_taostTween==null||!out_taostTween.IsActive()))
        {
            UnloadToast(lastToast);
        }
    }
    public void PlayerPropelled()
    {
        if (lastToast != null && lastToast.gameObject.name == Toast_FirstGas.gameObject.name && (out_taostTween == null || !out_taostTween.IsActive()))
        {
            UnloadToast(lastToast);
        }
    }
    public void PlayerShielded()
    {
        if (lastToast != null && lastToast.gameObject.name == Toast_FirstIce.gameObject.name&& (out_taostTween == null || !out_taostTween.IsActive()))
        {
            UnloadToast(lastToast);
        }
    }
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
    public void PlayerConsumed(PlayerPixelManager eater = null)
    {
        if (eater == null)
        {
            // Default Text
            OnPlayerDeath.SetText(OnPlayerDeath.captionText[UnityEngine.Random.Range(0, OnPlayerDeath.captionText.Length)]);
        }
        else
        {
            // Color of the player that consumed this player
            UnityEngine.Color color = InputManager.GetManager(eater.PlayerID).PlayerColors[eater.PlayerID];

            OnPlayerDeath.SetText(string.Format("<color=#{0}>Player #{1}</color> Killed You", UnityEngine.ColorUtility.ToHtmlStringRGBA(color), eater.PlayerID));
        }

        LoadCutscene(OnPlayerDeath);
        StartCoroutine(DelayToastDeath());
    }
    public IEnumerator DelayToastDeath()
    {
        ToastQueueFrozen = true;
        yield return new WaitForSeconds(2);
        if(!GravityManager.Instance.respawn_players)
            LoadToast(2f, Toast_Death);
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
        StartCoroutine(ReallyDelayedToast(Toast_GameStartDelayed));
    }
    #endregion
    public IEnumerator ReallyDelayedToast(RectTransform toast)
    {
        if (mode != LimitStates.OnlyToasts)
        {
            yield return new WaitForSeconds(15);
            yield return new WaitUntil(noToastRightNow);
        }
        yield return new WaitForSeconds(2);
        yield return new WaitUntil(noToastRightNow);
        LoadToast(1,toast);
    }
    bool noToastRightNow()
    {
        return lastToast == null&&CinematicBars.notCinematic() && !PauseMenu.isPaused;
    }
    //For Cutscenes:
    public IEnumerator DelayLoad(Cutscene c)
    {
        yield return new WaitUntil(CinematicBars.notCinematic);

        if (c!=GameStart&&lastToast != null)
        {
            //If cutscene isnt the opening cutscene then unload on start.
            UnloadToast(lastToast);
        }

        c.gameObject.SetActive(true);
    }
    public void LoadCutscene(Cutscene c)
    {
        if (mode==LimitStates.OnlyToasts||mode==LimitStates.None)
            return;
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

    public bool noToastLive()
    {//Returns true if there is no toast that is not unloading right now.
        return lastToast == null;
    }
    public void LoadToast(float launch_delay,RectTransform toast)
    {
        if (mode == LimitStates.None)
            return;
        if (toast == null)
            return;

        if(lastToast!=null)
        {//If there is a toast live already then just add this new one to the queue for later.
            StartCoroutine(ToastQueue(Mathf.Max(launch_delay,toast_unload_duration), toast));
        }
        else
            loadToast = StartCoroutine(DelayLoadToast(launch_delay, toast));
    }
    [HideInInspector]
    public bool ToastQueueFrozen;
    public IEnumerator ToastQueue(float delay, RectTransform toast)
    {
        yield return new WaitUntil(noToastLive);
        //Wait until the toast is clear and then run as normal.
        if(!ToastQueueFrozen||toast==Toast_Death)
            loadToast = StartCoroutine(DelayLoadToast(delay,toast));
    }
    public IEnumerator DelayLoadToast(float wait, RectTransform toast)
    {
        lastToast = toast;
        if(wait > 0)
            yield return new WaitForSeconds(wait);

        if(CinematicBars.isCinematic)
        {
            yield return new WaitUntil(CinematicBars.notCinematic);
        }
        //Load Toast
        taostTween?.Kill();
        taostTween = toast.DOLocalMoveX(764, toast_unload_duration);
        taostTween.Play();

        yield return new WaitForSeconds(toast_duration);
        StartCoroutine(DelayUnload(toast));

    }

    public void UnloadToast(RectTransform toast)
    {//Force unloads the toast right now.

        if (loadToast != null)
        {
            taostTween?.Kill();
            StopCoroutine(loadToast);
        }
        StartCoroutine(DelayUnload(toast));
    }
    public bool NoOutTween()
    {
        return out_taostTween == null || !out_taostTween.IsActive();
    }
    public IEnumerator DelayUnload(RectTransform toast)
    {
        yield return new WaitUntil(NoOutTween);
        //Unload Toast;
        out_taostTween?.Kill();
        out_taostTween = toast.DOLocalMoveX(1300, toast_unload_duration);
        out_taostTween.Play();
        yield return new WaitForSeconds(toast_unload_duration);

        if (lastToast == toast)
        {
            lastToast = null;
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        taostTween?.Kill();
        out_taostTween?.Kill();
    }
}
