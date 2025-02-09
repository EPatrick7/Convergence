using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ButtonSelected : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler//ISelectHandler
{

    [SerializeField]
    private float hoverScale;

    [SerializeField]
    private float normalScale;

    private enum ButtonType { None, Solo, Multi, Tutorial, Back, Other, Main, TutorialRestart };

    [SerializeField]
    private ButtonType btnState = ButtonType.None;

    GameObject target;

    private bool firstClick = false;

    private void Start()
    {
        if(GetComponent<UnityEngine.UI.Button>() != null)
            target = gameObject;
        else
        {
            target=transform.parent.gameObject;
        }
        gameObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(TaskOnClick);
    }
    private void Update()
    {

        if ((EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject))
        {
            if (GravityManager.Instance.isMultiplayer&&InputManager.GamePadDetected)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)&& GetComponent<Selectable>().navigation.selectOnLeft!=null)
                {
                    EventSystem.current.SetSelectedGameObject(GetComponent<Selectable>().navigation.selectOnLeft.gameObject);
                }
                if (Input.GetKeyDown(KeyCode.RightArrow)&& GetComponent<Selectable>().navigation.selectOnRight!=null)
                {
                    EventSystem.current.SetSelectedGameObject(GetComponent<Selectable>().navigation.selectOnRight.gameObject);
                }
            }
        }
    }
    private void FixedUpdate()
    {
        if(pointerOver||(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==gameObject))
        {
            if (!firstClick)
            {
                if(transform.parent==null||(transform.parent.GetComponent<CanvasGroup>() == null|| transform.parent.GetComponent<CanvasGroup>().alpha>0))
                    AudioManager.Instance?.HoverClick();
                firstClick = true;
            }
            if (target == gameObject)
            {
                if ((EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject))
                {
                    if (Input.GetKey(KeyCode.Return) &&GetComponent<UnityEngine.UI.Button>()!=null)
                    {
                        GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    }
                }
            }
            target.transform.localScale = new Vector3(Mathf.Sign(target.transform.localScale.x) * hoverScale, Mathf.Sign(target.transform.localScale.y) * hoverScale, Mathf.Sign(target.transform.localScale.z) * hoverScale);
        }
        else
        {
            if (firstClick) firstClick = false;
            target.transform.localScale = new Vector3(Mathf.Sign(target.transform.localScale.x) * normalScale, Mathf.Sign(target.transform.localScale.y) * normalScale, Mathf.Sign(target.transform.localScale.z) * normalScale);
        }
    }
    bool pointerOver;
    public void OnPointerEnter(PointerEventData eventData)
	{
        pointerOver = true;
	}

    public void OnPointerExit(PointerEventData eventData)
	{
        pointerOver = false;
	}
    

    private void TaskOnClick()
    {
        if (Time.timeSinceLevelLoad <= 0.1f)
            return;

            switch (btnState)
		{
            case ButtonType.Solo:
                AudioManager.Instance?.SoloSelect();
                break;

            case ButtonType.Multi:
                AudioManager.Instance?.MultiSelect();
                break;

            case ButtonType.Tutorial:
                AudioManager.Instance?.TutorialSelect();
                break;

            case ButtonType.Other:
                AudioManager.Instance?.GeneralSelect();
                break;

            case ButtonType.Back:
                AudioManager.Instance?.BackSelect();
                break;

            case ButtonType.Main:
                AudioManager.Instance?.MainSelect();
                break;

            case ButtonType.TutorialRestart:
                AudioManager.Instance?.TutorialRestartSelect();
                break;
        }
	}


}
