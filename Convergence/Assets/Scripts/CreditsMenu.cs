using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsMenu : MonoBehaviour
{
    public static CreditsMenu Instance { get; private set; }
    public float CreditsDelay = 10;

    Tween creditsTween;
    private void Start()
    {
        Instance = this;
    }
    private void OnDestroy()
    {
        creditsTween?.Kill();
        Instance = null;
    }
    bool creditsRolling;
    bool creditsDelayed;
    public void DelayRollCredits()
    {
        if (creditsRolling)
            return;

        creditsRolling = true;
        StartCoroutine(RollDelay(CreditsDelay));

    }
    private void OnEnable()
    {
        if(creditsRolling&&!creditsDelayed)
        {
            //If pause menu interrupted credits roll.
            creditsDelayed = true;
            RollCredits();
        }
    }
    public IEnumerator RollDelay(float delay)
    {
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(CinematicBars.notCinematic);
        yield return new WaitForSeconds(delay);
        AudioManager.Instance?.PlayerWinSucceedSFX();
        creditsDelayed = true;
        RollCredits();
    }

    public RectTransform CreditsHolder;
    private void RollCredits()
    {
        CreditsHolder.GetComponent<CanvasGroup>().alpha = 0;

        CreditsHolder.gameObject.SetActive(true);



        creditsTween?.Kill();
        creditsTween = CreditsHolder.GetComponent<CanvasGroup>().DOFade(1, 0.5f);
        creditsTween?.Play();
    }
}
