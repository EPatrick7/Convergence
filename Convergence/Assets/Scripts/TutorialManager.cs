using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TutorialManager : CutsceneManager
{
    public static TutorialManager instance;

    [Header("Loafs")]
    public LoafManager Loaf_MassIntro;
    public LoafManager Loaf_IceIntro;
    public LoafManager Loaf_GasIntro;


    LoafManager Current_Loaf;
    private void Start()
    {
        instance = this;
     
    }
    private void OnDestroy()
    {
        instance = null;
    }
    public void TriggerLoaf(LoafManager loaf,TutorialScene linked)
    {
        Current_Loaf = loaf;
        GravityManager.Instance.FreezeSimulation();
        LoadToast(0, loaf?.GetComponent<RectTransform>());
    }
    public void UnloadLoaf(LoafManager loaf)
    {
        GravityManager.Instance.UnfreezeSimulation();
        UnloadToast(loaf.GetComponent<RectTransform>());
        Current_Loaf = null;
    }

    //Called regardless of if the player sucessfully performed the action.
    public void Eject()
    {
        if(Current_Loaf != null&&Current_Loaf.disableTrigger==LoafManager.DisableTrigger.Eject)
        {
            UnloadLoaf(Current_Loaf);
        }
    }
    //Called regardless of if the player sucessfully performed the action.
    public void Propel()
    {

        if (Current_Loaf != null && Current_Loaf.disableTrigger == LoafManager.DisableTrigger.Propel)
        {
            UnloadLoaf(Current_Loaf);
        }
    }
    //Called regardless of if the player sucessfully performed the action.
    public void Shield()
    {

        if (Current_Loaf != null && Current_Loaf.disableTrigger == LoafManager.DisableTrigger.Shield)
        {
            UnloadLoaf(Current_Loaf);
        }
    }
    [HideInInspector]
    public bool hasBonked;
    public void Bonk()
    {
        hasBonked = true;
    }

}
