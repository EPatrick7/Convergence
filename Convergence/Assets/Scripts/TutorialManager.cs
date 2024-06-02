using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : CutsceneManager
{
    public static TutorialManager instance;
    public CanvasGroup PuaseOverlayGroup;
    public CanvasGroup LoafGroup;

    public bool isLoafVisible()
    {
        return LoafGroup.alpha > 0;
    }
    [Header("Loafs")]
    public LoafManager Loaf_MassIntro;
    public LoafManager Loaf_IceIntro;
    public LoafManager Loaf_GasIntro;

    public GameObject LeftButton;
    public GameObject RightButton;
    public void UpdateButtons()
    {
        LeftButton.SetActive(LoafManager.SelectedLoafID > 0);
        RightButton.SetActive(LoafManager.SelectedLoafID <2);
    }
    public void NextLoaf()
    {
        LoafManager.UpdateSelectedLoafID(LoafManager.SelectedLoafID + 1);
    }
    public void PrevLoaf()
    {
        LoafManager.UpdateSelectedLoafID(LoafManager.SelectedLoafID - 1);
        
    }
    public void EnableAlpha()
    {
        PuaseOverlayGroup.alpha = 0;
    }
    public void DisableAlpha()
    {
        if (!TutorialLive)
        {
            PuaseOverlayGroup.alpha = 1;
        }
        else
        { PuaseOverlayGroup.alpha = 0; }
    }

    LoafManager Current_Loaf;

    [HideInInspector]
    public bool TutorialLive;

    [HideInInspector]
    public bool TutorialCleared;
    Tween loafTween;
    public void ActivateTutorialScene(TutorialScene scene)
    {
        AudioManager.Instance?.GeneralSelect();
        TutorialLive = true;
        scene.gameObject.SetActive(true);
        loafTween?.Kill();
        loafTween = LoafGroup.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, 1000), 0.5f);
        loafTween.onComplete += SetAlphaZero;
        loafTween.Play();

        GravityManager.Instance.UnfreezeSimulation();
    }
    public void SetAlphaZero()
    {
        loafTween?.Kill();
        loafTween = LoafGroup.DOFade(0, 0.5f);
        loafTween.Play();
    }

    #region OutLoad
    bool isOutloading;
    public void LoadOutTutorial()
    {
        if(!isOutloading)
        {
            isOutloading = true;
            StartCoroutine(LoadOutTutorialAnim());
        }
    }
    public IEnumerator LoadOutTutorialAnim()
    {
        foreach(CameraLook camLook in CameraLook.camLooks)
            camLook.OverideLookAt = true;
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(0);
    }
    #endregion
    private void Start()
    {
        instance = this;
        UpdateButtons();
    }
    private void OnDestroy()
    {
        
        loafTween?.Kill();
        instance = null;
    }

}
