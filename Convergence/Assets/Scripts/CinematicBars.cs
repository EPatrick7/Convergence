using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CinematicBars : MonoBehaviour
{
    [SerializeField]
    private RectTransform topBar, bottomBar;

    private Tween topTween;

    private Tween bottomTween;

    private void Awake()
    {
        topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, 0f);
        bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, 0f);
    }

    public void EnterCinematic(float duration = 1f, float height = 150f)
    {
        topTween?.Kill();
        bottomTween?.Kill();

        topTween = topBar.DOSizeDelta(new Vector2(topBar.sizeDelta.x, height), duration);
        bottomTween = bottomBar.DOSizeDelta(new Vector2(bottomBar.sizeDelta.y, height), duration);

        topTween.Play();
        bottomTween.Play();

        InputManager.SetPlayerInput(false);
        InputManager.SetUIInput(true);
    }

    public void ExitCinematic(float duration = 1f)
    {
        topTween?.Kill();
        bottomTween?.Kill();

        topTween = topBar.DOSizeDelta(new Vector2(topBar.sizeDelta.x, 0f), duration);
        bottomTween = bottomBar.DOSizeDelta(new Vector2(bottomBar.sizeDelta.y, 0f), duration);

        topTween.Play();
        bottomTween.Play();

        InputManager.SetPlayerInput(true);
        InputManager.SetUIInput(false);
    }

    private void OnDestroy()
    {
        topTween?.Kill();
        bottomTween?.Kill();
    }
}
