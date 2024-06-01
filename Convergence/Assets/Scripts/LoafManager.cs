using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine.WSA;

public class LoafManager : MonoBehaviour
{
    public static int SelectedLoafID;
    
    public static List<LoafManager> loafManagers;
    RectTransform rectTransform;

    Tween moveTween;

    private void Start()
    {
        rectTransform=GetComponent<RectTransform>();
        if (loafManagers == null)
            loafManagers = new List<LoafManager>();
        loafManagers.Add(this);

        rectTransform.anchoredPosition = new Vector2(2100*(LoafID-SelectedLoafID),0);
    }
    private void OnDestroy()
    {
        moveTween?.Kill();
        loafManagers.Remove(this);
    }
    bool isSelectedLoaf()
    {
        return SelectedLoafID==LoafID;
    }
    public static void UpdateSelectedLoafID(int id)
    {
        SelectedLoafID = id;
        SelectedLoafID = Mathf.Clamp(SelectedLoafID, 0, 2);

        foreach(LoafManager loafManager in loafManagers)
        {
            Vector2 targAnchoredPos= new Vector2(2100 * (loafManager.LoafID-SelectedLoafID), 0);
            
            loafManager.moveTween?.Kill();
            loafManager.moveTween = loafManager.rectTransform.DOAnchorPos(targAnchoredPos, 1f);
            loafManager.moveTween.Play();
        }
    }

    private void Update()
    {
        if (LoafID==0)
        {
            if(Input.GetKeyDown(KeyCode.D))
            {
                UpdateSelectedLoafID(SelectedLoafID + 1);
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                UpdateSelectedLoafID(SelectedLoafID - 1);
            }
        }
    }

    [Min(0)]
    public int LoafID;


    public LoafManager LoafLeft;
    public LoafManager LoafRight;



}
