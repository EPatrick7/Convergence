using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine.WSA;
using UnityEngine.Windows;

public class LoafManager : MonoBehaviour
{
    public static int SelectedLoafID;
    
    public static List<LoafManager> loafManagers;
    RectTransform rectTransform;
    InputManager inputManager;

    static float loafSpace = 1425;

    Tween moveTween;

    private void Start()
    {
        rectTransform=GetComponent<RectTransform>();
        if (loafManagers == null)
            loafManagers = new List<LoafManager>();
        loafManagers.Add(this);

        rectTransform.anchoredPosition = new Vector2(loafSpace * (LoafID-SelectedLoafID),0);
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
            Vector2 targAnchoredPos= new Vector2(loafSpace * (loafManager.LoafID-SelectedLoafID), 0);
            
            loafManager.moveTween?.Kill();
            loafManager.moveTween = loafManager.rectTransform.DOAnchorPos(targAnchoredPos, 1f);
            loafManager.moveTween.Play();
        }
    }
    float delayBeforeNextChange;
    private void Update()
    {
        if (LoafID==0&&TutorialManager.instance.isLoafVisible())
        {
            bool going_Right = false, goingLeft = false;
            if (inputManager == null)
            {
                inputManager = InputManager.GetManager(1);
            }
            else if (inputManager.playerInput.currentControlScheme == "Gamepad")
            {
                Vector2 mouseDir = inputManager.playerInput.actions.FindActionMap("Player").FindAction("MousePosition").ReadValue<Vector2>().normalized;
                if(mouseDir.sqrMagnitude<=0.1f)
                {
                    delayBeforeNextChange = 0;
                }
                
                if (Mathf.Abs(mouseDir.y) < 0.2f&& Time.timeSinceLevelLoad > delayBeforeNextChange)
                {
                    if (mouseDir.x > 0.2f)
                    {
                        delayBeforeNextChange = Time.timeSinceLevelLoad + 0.5f;

                        going_Right = true;
                    }
                    else if (mouseDir.x < -0.2f)
                    {
                        delayBeforeNextChange = Time.timeSinceLevelLoad + 0.5f;
                        goingLeft = true;
                    }
                }
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)||going_Right)
            {
                UpdateSelectedLoafID(SelectedLoafID + 1);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)||goingLeft)
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
