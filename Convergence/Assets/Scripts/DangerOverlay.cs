using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DangerOverlay : MonoBehaviour
{

    [SerializeField]
    private PlayerHud hud;

    [SerializeField]
    private float maxAlpha, fadeTime;

    private Image dangerOverlay;


    private Tween tween;

    // Start is called before the first frame update
    void Start()
    {
        dangerOverlay = GetComponent<Image>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        CheckDanger();
    }

    private void FadeOutOverlay()
	{
        tween?.Kill();
        var tempColor = dangerOverlay.color;
        tempColor.a = 0;
        tween = dangerOverlay.DOColor(tempColor, fadeTime);
        tween.Play();
    }

    private void FadeInOverlay() 
    {
        tween?.Kill();
        var tempColor = dangerOverlay.color;
        tempColor.a = maxAlpha;
        tween = dangerOverlay.DOColor(tempColor, fadeTime);
        tween.Play();
    }

    private void CheckDanger()
    {
        if (hud.GetPlayer() == null)
        {
            if (dangerOverlay.color.a > 0)
            {
                FadeOutOverlay();
            }
            return;
        }
        if (hud.GetPlayer().inDanger)
        {
            //dangerOverlay.gameObject.SetActive(true);
            FadeInOverlay();
        }
        else
        {
            //dangerOverlay.gameObject.SetActive(false);
            FadeOutOverlay();
        }
    }
    private void OnDestroy()
    {
        tween?.Kill();
    }
}
