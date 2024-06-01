using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : CutsceneManager
{
    public static TutorialManager instance;
    public CanvasGroup LoafGroup;

    public bool isLoafVisible()
    {
        return LoafGroup.alpha > 0;
    }
    [Header("Loafs")]
    public LoafManager Loaf_MassIntro;
    public LoafManager Loaf_IceIntro;
    public LoafManager Loaf_GasIntro;


    LoafManager Current_Loaf;

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
     
    }
    private void OnDestroy()
    {
        instance = null;
    }

}
