using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine.WSA;
using UnityEngine.Windows;
using Unity.VisualScripting;

public class LoafManager : MonoBehaviour
{
    public static int SelectedLoafID;
    
    public static List<LoafManager> loafManagers;

    public CanvasGroup PlayText;

    RectTransform rectTransform;
    InputManager inputManager;

    static float loafSpace = 1425;

    Tween moveTween;
    Tween buttonTween;

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
        buttonTween?.Kill();
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
            loafManager.moveTween = loafManager.rectTransform.DOAnchorPos(targAnchoredPos, 0.5f);
            loafManager.moveTween.Play();
        }
    }
    public void UpdatePlayTextAlpha(float newValue)
    {
        buttonTween?.Kill();
        buttonTween = PlayText.DOFade(newValue, 0.5f);
        buttonTween.Play();
    }
    float delayBeforeNextChange;
    private void Update()
    {
        UpdatePlayTextAlpha(isSelectedLoaf() ? 1 : 0);


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
            else
            {
                float ScrollAxis = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(ScrollAxis) <= 0.1f&&Time.timeSinceLevelLoad < delayBeforeNextChange)
                {
                    delayBeforeNextChange-=0.01f;
                }

                if (Mathf.Abs(ScrollAxis) > 0.3f && Time.timeSinceLevelLoad > delayBeforeNextChange)
                {
                    if (ScrollAxis>0.1f)
                    {
                        delayBeforeNextChange = Time.timeSinceLevelLoad + 0.5f;

                        going_Right = true;
                    }
                    else if (ScrollAxis<0.1)
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
