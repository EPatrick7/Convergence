using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;

public class SunManager : MonoBehaviour
{

    [SerializeField]
    private float angleIncrement = 0;

    private float angle;

    private ParticleSystem ps;

    private SpriteRenderer sprite;

    [SerializeField]
    private CanvasGroup mainUI;
    
    [SerializeField]
    private CanvasGroup opUI;

    [SerializeField]
    private CanvasGroup credUI;

    [SerializeField]
    private TextMeshProUGUI title;
    private Image icon;
    [SerializeField]
    private InputManager inputManager;

    [SerializeField]
    private CanvasGroup skipUI;

    private bool multiplayer = false;

    [SerializeField]
    private VolumeProfile ppVol;

    private DepthOfField dOF;

    public Selectable ExitOptions;
    public Selectable Options;

    //private Camera camera;

    private Tween TcamSize, Ttext, TMtext, Tcam, opTween, mainTween, skipTween, creditsTween;
    public static bool OptionsOpen;

    public CanvasGroup onlineUI;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        ps = GetComponentInChildren<ParticleSystem>();
        ps.Stop();
        icon = title.GetComponentInChildren<Image>();

        opUI.alpha = 0;
        opUI.interactable = false;
        skipUI.alpha = 0;
        //camera = Camera.main;
    }

    float delayClickTime;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
            delayClickTime = Time.timeSinceLevelLoad + 0.1f;
        Transform pos = gameObject.transform;
        pos.eulerAngles = new Vector3(pos.eulerAngles.x, pos.eulerAngles.y, pos.eulerAngles.z + angleIncrement);
        angle += angleIncrement;

        if (isShaking)
        {
            inputManager.AddRumble(0.5f);
        }
    }
    bool isShaking = false;
    public float ShakeStartDelay;
    public float ShakeStopDelay;
    public float DispersePower = -600;
    public IEnumerator ShakeDelay()
    {
        yield return new WaitForSeconds(ShakeStartDelay);
        GravityManager.Instance.drift_power = DispersePower;
        isShaking = true;
        yield return new WaitForSeconds(ShakeStopDelay);
        isShaking = false;
    }

    public void sceneStart()
	{
        TcamSize?.Kill();
        StartCoroutine(ShakeDelay());

        FadeInSkipUI();

        FadeOutMainButtons(1f);

        Vector3 newPos = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, Camera.main.transform.position.z); //set z to 0 so no clipping out
        
        Color newTCol = title.color;
        newTCol.a = 0;

        Color newICol = icon.color;
        newICol.a = 0;

        Sequence cam = DOTween.Sequence();
        cam.Append(Camera.main.DOOrthoSize(1000, 2)); //zoom out
        cam.Insert(0, Camera.main.transform.DOMove(newPos, 2, true)); //center on sun
        cam.Insert(0, title.transform.DOMoveY(title.transform.position.y + 100, 2, true)); //move title up
        cam.OnComplete(StartPS);
        cam.Play();

        Sequence camShake = DOTween.Sequence();
        camShake.Insert(3, Camera.main.transform.DOShakePosition(15, 20, 20, 45, true, false)); //shake cam
        camShake.Insert(4, title.DOColor(newTCol, 1)); //fade out title
        camShake.Insert(4, icon.DOColor(newICol, 2)); //fade out icon
        camShake.Insert(4, skipUI.DOFade(0f, 1));
        camShake.Insert(6, Camera.main.DOOrthoSize(1, 1)); //quick zoom in before
        camShake.Play();
    }

    public void tutorialStart()
	{

        FadeInSkipUI();

        FadeOutMainButtons(1f);
        Vector3 newPos = new Vector3(gameObject.transform.position.x + Camera.main.pixelWidth/3f, gameObject.transform.position.y, Camera.main.transform.position.z); //set z to 0 so no clipping out

        if (ppVol.TryGet<DepthOfField>(out dOF))
		{
            dOF.active = true;
            float targetDist = dOF.focusDistance.value;
           // Debug.Log(targetDist);
            var tween = DOTween.To(() => targetDist, x => targetDist = x, .1f, 2f).OnUpdate(() =>
            {
                dOF.focusDistance.value = targetDist;
            });
            tween.Play();
		}

        Color newTCol = title.color;
        newTCol.a = 0;

        Color newICol = icon.color;
        newICol.a = 0;

        Sequence cam = DOTween.Sequence();
        cam.Append(Camera.main.DOOrthoSize(650, 3)); //zoom out
        cam.Insert(0, gameObject.transform.DOScale(500, 3));
        cam.Insert(0, Camera.main.transform.DOMove(newPos, 2, true)); //center on sun
        cam.Insert(0, title.transform.DOMoveY(title.transform.position.y + 100, 2, true)); //move title up
        cam.Insert(1, title.DOColor(newTCol, 1)); //fade out title
        cam.Insert(1, icon.DOColor(newICol, 2)); //fade out icon
        cam.Play();
    }

    public void onlineStart()
    {
        FadeOutOnline();
        sceneStart();

    }

    public void onlinePreMenu()
	{
        FadeInOnline();
        FadeOutMainButtons(.5f);
	}

    public void onlineBack()
	{
        FadeOutOnline();
        FadeInMainButtons();
	}

    /*
    public void multiStart()
	{
        TcamSize?.Kill();
        StartCoroutine(ShakeDelay());
        
        FadeOutMainButtons();

        Vector3 newPos = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, Camera.main.transform.position.z); //set z to 0 so no clipping out

        Color newTCol = title.color;
        newTCol.a = 0;

        Color newICol = icon.color;
        newICol.a = 0;

        Sequence cam = DOTween.Sequence();
        cam.Append(Camera.main.transform.DOMove(newPos, 2, true)); //center on sun
        cam.Insert(.5f, Camera.main.DOOrthoSize(10, 1)); //zoom in
        cam.Insert(0, title.transform.DOMoveY(title.transform.position.y + 100, 2, true)); //move title up
        cam.Insert(0, title.DOColor(newTCol, 1)); //fade out title
        cam.Insert(0, icon.DOColor(newICol, 2)); //fade out icon
        cam.Play();
    }
    */
    float timeOptionsOpened;
    float timeOptionsClosed;
    public void optionStart()
    {
        if (Time.timeSinceLevelLoad < timeOptionsClosed)
        {
            return;
        }

        OptionsOpen = true;
        FadeOutMainButtons(1f);
        FadeInOptions();

        if (Time.timeSinceLevelLoad < delayClickTime&&!InputManager.GamePadDetected)
            EventSystem.current.SetSelectedGameObject(null);
        else if (EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(Options.gameObject);

        Tcam?.Kill();
        Tcam = Camera.main.transform.DOMoveX(300, 2);
        Tcam.Play();

        timeOptionsOpened = Time.timeSinceLevelLoad+0.25f;

    }

	public void optionBack()
	{
        if(Time.timeSinceLevelLoad< timeOptionsOpened)
        {
            return;
        }
        OptionsOpen = false;

        FadeOutOptions();
		FadeInMainButtons();
        if (Time.timeSinceLevelLoad < delayClickTime&&!InputManager.GamePadDetected)
            EventSystem.current.SetSelectedGameObject(null);
        else if (EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(ExitOptions.gameObject);
        Tcam?.Kill();
        Tcam = Camera.main.transform.DOMoveX(-300, 2);
        Tcam.Play();


        timeOptionsClosed = Time.timeSinceLevelLoad + 0.25f;
    }

    private void FadeInSkipUI()
	{
        float alpha = 0;
        skipTween?.Kill();
        skipTween = DOTween.To(() => alpha, x => alpha = x, .5f, 2).OnUpdate(() =>
        {
            skipUI.alpha = alpha;
        });
        skipTween.Play();
    }

    private void DisableButton(TextMeshProUGUI button)
	{
        button.gameObject.GetComponent<Button>().enabled = false;
	}

    private void EnableButton(TextMeshProUGUI button)
    {
        button.gameObject.GetComponent<Button>().enabled = true;
    }

    private void FadeOutMainButtons(float fadeTime)
	{
        PauseMenu.Instance.isPressingFire = false;
        mainUI.interactable = false;
        //float alpha = 1;
        mainTween?.Kill();
        mainTween = mainUI.DOFade(0f, fadeTime);
        /*
        mainTween = DOTween.To(()=> alpha, x => alpha = x, 0, 1).OnUpdate(() =>
        {
            mainUI.alpha = alpha;
        });
        */
        mainTween.Play();
    }

    private void FadeInMainButtons()
	{
        //Don't need to comment out, cutscene skip works fine w/ disabled buttons
        //Disabling them so you cant spam click them when going to optiosn
        mainUI.interactable = true;
        //float alpha = 0;
        mainTween?.Kill();
        mainTween = mainUI.DOFade(1f, 1f);
        /*
        mainTween = DOTween.To(() => alpha, x => alpha = x, 1, 1).OnUpdate(() =>
        {
            mainUI.alpha = alpha;
        });
        */
        mainTween.Play();
    }

    public void FadeCredits()
    {
        creditsTween?.Kill();
        creditsTween = credUI.DOFade(1f, 1.5f);
        creditsTween.Play();

    }

    private void FadeInOnline()
    {
        //onlineUI.
        onlineUI.interactable = true;
        /*
        float alpha = 1;
        
        onlineTween = DOTween.To(() => alpha, x => alpha = x, 0, 1).OnUpdate(() =>
        {
            onlineUI.alpha = alpha;
        });
        */
        Sequence onlineTween = DOTween.Sequence();
        onlineTween.Append(onlineUI.DOFade(1f, 1f));
        onlineTween.Insert(0, onlineUI.gameObject.GetComponent<RectTransform>().DOLocalMoveX(0, 1f));
        onlineTween.Play();
    }

    private void FadeOutOnline()
    {
        onlineUI.interactable = false;
        /*
        float alpha = 0;
        onlineTween?.Kill();
        onlineTween = DOTween.To(() => alpha, x => alpha = x, 1, 1).OnUpdate(() =>
        {
            onlineUI.alpha = alpha;
        });
        */
        Sequence onlineTween = DOTween.Sequence();
        onlineTween.Append(onlineUI.DOFade(0f, .75f));
        onlineTween.Insert(0, onlineUI.gameObject.GetComponent<RectTransform>().DOLocalMoveX(-500, 1f));
        onlineTween.Play();
    }

    private void FadeInOptions()
	{
        opUI.interactable = true;
        float alpha = 0;
        opTween?.Kill();
        opTween = DOTween.To(() => alpha, x => alpha = x, 1, 1).OnUpdate(() =>
        {
            opUI.alpha = alpha;
        });
        opTween.onComplete += FadeCredits;
        opTween.Play();
    }

    private void FadeOutOptions()
    {
        creditsTween?.Kill();
        creditsTween = credUI.DOFade(0, 0.5f);
        creditsTween.Play();
        opUI.interactable = false;
        float alpha = 1;
        opTween?.Kill();
        opTween = DOTween.To(() => alpha, x => alpha = x, 0, 1).OnUpdate(() =>
        {
            opUI.alpha = alpha;
        });
        opTween.Play();
    }

    public void StartPS()
	{
        sprite.enabled = false;
        ps.Play();
	}

    void OnDestroy()
	{
        OptionsOpen = false;
        DOTween.KillAll();
        if (ppVol.TryGet<DepthOfField>(out dOF))
        {
            dOF.active = false;
            dOF.focusDistance.value = 3f;
        }
    }

}
