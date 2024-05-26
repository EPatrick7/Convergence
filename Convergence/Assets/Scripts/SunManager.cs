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
    private TextMeshProUGUI title;
    private Image icon;
    [SerializeField]
    private InputManager inputManager;

    private bool multiplayer = false;

    [SerializeField]
    private VolumeProfile ppVol;

    private DepthOfField dOF;

    public Selectable ExitOptions;
    public Selectable Options;

    //private Camera camera;

    private Tween TcamSize;
    private Tween Ttext;
    private Tween TMtext;
    private Tween Tcam;
    private Tween opTween;
    private Tween mainTween;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        ps = GetComponentInChildren<ParticleSystem>();
        ps.Stop();
        icon = title.GetComponentInChildren<Image>();

        opUI.alpha = 0;
        opUI.interactable = false;
        //camera = Camera.main;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
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

        FadeOutMainButtons();

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
        camShake.Insert(6, Camera.main.DOOrthoSize(1, 1)); //quick zoom in before
        camShake.Play();
    }

    public void tutorialStart()
	{
        FadeOutMainButtons();
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

    public void optionStart()
	{
        FadeOutMainButtons();
        FadeInOptions();

        if (EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(Options.gameObject);

        Tcam?.Kill();
        Tcam = Camera.main.transform.DOMoveX(300, 2);
        Tcam.Play();
	}

	public void optionBack()
	{
		FadeOutOptions();
		FadeInMainButtons();
        if(EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(ExitOptions.gameObject);
        Tcam?.Kill();
        Tcam = Camera.main.transform.DOMoveX(-300, 2);
        Tcam.Play();
	}

    private void DisableButton(TextMeshProUGUI button)
	{
        button.gameObject.GetComponent<Button>().enabled = false;
	}

    private void EnableButton(TextMeshProUGUI button)
    {
        button.gameObject.GetComponent<Button>().enabled = true;
    }

    private void FadeOutMainButtons()
	{
        mainUI.interactable = false;
        float alpha = 1;
        mainTween?.Kill();
        mainTween = DOTween.To(()=> alpha, x => alpha = x, 0, 1).OnUpdate(() =>
        {
            mainUI.alpha = alpha;
        });
        mainTween.Play();
    }

    private void FadeInMainButtons()
	{
        mainUI.interactable = true;
        float alpha = 0;
        mainTween?.Kill();
        mainTween = DOTween.To(() => alpha, x => alpha = x, 1, 1).OnUpdate(() =>
        {
            mainUI.alpha = alpha;
        });
        mainTween.Play();
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
        opTween.Play();
    }

    private void FadeOutOptions()
	{
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
        DOTween.KillAll();
        if (ppVol.TryGet<DepthOfField>(out dOF))
        {
            dOF.active = false;
            dOF.focusDistance.value = 3f;
        }
    }

}
