using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

public class CinematicBars : MonoBehaviour
{
    public static CinematicBars Instance;
    public static bool isCinematic;
    [SerializeField]
    private RectTransform topBar, bottomBar;

    [SerializeField]
    private RectTransform playerHud;

    private Tween topTween;

    private Tween bottomTween;

    private Tween hudTween;

    public static bool notCinematic()
    {
        return !isCinematic;
    }
    private void Awake()
    {
        topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, 0f);
        bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, 0f);
        Instance = this;
        isCinematic = false;
    }
    
    public void EnterCinematic(float duration = 1f, float height = 150f)
    {
        isCinematic = true;
        topTween?.Kill();
        bottomTween?.Kill();
        hudTween?.Kill();

        topTween = topBar.DOSizeDelta(new Vector2(topBar.sizeDelta.x, height), duration);
        bottomTween = bottomBar.DOSizeDelta(new Vector2(bottomBar.sizeDelta.y, height), duration);

        hudTween = playerHud.DOLocalMoveY(-367, duration);

        topTween.Play();
        bottomTween.Play();
        hudTween.Play();

  //      InputManager.SetPlayerInput(false);
    //    InputManager.SetUIInput(true);
    }

    public void ExitCinematic(float duration = 1f)
    {
        topTween?.Kill();
        bottomTween?.Kill();
        hudTween?.Kill();

        topTween = topBar.DOSizeDelta(new Vector2(topBar.sizeDelta.x, 0f), duration);
        bottomTween = bottomBar.DOSizeDelta(new Vector2(bottomBar.sizeDelta.y, 0f), duration);
        hudTween = playerHud.DOLocalMoveY(-150, duration);

        topTween.Play();
        bottomTween.Play();
        hudTween.Play();

        //   InputManager.SetPlayerInput(true);
        //  InputManager.SetUIInput(false);
        StartCoroutine(DelayUncheck(duration));
    }
    public IEnumerator DelayUncheck(float duration)
    {
        yield return new WaitForSeconds(duration);
        isCinematic = false;
    }
    private void OnDestroy()
    {
        topTween?.Kill();
        bottomTween?.Kill();
        hudTween?.Kill();
    }
}
