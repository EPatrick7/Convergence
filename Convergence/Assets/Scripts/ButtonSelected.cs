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
    GameObject target;
    private void Start()
    {
        if(GetComponent<UnityEngine.UI.Button>() != null)
            target = gameObject;
        else
        {
            target=transform.parent.gameObject;
        }
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
            target.transform.localScale = new Vector3(hoverScale, hoverScale, hoverScale);
        }
        else
        {
            target.transform.localScale = new Vector3(normalScale, normalScale, normalScale);
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


}
