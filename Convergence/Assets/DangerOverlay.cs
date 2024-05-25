using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DangerOverlay : MonoBehaviour
{

    [SerializeField]
    private PlayerHud hud;

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

    private void CheckDanger()
    {
        if (hud.GetPlayer() == null)
            return;
        if (hud.GetPlayer().inDanger)
        {
            tween?.Kill();
            var tempColor = dangerOverlay.color;
            tempColor.a = .2f;
            tween = dangerOverlay.DOColor(tempColor, .5f);
            tween.Play();
            //dangerOverlay.gameObject.SetActive(true);
        }
        else
        {
            //dangerOverlay.gameObject.SetActive(false);
            tween?.Kill();
            var tempColor = dangerOverlay.color;
            tempColor.a = 0;
            tween = dangerOverlay.DOColor(tempColor, .5f);
            tween.Play();
        }
    }
    private void OnDestroy()
    {
        tween?.Kill();
    }
}
